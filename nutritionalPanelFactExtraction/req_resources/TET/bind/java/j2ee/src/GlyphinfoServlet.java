import java.io.IOException;
import java.io.PrintWriter;
import java.util.Formatter;
import java.util.Locale;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * Simple PDF glyph dumper servlet based on PDFlib TET.
 *
 * @version $Id: GlyphinfoServlet.java,v 1.3 2010/06/25 09:58:12 stm Exp $
 */
public class GlyphinfoServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;

    /**
     * Document-specific option list
     */
    static final String docoptlist = "";

    /**
     * Page-specific option list
     */
    static final String pageoptlist = "granularity=word";

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
        writer.println("<title>TET J2EE Glyph Information Servlet Example<title>");
        writer.println("</head>");
        writer.println("<body>");
        writer.println("<pre>");

        TET tet = null;
        try
        {
            Formatter formatter = new Formatter(writer, Locale.US);

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

            int doc = tet.open_document(infile, docoptlist);

            if (doc == -1)
            {
                throw new Exception("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
            }
            
            /* get number of pages in the document */
            int n_pages = (int) tet.pcos_get_number(doc, "length:pages");

            /* loop over pages in the document */
            for (int pageno = 1; pageno <= n_pages; ++pageno)
            {
                String text;
                int page;
                
                page = tet.open_page(doc, pageno, pageoptlist);

                if (page == -1)
                {
                    print_tet_error(tet, pageno, writer);
                    continue; /* try next page */
                }

                /* Administrative information */
                formatter.format("\n[ Document: '" + 
                    tet.pcos_get_string(doc, "filename") + "' ]\n");

                formatter.format("[ Document options: '%s' ]\n", docoptlist);

                formatter.format("[ Page options: '%s' ]\n", pageoptlist);

                formatter.format("[ ----- Page %d ----- ]\n", pageno);

                /* Retrieve all text fragments */
                while ((text = tet.get_text(page)) != null)
                {
                    /* print the retrieved text */
                    writer.write("[" + text + "]\n");

                    /* Loop over all glyphs and print their details */
                    while (tet.get_char_info(page) != -1)
                    {
                        final String fontname;

                        /* Fetch the font name with pCOS (based on its ID) */
                        fontname = tet.pcos_get_string(doc,
                                    "fonts[" + tet.fontid + "]/name");

                        /* Print the character */
                        formatter.format("U+%04X", tet.uv);

                        /* ...and its UTF8 representation */
                        formatter.format(" '%c'", tet.uv);

                        /* Print font name, size, and position */
                        formatter.format(" %s size=%.2f x=%.2f y=%.2f",
                            fontname, tet.fontsize, tet.x, tet.y);

                        /* Examine the "type" member */
                        if (tet.type == 1)
                            formatter.format(" ligature_start");

                        else if (tet.type == 10)
                            formatter.format(" ligature_cont");

                        /* Separators are only inserted for granularity > word*/
                        else if (tet.type == 12)
                            formatter.format(" inserted");

                        /* Examine the bit flags in the "attributes" member */
                        final int ATTR_NONE = 0;
                        final int ATTR_SUB = 1;
                        final int ATTR_SUP = 2;
                        final int ATTR_DROPCAP = 4;
                        final int ATTR_SHADOW = 8;
                        final int ATTR_DH_PRE = 16;
                        final int ATTR_DH_ARTIFACT = 32;
                        final int ATTR_DH_POST = 64;

                        if (tet.attributes != ATTR_NONE)
                        {
                            if ((tet.attributes & ATTR_SUB) == ATTR_SUB)
                                formatter.format("/sub");
                            if ((tet.attributes & ATTR_SUP) == ATTR_SUP)
                                formatter.format("/sup");
                            if ((tet.attributes & ATTR_DROPCAP) == ATTR_DROPCAP)
                                formatter.format("/dropcap");
                            if ((tet.attributes & ATTR_SHADOW) == ATTR_SHADOW)
                                formatter.format("/shadow");
                            if ((tet.attributes & ATTR_DH_PRE) == ATTR_DH_PRE)
                                formatter.format("/dehyphenation_pre");
                            if ((tet.attributes & ATTR_DH_ARTIFACT) == ATTR_DH_ARTIFACT)
                                formatter.format("/dehyphenation_artifact");
                            if ((tet.attributes & ATTR_DH_POST) == ATTR_DH_POST)
                                formatter.format("/dehyphenation_post");
                        }

                        formatter.format("\n");
                    }

                    formatter.format("\n");
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
            writer.println("TET exception occurred in glyphinfo sample:");
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
