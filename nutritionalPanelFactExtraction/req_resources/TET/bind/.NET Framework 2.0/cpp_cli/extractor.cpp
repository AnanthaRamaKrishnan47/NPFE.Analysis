// extractor.cpp: main project file.
// Extractor example in C++/CLI calling into the TET .NET assembly
//
// $Id: extractor.cpp,v 1.1.2.1 2010/09/14 15:01:46 stm Exp $

#using "TET_dotnet.dll"

using namespace System;
using namespace System::IO;
using namespace System::Text;
using namespace TET_dotnet;

int main(array<System::String ^> ^args)
{
    /* global option list */
    String^ globaloptlist = "searchpath={{../data} {../../data}}";

    /* document-specific  option list */
    String^ docoptlist = "";

    /* page-specific option list */
    String^ pageoptlist = "granularity=page";

    /* separator to emit after each chunk of text. This depends on the
     * applications needs; for granularity=word a space character may be useful.
     */
    String^ separator = "\n";
 
    if (args->Length != 2)
    {
        Console::Error->WriteLine("usage: extractor <infilename> <outfilename>");
        return 2 ;
    }
   
    int pageno = 0;
    try
    {
        // Create a StreamWriter for writing UTF-8-encoded text with a BOM
        StreamWriter writer(File::Create(args[1]), gcnew UTF8Encoding(true));

        TET tet;
        tet.set_option(globaloptlist);

        const int doc = tet.open_document(args[0], docoptlist);

        if (doc == -1)
        {
            Console::Error->WriteLine("Error " + tet.get_errnum()
                + " in " + tet.get_apiname() + "(): "
                + tet.get_errmsg());
            return 2;
        }

        /* get number of pages in the document */
        const int n_pages = static_cast<int>(tet.pcos_get_number(doc, "length:pages"));

        /* loop over pages in the document */
        for (pageno = 1; pageno <= n_pages; ++pageno)
        {
            const int page = tet.open_page(doc, pageno, pageoptlist);

            if (page == -1)
            {
                Console::Error->WriteLine("Error " + tet.get_errnum()
                    + " in " + tet.get_apiname()
                    + "(): " + tet.get_errmsg());
                continue;                        /* try next page */
            }

            /* Retrieve all text fragments; This is actually not required
             * for granularity=page, but must be used for other granularities.
             */
            String^ text;
            while ((text = tet.get_text(page)) != nullptr)
            {
                writer.Write(text);

                /* print a separator between chunks of text */
                writer.Write(separator);
            }

            if (tet.get_errnum() != 0)
            {
                Console::Error->WriteLine("Error " + tet.get_errnum()
                    + " in " + tet.get_apiname()
                    + "() on page " + pageno
                    + ": " + tet.get_errmsg());
            }

            tet.close_page(page);
        }

        tet.close_document(doc);
        writer.Close();
    }
    catch (TETException^ ex) {
        if (pageno == 0)
        {
            Console::Error->WriteLine("Error " + ex->get_errnum()
                + " in " + ex->get_apiname()
                + "(): " + ex->get_errmsg());
        }
        else
        {
            Console::Error->WriteLine("Error " + ex->get_errnum()
                + " in " + ex->get_apiname()
                + "() on page " + pageno
                + ": " + ex->get_errmsg());
        }
        return 2;
    }

    return 0;
}
