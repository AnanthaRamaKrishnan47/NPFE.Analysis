/* PDF text extractor based on PDFlib TET
 *
 * $Id: extractor.java,v 1.31.2.1 2011/08/25 20:16:19 rjs Exp $
 */

import java.io.*;
import com.pdflib.TETException;
import com.pdflib.TET;
import java.text.DecimalFormat;

public class extractor
{
    /**
     * Global option list
     */
    static final String globaloptlist =
	    "searchpath={{../data} {../../../resource/cmap}}";
    
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

    public static void main (String argv[])
    {
        TET tet = null;
        
	try
        {
	    if (argv.length != 2)
            {
                throw new Exception(
		    "usage: extractor <filename> <outfilename>");
            }

            Writer outfp = new BufferedWriter(new OutputStreamWriter(
                    new FileOutputStream(argv[1]), "UTF-8"));

            tet = new TET();

            tet.set_option(globaloptlist);

            int doc = tet.open_document(argv[0], docoptlist);

            if (doc == -1)
            {
                throw new Exception("Error " + tet.get_errnum() + "in "
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
                    print_tet_error(tet, pageno);
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
                    outfp.write(text);

                    /* print a separator between chunks of text */
                    outfp.write(separator);
                }


                if (tet.get_errnum() != 0)
                {
                    print_tet_error(tet, pageno);
                }

                tet.close_page(page);
            }

            tet.close_document(doc);
            outfp.close();
        }
	catch (TETException e)
	{
	    System.err.println("TET exception occurred in extractor sample:");
	    System.err.println("[" + e.get_errnum() + "] " + e.get_apiname() +
			    ": " + e.get_errmsg());
        }
        catch (Exception e)
        {
            System.err.println(e.getMessage());
        }
        finally
        {
            if (tet != null) {
		tet.delete();
            }
        }
    }

    /**
     * Report a TET error.
     * 
     * @param tet The TET object
     * @param pageno The page number on which the error occurred
     */
    private static void print_tet_error(TET tet, int pageno)
    {
        System.err.println("Error " + tet.get_errnum() + " in  "
                + tet.get_apiname() + "() on page " + pageno + ": "
                + tet.get_errmsg());
    }
}
