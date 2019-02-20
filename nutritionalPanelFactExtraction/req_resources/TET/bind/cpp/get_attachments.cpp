/*
 * PDF text extractor based on PDFlib TET
 *
 * The text is written as UTF-16 or as UTF-32 in native byte order, depending
 * on the size of wchar_t.
 *
 * Due to the lack of portable conversions from narrow to wide character
 * the program assumes that the program arguments are encoded as UTF-8.
 *
 * $Id: get_attachments.cpp,v 1.3 2017/05/23 15:57:21 rp Exp $
 */

#include <iostream>
#include <iomanip>
#include <fstream>
#include <algorithm>

#include "tet.hpp"

using namespace std;
using namespace pdflib;

namespace
{

/*  global option list */
const wstring globaloptlist =
        L"searchpath={{../data} {../../../resource/cmap}}";

/* document-specific  option list */
const wstring docoptlist = L"";

/* page-specific option list */
const wstring pageoptlist = L"granularity=page";

/* separator to emit after each chunk of text. This depends on the
 * applications needs; for granularity=word a space character may be useful.
 */
const string utf8_separator = "\n";


wstring get_wstring(const TET& tet, const string& utf8_string);
} // end of anonymous namespace
void process_document(ofstream& outfp, wstring filename, const wstring realname, const void *data, size_t size);
void process_document_file(ofstream& outfp, TET& tet, int doc);



/**
* Extract text from a document for which a TET handle is already available.
* 
* @param tet
*            The TET object
* @param doc
*            A valid TET document handle
* @param outfp
*            Output file handle
* 
* @throws TETException
* @throws IOException
*/

void extract_text(TET& tet, int doc, ofstream& outfp){
            /* get number of pages in the document */
        const int n_pages = (int) tet.pcos_get_number(doc, L"length:pages");

        /* loop over pages in the document */
        for (int pageno = 1; pageno <= n_pages; ++pageno)
        {
            wstring text;
            const int page = tet.open_page(doc, pageno, pageoptlist);

            if (page == -1)
            {
                wcerr << L"Error " << tet.get_errnum()
                    << L" in " << tet.get_apiname()
                    << L"(): " << tet.get_errmsg() << endl;
                continue;                        /* try next page */
            }

            /* Retrieve all text fragments; This is actually not required
             * for granularity=page, but must be used for other granularities.
             */
            while ((text = tet.get_text(page)) != L"")
            {
                /* print the retrieved text as UTF-16 or UTF-32 encoded in
                 * the native byte order
                 */
                outfp.write(reinterpret_cast<const char *>(text.c_str()),
                        static_cast<streamsize>(text.size())
                            * sizeof(wstring::value_type));

                /* print a separator between chunks of text */
                outfp.write(reinterpret_cast<const char *>(utf8_separator.c_str()),
                        static_cast<streamsize>(utf8_separator.size())
                            * sizeof(wstring::value_type));
            }

            if (tet.get_errnum() != 0)
            {
                wcerr << L"Error " << tet.get_errnum()
                    << L" in " << tet.get_apiname()
                    << L"() on page " << pageno
                    << L": " << tet.get_errmsg() << endl;
            }

            tet.close_page(page);
        }
    }

