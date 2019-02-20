import java.io.IOException;
import java.io.PrintWriter;
import java.io.UnsupportedEncodingException;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * TET sample servlet for dumping PDF information with pCOS
 * 
 * @version $Id: DumperServlet.java,v 1.2 2010/06/25 09:58:12 stm Exp $
 */
public class DumperServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;

    /**
     * @see HttpServlet#doGet(HttpServletRequest request, HttpServletResponse
     *      response)
     */
    protected void doGet(HttpServletRequest request,
            HttpServletResponse response) throws ServletException, IOException {
        response.setContentType("text/html; charset=UTF-8");
        PrintWriter writer = response.getWriter();

        final String filename = "TET-datasheet.pdf";

        writer.println("<html>");
        writer.println("<head>");
        writer.println("<title>TET J2EE Dumper Servlet Example<title>");
        writer.println("</head>");
        writer.println("<body>");
        writer.println("<pre>");

        TET tet = null;

        try {
            tet = new TET();
            String docoptlist = "requiredmode=minimum";
            String globaloptlist = "";

            /* This is where input files live. Adjust as necessary. */
            ServletContext context = getServletContext();
            String searchpath = context.getRealPath("/WEB-INF/data");

            String optlist;

            tet.set_option(globaloptlist);

            optlist = "searchpath={{" + searchpath + "}}";
            tet.set_option(optlist);

            final int doc = tet.open_document(filename, docoptlist);

            writer.println("<pre>");

            if (doc == -1) {
                writer.println("ERROR: " + tet.get_errmsg());
            }
            else {
                print_infos(tet, doc, writer);
                tet.close_document(doc);
            }
        }
        catch (TETException e) {
            writer.println("Error " + e.get_errnum() + " in "
                    + e.get_apiname() + "(): " + e.get_errmsg());
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
     * Print infos about the document.
     * 
     * @param tet
     *            TET object
     * @param doc
     *            TET document handle
     * @param writer
     *            Output stream   
     *
     * @throws TETException
     */
    private static void print_infos(TET tet, int doc, PrintWriter writer)
            throws TETException {
        /* --------- general information (always available) */
        int pcosmode = (int) tet.pcos_get_number(doc, "pcosmode");

        writer.println("   File name: " + tet.pcos_get_string(doc, "filename"));

        writer.println(" PDF version: "
                + tet.pcos_get_string(doc, "pdfversionstring"));

        writer.println("  Encryption: "
                + tet.pcos_get_string(doc, "encrypt/description"));

        writer.println("   Master pw: "
                + (tet.pcos_get_number(doc, "encrypt/master") != 0 ? "yes"
                        : "no"));

        writer.println("     User pw: "
                        + (tet.pcos_get_number(doc, "encrypt/user") != 0 ? "yes"
                                : "no"));

        writer.println("Text copying: "
                + (tet.pcos_get_number(doc, "encrypt/nocopy") != 0 ? "no"
                        : "yes"));

        writer.println("  Linearized: "
                + (tet.pcos_get_number(doc, "linearized") != 0 ? "yes" : "no"));

        if (pcosmode == 0) {
            writer.println("Minimum mode: no more information available\n\n");
        }
        else {
            print_userpassword_infos(tet, doc, pcosmode, writer);
        }
    }

    /**
     * Print infos that require at least the user password.
     * 
     * @param tet
     *            TET object
     * @param doc
     *            TET document handle
     * @param pcosmode
     *            pCOS mode for the document
     * @param writer
     *            Output stream           
     * 
     * @throws TETException
     */
    private static void print_userpassword_infos(TET tet, int doc,
            int pcosmode, PrintWriter writer) throws TETException {
        writer.println("PDF/X status: " + tet.pcos_get_string(doc, "pdfx"));

        writer.println("PDF/A status: " + tet.pcos_get_string(doc, "pdfa"));

        boolean xfa_present = !tet.pcos_get_string(doc,
                "type:/Root/AcroForm/XFA").equals("null");
        writer.println("    XFA data: " + (xfa_present ? "yes" : "no"));

        writer.println("  Tagged PDF: "
                + (tet.pcos_get_number(doc, "tagged") != 0 ? "yes" : "no"));
        writer.println();

        writer.println("No. of pages: "
                + (int) tet.pcos_get_number(doc, "length:pages"));

        writer.println(" Page 1 size: width="
                + (int) tet.pcos_get_number(doc, "pages[0]/width")
                + ", height="
                + (int) tet.pcos_get_number(doc, "pages[0]/height"));

        int count = (int) tet.pcos_get_number(doc, "length:fonts");
        writer.println("No. of fonts: " + count);

        for (int i = 0; i < count; i++) {
            if (tet.pcos_get_number(doc, "fonts[" + i + "]/embedded") != 0)
                writer.print("embedded ");
            else
                writer.print("unembedded ");

            writer.print(tet.pcos_get_string(doc, "fonts[" + i + "]/type")
                    + " font ");
            writer.println(tet.pcos_get_string(doc, "fonts[" + i + "]/name"));
        }

        writer.println();

        boolean plainmetadata = tet.pcos_get_number(doc,
                "encrypt/plainmetadata") != 0;

        if (pcosmode == 1 && !plainmetadata
                && tet.pcos_get_number(doc, "encrypt/nocopy") != 0) {
            writer
                    .println("Restricted mode: no more information available");
        }
        else {
            print_masterpassword_infos(tet, doc, writer);
        }
    }

    /**
     * Print document info keys and XMP metadata (requires master pw or
     * plaintext metadata).
     * 
     * @param tet
     *            TET object
     * @param doc
     *            TET document handle
     * @param pcosmode
     *            pCOS mode for the document
     * @param writer
     *            Output stream           
     *            
     * @throws TETException
     */
    private static void print_masterpassword_infos(TET tet, int doc,
            PrintWriter writer) throws TETException {
        String objtype;
        int count = (int) tet.pcos_get_number(doc, "length:/Info");

        for (int i = 0; i < count; i++) {
            objtype = tet.pcos_get_string(doc, "type:/Info[" + i + "]");
            String key = tet.pcos_get_string(doc, "/Info[" + i + "].key");

            int len = 12 - key.length();
            while (len-- > 0)
                writer.print(" ");
            writer.print(key + ": ");

            /*
             * Info entries can be stored as string or name objects
             */
            if (objtype.equals("string") || objtype.equals("name")) {
                writer.println("'"
                        + tet.pcos_get_string(doc, "/Info[" + i + "]") + "'");
            }
            else {
                writer.println("("
                        + tet.pcos_get_string(doc, "type:/Info[" + i + "]")
                        + "object)");
            }
        }

        writer.println();
        writer.print("XMP meta data: ");

        objtype = tet.pcos_get_string(doc, "type:/Root/Metadata");
        if (objtype.equals("stream")) {
            byte contents[] = tet.pcos_get_stream(doc, "", "/Root/Metadata");
            writer.print(contents.length + " bytes ");

            try {
                String string = new String(contents, "UTF-8");
                writer.println("(" + string.length()
                                + " Unicode characters)\n");
            }
            catch (UnsupportedEncodingException e) {
                writer.println("Internal error: wrong encoding specified");
            }
        }
        else {
            writer.println("not present\n\n");
        }
    }
}
