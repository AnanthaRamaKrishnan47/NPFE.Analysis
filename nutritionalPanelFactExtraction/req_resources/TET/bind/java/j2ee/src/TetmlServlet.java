
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.PrintWriter;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import org.xml.sax.Attributes;
import org.xml.sax.InputSource;
import org.xml.sax.SAXException;
import org.xml.sax.XMLReader;
import org.xml.sax.helpers.DefaultHandler;
import org.xml.sax.helpers.XMLReaderFactory;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * Extract text from PDF document as XML. Fetch the XML in memory, parse it and
 * print some information to the browser.
 * 
 * @version $Id: TetmlServlet.java,v 1.3 2015/07/29 12:32:38 rp Exp $
 */
public class TetmlServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;

    /**
     * Set inmemory to true to process TETML in memory and to display the
     * results as a web page. Otherwise the TETML file is written to disk.
     */
    static final boolean inmemory = true;
    
    /**
     * Document specific option list.
     */
    static final String basedocoptlist = "";

    /**
     * Page-specific option list.
     */
    static final String pageoptlist = "granularity=word";

    /**
     * Name of input document.
     */
    static final String infile = "TET-datasheet.pdf";

    /**
     * Word counter for in-memory processing code.
     */
    int word_count = 0;
    
    /**
     * SAX handler class to count the words in the document.
     */
    private class sax_handler extends DefaultHandler
    {
        PrintWriter writer;
        
        public sax_handler(PrintWriter writer) {
            this.writer = writer;
        }

        public void startElement (String uri, String local_name,
            String qualified_name, Attributes attributes) throws SAXException
        {
            if (local_name.equals("Word"))
            {
                word_count += 1;
            }
            else if (local_name.equals("Font"))
            {
                writer.println("Font " + attributes.getValue("", "name")
                        + " (" + attributes.getValue("", "type") + ")");
            }
        }
    }
    
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
        writer.println("<title>TET J2EE TETML Servlet Example<title>");
        writer.println("</head>");
        writer.println("<body>");
        writer.println("<pre>");

        /*
         * For JRE 1.4 the property must be set what XML parser to use, later
         * JREs seem to have a default set internally. It seems to be the case
         * that in 1.4 org.apache.crimson.parser.XMLReaderImpl is always
         * available.
         */
        String jre_version = System.getProperty("java.version");
        if (jre_version.startsWith("1.4")) {
            System.setProperty("org.xml.sax.driver",
                    "org.apache.crimson.parser.XMLReaderImpl");
        }

        TET tet = null;
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

            final String tetmlname = infile + ".tetml";
            final String docoptlist = (inmemory ? "tetml={}"
                    : "tetml={filename={" + tetmlname + "}}")
                    + " " + basedocoptlist;

            if (inmemory) {
                writer.println("Processing TETML output for document \""
                        + infile + "\" in memory...");
            }
            else {
                writer.println("Extracting TETML for document \"" + infile
                        + "\" to file \"" + tetmlname + "\"...");
            }

            final int doc = tet.open_document(infile, docoptlist);
            if (doc == -1) {
                writer.println("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
                tet.delete();
                return;
            }

            final int n_pages = (int) tet.pcos_get_number(doc, "length:pages");

            /*
             * Loop over pages in the document;
             */
            for (int pageno = 0; pageno <= n_pages; ++pageno) {
                tet.process_page(doc, pageno, pageoptlist);
            }

            /*
             * This could be combined with the last page-related call.
             */
            tet.process_page(doc, 0, "tetml={trailer}");

            if (inmemory) {
                /*
                 * Get the XML document as a byte array.
                 */
                final byte[] tetml = tet.get_tetml(doc, "");

                if (tetml == null) {
                    writer.println("tetml: couldn't retrieve XML data");
                    return;
                }

                /*
                 * Process the in-memory XML document to print out some
                 * information that is extracted with the sax_handler class.
                 */

                XMLReader reader = XMLReaderFactory.createXMLReader();
                reader.setContentHandler(new sax_handler(writer));
                reader.parse(new InputSource(new ByteArrayInputStream(tetml)));
                writer.println("Found " + word_count + " words in document");
            }

            tet.close_document(doc);
        }
        catch (TETException e) {
            writer.println("Error " + e.get_errnum() + " in "
                    + e.get_apiname() + "(): " + e.get_errmsg());
        }
        catch (Exception e) {
            e.printStackTrace(writer);
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
}