/**
* Open a named physical or virtual file, extract the text from it, search
* for document or page attachments, and process these recursively. Either
* filename must be supplied for physical files, or data+length from which a
* virtual file will be created. The caller cannot create the PVF file since
* we create a new TET object here in case an exception happens with the
* embedded document - the caller can happily continue with his TET object
* even in case of an exception here.
* 
* @param outfp
* @param filename
* @param realname
* @param data
* 
* @return 0 if successful, otherwise a non-null code to be used as exit
*         status
*/
void process_document(ofstream& outfp, wstring filename, const wstring realname, const void *data, size_t size)
    {
        int retval = 0;
        TET tet;

        try
        {
            const wstring pvfname = L"/pvf/attachment";
            
            /*
             * Construct a PVF file if data instead of a filename was provided
             */
            if (filename == L"")
            {
                tet.create_pvf(pvfname, data, size, L"");
                filename = pvfname;
            }

            tet.set_option(globaloptlist);

            int doc = tet.open_document(filename, docoptlist);

            if (doc == -1)

            {
                wcerr << L"Error " << tet.get_errnum()
                    << L" in " << tet.get_apiname() << L"(): (source: attachment '"
                    << realname << L"'):" << tet.get_errmsg() << endl;
                    retval = 5;
            }
            else
            {
                process_document_file(outfp, tet, doc);
            }

            /*
             * If there was no PVF file deleting it won't do any harm
             */
            tet.delete_pvf(pvfname);
        }
        catch (TET::Exception &e)
        {
            wcerr << L"Error " << e.get_errnum()
                    << L" in " << e.get_apiname() << L"(): (source: attachment '"
                    << realname << L"'):" << e.get_errmsg() << endl;
                    retval = 5;
            retval = 1;
        }

        //return retval;
    }



    /**
     * Process a single file.
     * 
     * @param outfp Output stream for messages
     * @param tet The TET object
     * @param doc The TET document handle
     * 
     * @throws TETException
     * @throws IOException
     */
void process_document_file(ofstream& outfp, TET& tet, int doc){
        wstring objtype;
        wostringstream pcospath;
        
        // -------------------- Extract the document's own page contents
        extract_text(tet, doc, outfp);

        // -------------------- Process all document-level file attachments

        // Get the number of document-level file attachments.

        int filecount = (int) tet.pcos_get_number(doc,
                L"length:names/EmbeddedFiles");

        for (int file = 0; file < filecount; file++)
        {
            wstring attname;

            /*
             * fetch the name of the file attachment; check for Unicode file
             * name (a PDF 1.7 feature)
             */
            pcospath.str(L"");
            pcospath << L"type:names/EmbeddedFiles[" << file << "]/UF";
            objtype = tet.pcos_get_string(doc, pcospath.str());

            if (objtype == L"string")
            {
                pcospath.str(L"");
                pcospath << L"names/EmbeddedFiles[" << file << "]/UF";
                attname = tet.pcos_get_string(doc, pcospath.str());
            }
            else
            {
                pcospath.str(L"");
                pcospath << L"type:names/EmbeddedFiles[" << file << "]/F";
                objtype = tet.pcos_get_string(doc, pcospath.str());
    
                if (objtype == L"string")
                {
                    pcospath.str(L"");
                    pcospath << L"names/EmbeddedFiles[" << file << "]/F";
                    attname = tet.pcos_get_string(doc, pcospath.str());
                }
                else
                {
                    attname = L"(unnamed)";
                }
            }
            /* fetch the contents of the file attachment and process it */
            pcospath.str(L"");
            pcospath << L"type:names/EmbeddedFiles[" << file << "]/EF/F";
            objtype = tet.pcos_get_string(doc, pcospath.str());

            if (objtype == L"stream")
            {
                int len;
                wstring text = L"----- File attachment '" + attname + L"'\n";
                outfp.write(reinterpret_cast<const char*>(text.c_str()),
                            text.size() * sizeof(wstring::value_type));

                pcospath.str(L"");
                pcospath << L"names/EmbeddedFiles[" << file << "]/EF/F";
                const unsigned char *attdata = tet.pcos_get_stream(doc, &len, L"", pcospath.str());

                process_document(outfp, L"" , attname, attdata, len);
                text = L"----- End file attachment '" + attname + L"'\n";
                outfp.write(reinterpret_cast<const char*>(text.c_str()),
                            text.size() * sizeof(wstring::value_type));
            }
        }

        // -------------------- Process all page-level file attachments

        int pagecount = (int) tet.pcos_get_number(doc, L"length:pages");

        // Check all pages for annotations of type FileAttachment
        for (int page = 0; page < pagecount; page++)
        {
            pcospath.str(L"");
            pcospath << L"length:pages[" << page << "]/Annots";
            int annotcount = (int) tet.pcos_get_number(doc, pcospath.str());

            for (int annot = 0; annot < annotcount; annot++)
            {
                wstring val;
                wostringstream attname;

                pcospath.str(L"");
                pcospath << L"pages[" << page << "]/Annots[" << annot << "]/Subtype";
                val = tet.pcos_get_string(doc, pcospath.str());

                attname << L"page " << static_cast<int>(page + 1) << L", annotation " << static_cast<int>(annot + 1);
                if (val == L"FileAttachment")
                {
                    wostringstream attpath;
                    attpath << L"pages[" << page << L"]/Annots[" << annot << L"]/FS/EF/F";
                    /*
                     * fetch the contents of the attachment and process it
                     */
                    pcospath.str(L"");
                    pcospath << L"type:" << attpath.str();
                    objtype = tet.pcos_get_string(doc, pcospath.str());

                    if (objtype == L"stream")
                    { 
                        int len;
                        wstring text;
                        text = L"----- Page level attachment '" + attname.str() + L"':\n";
                        outfp.write(reinterpret_cast<const char*>(text.c_str()),
                                    text.size() * sizeof(wstring::value_type));
                        
                        const unsigned char *attdata;
                        attdata = tet.pcos_get_stream(doc, &len, L"", pcospath.str());

                        process_document(outfp, L"", attname.str(), attdata, len);
                        text = L"----- End page level attachment '" + attname.str() + L"'\n";
                        outfp.write(reinterpret_cast<const char*>(text.c_str()),
                                    text.size() * sizeof(wstring::value_type));
                    }
                }
            }
        }

        tet.close_document(doc);
    }



