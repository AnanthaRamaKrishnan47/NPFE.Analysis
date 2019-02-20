import java.io.IOException;
import java.io.PrintWriter;
import java.text.DecimalFormat;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * PDF text extractor servlet based on PDFlib TET
 *
 * @version $Id: ExtractorServlet.java,v 1.3 2010/06/25 09:58:12 stm Exp $
 */
public class ExtractorServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;
    
    /**
     * Document-specific option list
     */
    static final String docoptlist = "";
    
    /**
     * Page-specific option list
     */
    static final String pageoptlist = "granularity=page";
    
    /**
     * Separator to emit after each chunk of text. This depends on the
     * applications needs; for granularity=word a space character may be useful.
     */
    static final String separator = "\n";

    /**
     * Here you can insert basic image extract options (more below)
     */
    static final String baseimageoptlist = "";

    /**
     * Set inmemory to true to generate the image in memory.
     */
    static final boolean inmemory = true;
    
    /**
     * Name of input document.
     */
    static final String infile = "TET-datasheet.pdf";
    
    /**
     * @see HttpServlet#doGet(HttpServletRequest request, HttpServletResponse
     *      response)
     */
    protected void doGet(HttpServletRequest request,
            HttpServletResponse response) throws ServletException, IOException {
        response.setContentType("text/html; charset=UTF-8");
        PrintWriter writer = response.getWriter();
        
        writer.println("<html>");
        writer.println("<head>");
        writer.println("<title>TET J2EE Extractor Servlet Example<title>");
        writer.println("</head>");
        writer.println("<body>");
        writer.println("<pre>");
        
        TET tet = null;
        
        try
        {
            tet = new TET();

            /* This is where input files live. Adjust as necessary. */
            ServletContext context = getServletContext();
            final String datapath = context.getRealPath("/WEB-INF/data");
            final String cmappath = context.getRealPath("/WEB-INF/resource/cmap");

            /**
             * Global option list
             */
            final String globaloptlist = "searchpath={{" + datapath + 
                                "} {" + cmappath + "}}";
            
            tet.set_option(globaloptlist);
            
            final int doc = tet.open_document(infile, docoptlist);

            if (doc == -1)
            {
                throw new Exception("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
            }
            
            /* get number of pages in the document */
            final int n_pages = (int) tet.pcos_get_number(doc, "length:pages");

            /* loop over pages in the document */
            for (int pageno = 1; pageno <= n_pages; ++pageno)
            {
                String text;
                int page;
                int imageno = -1;
                
                page = tet.open_page(doc, pageno, pageoptlist);

                if (page == -1)
                {
                    print_tet_error(tet, pageno, writer);
                    continue; /* try next page */
                }

                /*
                 * Retrieve all text fragments; This is actually not required
                 * for granularity=page, but must be used for other
                 * granularities.
                 */
                while ((text = tet.get_text(page)) != null)
                {
                    /* print the retrieved text */
                    writer.write(text);

                    /* print a separator between chunks of text */
                    writer.write(separator);
                }

                /* Retrieve all images on the page */
                while (tet.get_image_info(page) == 1)
                {
                    int width, height, bpc, cs;

                    imageno++;

                    /* Print the following information for each image:
                     * - page and image number
                     * - pCOS id (required for indexing the images[] array)
                     * - physical size of the placed image on the page
                     * - pixel size of the underlying PDF image
                     * - number of components, bits per component,and colorspace
                     */
                    width = (int) tet.pcos_get_number(doc,
                                "images[" + tet.imageid + "]/Width");
                    height = (int) tet.pcos_get_number(doc,
                                "images[" + tet.imageid + "]/Height");
                    bpc = (int) tet.pcos_get_number(doc,
                                "images[" + tet.imageid + "]/bpc");
                    cs = (int) tet.pcos_get_number(doc,
                                "images[" + tet.imageid + "]/colorspaceid");

                    DecimalFormat df = new DecimalFormat();
                    df.setMinimumFractionDigits(2);
                    df.setMaximumFractionDigits(2);

                    writer.print("page " + pageno + ", image " 
                            + imageno + ": id=" + tet.imageid  
                            + ", " + df.format(tet.width)
                            + "x" + df.format(tet.height)
                            + " point, ");

                    writer.print(width + "x" + height + " pixel, ");
                    
                    if (cs != -1)
                    {
                        writer.println(
                            (int) tet.pcos_get_number(doc, "colorspaces[" 
                            + cs + "]/components") + "x" + bpc + " bit " +
                            tet.pcos_get_string(doc, "colorspaces[" + cs 
                            + "]/name"));
                    }
                    else {
                        /* cs==-1 may happen for some JPEG 2000 images. bpc,
                         * colorspace name and number of components are not
                         * available in this case.
                         */
                        writer.println("JPEG2000");
                    }

                    if (inmemory)
                    {
                        /* Fetch the image data and store it in memory */
                        byte imagedata[] = tet.get_image_data(doc, tet.imageid,
                                baseimageoptlist);

                        if (imagedata == null)
                        {
                            print_tet_error(tet, pageno, writer);
                            continue; /* process next image */
                        }

                        /*
                         * Client-specific image data consumption would go here
                         * We simply report the size of the data.
                         */
                        writer.println("Page " + pageno + ": "
                                + imagedata.length + " bytes of image data");
                    }
                    else
                    {
                        /*
                         * Fetch the image data and write it to a disk file. The
                         * output filenames are generated from the inputfilename
                         * by appending page number and image number.
                         * 
                         * This is intentionally disabled in the sample program
                         * to prevent unwanted writing of the images to an 
                         * directory in the application server installation.
                         * 
                        String imageoptlist = baseimageoptlist + " filename={"
                            + outfilebase + "_p" + pageno + "_" + imageno + "}";

                        if (tet.write_image_file(doc, tet.imageid,
                                        imageoptlist) == -1)
                        {
                            print_tet_error(tet, pageno, writer);
                            continue;
                        }
                         */
                        writer.println("Image extraction to disk disabled!");
                    }

                }

                if (tet.get_errnum() != 0)
                {
                    print_tet_error(tet, pageno, writer);
                }

                tet.close_page(page);
            }

            tet.close_document(doc);
        }
        catch (TETException e)
        {
            writer.println("TET exception occurred in extractor sample:");
            writer.println("[" + e.get_errnum() + "] " + e.get_apiname() +
                            ": " + e.get_errmsg());
        }
        catch (Exception e)
        {
            writer.println(e.getMessage());
        }
        finally
        {
            writer.println("</pre>");
            writer.println("</body>");
            writer.println("</html>");
            writer.close();
            if (tet != null) {
                tet.delete();
            }
        }
    }

    /**
     * Report a TET error.
     * 
     * @param tet
     *            The TET object
     * @param pageno
     *            The page number on which the error occurred
     * @param writer
     *            Output stream for error message
     */
    private static void print_tet_error(TET tet, int pageno, PrintWriter writer)
    {
        writer.println("Error " + tet.get_errnum() + " in  "
                + tet.get_apiname() + "() on page " + pageno + ": "
                + tet.get_errmsg());
    }

}
