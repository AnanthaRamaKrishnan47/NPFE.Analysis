import java.io.IOException;
import java.io.PrintWriter;
import java.math.BigDecimal;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * Extract text from PDF and filter according to font name and size. This can be
 * used to identify headings in the document and create a table of contents.
 * 
 * @version $Id: FontfilterServlet.java,v 1.2 2010/06/25 09:58:12 stm Exp $
  */
public class FontfilterServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;

    /**
     * Document specific option list.
     */
    static final String docoptlist = "";

    /**
     * Page-specific option list.
     */
    static final String pageoptlist = "granularity=line";

    /**
     * Search text with at least this size (use 0 to catch all sizes).
     */
    static final double fontsizetrigger = 10;

    /**
     * Catch text where the font name contains this string (use empty string to
     * catch all font names).
     */
    static final String fontnametrigger = "Bold";

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
        writer.println("<title>TET J2EE Fontfilter Servlet Example<title>");
        writer.println("</head>");
        writer.println("<body>");
        writer.println("<pre>");

        TET tet = null;
        int pageno = 0;

        try {
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
            if (doc == -1) {
                writer.println("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
                return;
            }

            /*
             * Loop over pages in the document
             */
            final int n_pages = (int) tet.pcos_get_number(doc, "length:pages");
            for (pageno = 1; pageno <= n_pages; ++pageno) {
                process_page(tet, doc, pageno, writer);
            }

            tet.close_document(doc);
        }
        catch (TETException e) {
            if (pageno == 0) {
                writer.println("Error " + e.get_errnum() + " in "
                        + e.get_apiname() + "(): " + e.get_errmsg() + "\n");
            }
            else {
                writer.println("Error " + e.get_errnum() + " in "
                        + e.get_apiname() + "() on page " + pageno + ": "
                        + e.get_errmsg() + "\n");
            }
        }
        finally {
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
     * Process all words on the page and print the words that match the desired
     * font.
     * 
     * @param tet
     *            TET object
     * @param doc
     *            TET document handle
     * @param pageno
     *            Page to process
     * @param writer
     *            Output stream
     * 
     * @throws TETException
     *             An error occurred in the TET API
     */
    private static void process_page(TET tet, final int doc, int pageno, 
                                        PrintWriter writer)
            throws TETException {
        final int page = tet.open_page(doc, pageno, pageoptlist);

        if (page == -1) {
            writer.println("Error " + tet.get_errnum() + " in "
                    + tet.get_apiname() + "(): " + tet.get_errmsg());
            return; /* try next page */
        }

        /* Retrieve all text fragments for the page */
        for (String text = tet.get_text(page); text != null; 
                text = tet.get_text(page)) {
            /* Loop over all characters */
            for (int ci = tet.get_char_info(page); ci != -1; 
                        ci = tet.get_char_info(page)) {
                /*
                 * We need only the font name and size; the text position could
                 * be fetched from tet.x and tet.y.
                 */
                final String fontname = tet.pcos_get_string(doc, "fonts["
                        + tet.fontid + "]/name");

                /* Check whether we found a match */
                if (tet.fontsize >= fontsizetrigger
                        && fontname.indexOf(fontnametrigger) != -1) {
                    /* print the retrieved font name, size, and text */
                    BigDecimal roundedValue = (new BigDecimal(tet.fontsize))
                            .setScale(2, BigDecimal.ROUND_HALF_UP);
                    writer.println("[" + fontname + " " + roundedValue.toString()
                            + "] " + text);
                }

                /*
                 * In this sample we check only the first character of each
                 * fragment.
                 */
                break;
            }
        }

        if (tet.get_errnum() != 0) {
            writer.println("Error " + tet.get_errnum() + " in "
                    + tet.get_apiname() + "(): " + tet.get_errmsg());
        }

        tet.close_page(page);
    }
}
