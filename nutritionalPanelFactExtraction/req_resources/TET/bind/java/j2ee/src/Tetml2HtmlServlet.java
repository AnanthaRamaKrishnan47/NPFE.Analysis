import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.io.PrintWriter;

import javax.servlet.ServletContext;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.xml.transform.Result;
import javax.xml.transform.Source;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.stream.StreamResult;
import javax.xml.transform.stream.StreamSource;

import com.pdflib.TET;
import com.pdflib.TETException;

/**
 * Servlet to perform on-the-fly transformation of PDF to HTML by generating
 * TETML with PDFlib TET and by transforming TETML to HTML with an XSLT
 * stylesheet.
 * 
 * @version $Id: Tetml2HtmlServlet.java,v 1.5 2015/07/29 12:32:38 rp Exp $
 */
public class Tetml2HtmlServlet extends HttpServlet {
    private static final long serialVersionUID = 1L;

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
        OutputStream out = response.getOutputStream();
        
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
            
            /* Get paths of resources */
            ServletContext context = getServletContext();
            final String datapath = context.getRealPath("/WEB-INF/data");
            final String cmappath = context.getRealPath("/WEB-INF/resource/cmap");
            final String xsltpath = context.getRealPath("/WEB-INF/tetml2html.xsl");

            /*
             * Global option list
             */
            final String globaloptlist = "searchpath={{" + datapath + 
                                "} {" + cmappath + "}}";
            
            tet.set_option(globaloptlist);

            final int doc = tet.open_document(infile, "tetml={}");
            if (doc == -1) {
                response.getWriter().println("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
                tet.delete();
                return;
            }

            final int n_pages = (int) tet.pcos_get_number(doc, "length:pages");

            /*
             * Loop over pages in the document;
             */
            final String pageoptlist = "tetml={ glyphdetails={all} } granularity=word";
            for (int pageno = 1; pageno <= n_pages; ++pageno) {
                tet.process_page(doc, pageno, pageoptlist);
            }

            /*
             * This could be combined with the last page-related call.
             */
            tet.process_page(doc, 0, "tetml={trailer}");

            /*
             * Get the TETML document as a byte array.
             */
            final byte[] tetml = tet.get_tetml(doc, "");

            if (tetml == null) {
                PrintWriter writer = new PrintWriter(out);
                writer.println("tetml: couldn't retrieve XML data");
            }
            else {
                /*
                 * Transform the TETML to HTML by transforming it with the 
                 * XSL stylesheet and send it to the browser.
                 */
                Source xmlInput = new StreamSource(new ByteArrayInputStream(tetml));
                Source xsltSource = new StreamSource(xsltpath);
                Result result = new StreamResult(response.getOutputStream());
                
                TransformerFactory factory = TransformerFactory.newInstance();
                Transformer transformer = factory.newTransformer(xsltSource);
                transformer.transform(xmlInput, result);
            }

            tet.close_document(doc);
        }
        catch (TETException e) {
            PrintWriter writer = new PrintWriter(out);
            writer.println("Error " + e.get_errnum() + " in "
                    + e.get_apiname() + "(): " + e.get_errmsg());
        }
        catch (Exception e) {
            PrintWriter writer = new PrintWriter(out);
            e.printStackTrace(writer);
        }
        finally {
            if (tet != null) {
                tet.delete();
            }
        }
    }
}