int main(int argc, char **argv)
{
    ofstream outfp;
    int pageno = 0;
    int ret = 0;
    
    try
    {
        TET tet;

        if (argc != 3)
        {
            wcerr << L"usage: get_attachments <infilename> <outfilename>" << endl;
            return(2);
        }

        /* get separator in native byte ordering */
        wstring const separator(get_wstring(tet, utf8_separator));

        outfp.open(argv[2], ios::binary);
        if (!outfp.is_open())
        {
            wcerr << L"Couldn't open output file " << argv[2] << endl;
            return(2);
        }
        /* And first write a BOM */
        if (sizeof(wchar_t) == 4)
        {
            unsigned int bom = 0xfeff;
            outfp.write(reinterpret_cast<char*>(&bom), sizeof(wchar_t));
        }
        else
        {
            unsigned short bom = 0xfeff;
            outfp.write(reinterpret_cast<char*>(&bom), sizeof(wchar_t));
        }
        unsigned char *data = NULL;
        process_document(outfp, get_wstring(tet, string(argv[1])), get_wstring(tet, string(argv[1])), data, 0);

        outfp.close();


    }
    catch (TET::Exception &ex) {
        if (pageno == 0)
        {
            wcerr << L"Error " << ex.get_errnum()
                << L" in " << ex.get_apiname()
                << L"(): " << ex.get_errmsg() << endl;
        }
        else
        {
            wcerr << L"Error " << ex.get_errnum()
                << L" in " << ex.get_apiname()
                << L"() on page " << pageno
                << L": " << ex.get_errmsg() << endl;
        }
        return 2;
    }

    return ret;
}

namespace
{

    /*
     * Get a wstring for the given string.
     */
    wstring get_wstring(const TET& tet, const string& utf8_string)
    {
        const size_t size = sizeof(wstring::value_type);
        string wide_string;

        switch (size)
        {
        case 2:
            wide_string = tet.convert_to_unicode(L"auto", utf8_string,
                                                    L"outputformat=utf16");
            break;

        case 4:
            wide_string = tet.convert_to_unicode(L"auto", utf8_string,
                                                    L"outputformat=utf32");
            break;

        default:
            throw std::logic_error("Unsupported wchar_t size");
        }

        return wstring(reinterpret_cast<const wchar_t *>(wide_string.data()),
                wide_string.length() / size);

    } 
}// end anonymous namespace
