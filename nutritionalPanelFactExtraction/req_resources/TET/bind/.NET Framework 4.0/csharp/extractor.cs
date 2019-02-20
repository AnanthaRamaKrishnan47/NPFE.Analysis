// $Id: extractor.cs,v 1.24.2.1 2010/07/28 15:00:52 rjs Exp $
//
// PDF text extractor based on PDFlib TET
//

using System;
using System.IO;
using System.Text;
using TET_dotnet;

class Extractor {
  static int Main(string[] args) {

    /* global option list */
    string globaloptlist = "searchpath={{../data} {../../data}}";

    /* document-specific  option list */
    string docoptlist = "";

    /* page-specific option list */
    string pageoptlist = "granularity=page";

    /* separator to emit after each chunk of text. This depends on the
     * applications needs; for granularity=word a space character may be useful.
     */
    string separator = "\n";

    TET tet;
    FileStream outfile;
    BinaryWriter w;
    int pageno = 0;

    UnicodeEncoding unicode = new UnicodeEncoding(false, true);
    Byte[] byteOrderMark = unicode.GetPreamble();


    if (args.Length != 2)
    {
        Console.WriteLine("usage: extractor <infilename> <outfilename>");
        return(2);
    }

    outfile = File.Create(args.GetValue(1).ToString());
    w = new BinaryWriter(outfile);
    w.Write(byteOrderMark);

    tet = new TET();

    try
    {
        int n_pages;

        tet.set_option(globaloptlist);

        int doc = tet.open_document(args.GetValue(0).ToString(), docoptlist);

        if (doc == -1)
        {
            Console.WriteLine("Error {0} in {1}(): {2}",
                tet.get_errnum(), tet.get_apiname(), tet.get_errmsg());
            return(2);
        }

        /* get number of pages in the document */
        n_pages = (int) tet.pcos_get_number(doc, "length:pages");

        /* loop over pages in the document */
        for (pageno = 1; pageno <= n_pages; ++pageno)
        {
            string text;
            int page;

            page = tet.open_page(doc, pageno, pageoptlist);

            if (page == -1)
            {
                Console.WriteLine("Error {0} in {1}() on page {2}: {3}",
                    tet.get_errnum(), tet.get_apiname(), pageno,
                    tet.get_errmsg());
                continue;                        /* try next page */
            }

            /* Retrieve all text fragments; This is actually not required
             * for granularity=page, but must be used for other
             * granularities.
             */
            while ((text = tet.get_text(page)) != null)
            {
                /* print the retrieved text */
                w.Write(unicode.GetBytes(text));

                /* print a separator between chunks of text */
                w.Write(unicode.GetBytes(separator));
            }

            if (tet.get_errnum() != 0)
            {
                Console.WriteLine("Error {0} in {1}(): {3}",
                    tet.get_errnum(), tet.get_apiname(), tet.get_errmsg());
            }
            tet.close_page(page);
        }
        tet.close_document(doc);
    }
    catch (TETException e) {
        /* caught exception thrown by TET */
	Console.WriteLine("Error {0} in {1}(): {2}",
                e.get_errnum(), e.get_apiname(), e.get_errmsg());
    }
    catch (Exception e)
    {
        Console.WriteLine("General Exception: " + e.ToString());
        return(2);
    }
    finally
    {
        outfile.Close();
        if (tet != null) {
            tet.Dispose();
        }
    }

    return 0;
  }
}
