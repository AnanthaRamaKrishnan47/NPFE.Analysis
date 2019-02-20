// $Id: extractor.cs,v 1.25 2010/08/11 13:03:01 rjs Exp $
//
// PDF text extractor which also searches PDF file attachments.
//

using System;
using System.IO;
using System.Text;
using TET_dotnet;

class get_attachments {
     /**
     * Global option list.
     */
    static String globaloptlist = "searchpath={{../data} " +
			"{../../../resource/cmap}}";

    /**
     * Document specific option list.
     */
    static String docoptlist = "";

    /**
     * Page-specific option list.
     */
    static String pageoptlist = "granularity=page";

    /**
     * Separator to emit after each chunk of text. This depends on the
     * application's needs; for granularity=word a space character may be
     * useful.
     */
    static String separator = "\n";

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
    static void extract_text(TET tet, int doc, BinaryWriter outfp)
    {
        UnicodeEncoding unicode = new UnicodeEncoding(false, true);
        /*
         * Get number of pages in the document.
         */
        int n_pages = (int) tet.pcos_get_number(doc, "length:pages");

        /* loop over pages */
        for (int pageno = 1; pageno <= n_pages; ++pageno)
        {
            String text;
            int page;

            page = tet.open_page(doc, pageno, pageoptlist);

            if (page == -1)
            {
                Console.WriteLine("Error " + tet.get_errnum() + " in  "
                        + tet.get_apiname() + "() on page " + pageno + ": "
                        + tet.get_errmsg());
                continue; /* try next page */
            }

            /*
             * Retrieve all text fragments; This loop is actually not required
             * for granularity=page, but must be used for other granularities.
             */
            while ((text = tet.get_text(page)) != null)
            {
                outfp.Write(unicode.GetBytes(text)); // print the retrieved text

                /* print a separator between chunks of text */
                outfp.Write(unicode.GetBytes(separator));
            }

            if (tet.get_errnum() != 0)
            {
                Console.WriteLine("Error " + tet.get_errnum() + " in  "
                        + tet.get_apiname() + "() on page " + pageno + ": "
                        + tet.get_errmsg());
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
    static int process_document(BinaryWriter outfp, String filename, String realname,
            byte[] data)
    {
        int retval = 0;
        TET tet = null;
        try
        {
            String pvfname = "/pvf/attachment";

            tet = new TET();

            /*
             * Construct a PVF file if data instead of a filename was provided
             */
            if (filename == null || filename.Length == 0)
            {
                tet.create_pvf(pvfname, data, "");
                filename = pvfname;
            }

            tet.set_option(globaloptlist);

            int doc = tet.open_document(filename, docoptlist);

            if (doc == -1)

            {
                Console.WriteLine("Error " + tet.get_errnum() + " in  "
                        + tet.get_apiname() + "() (source: attachment '"
                        + realname + "'): " + tet.get_errmsg());

                retval = 5;
            }
            else
            {
                process_document(outfp, tet, doc);
            }

            /*
             * If there was no PVF file deleting it won't do any harm
             */
            tet.delete_pvf(pvfname);
        }
        catch (TETException e)
        {
            Console.WriteLine("Error " + e.get_errnum() + " in  "
                    + e.get_apiname() + "() (source: attachment '" + realname
                    + "'): " + e.get_errmsg());
            retval = 1;
        }
        catch (Exception e)
        {
            Console.WriteLine("General Exception: " + e.ToString());
            retval = 1;
        }
        finally
        {
            if (tet != null)
            {
                tet.Dispose();
            }
        }


        return retval;
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
    private static void process_document(BinaryWriter outfp, TET tet, int doc){
        String objtype;
        UnicodeEncoding unicode = new UnicodeEncoding(false, true);

        // -------------------- Extract the document's own page contents
        extract_text(tet, doc, outfp);

        // -------------------- Process all document-level file attachments

        // Get the number of document-level file attachments.
        int filecount = (int) tet.pcos_get_number(doc,
                "length:names/EmbeddedFiles");

        for (int file = 0; file < filecount; file++)
        {
            String attname;

            /*
             * fetch the name of the file attachment; check for Unicode file
             * name (a PDF 1.7 feature)
             */
            objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles["
                + file + "]/UF");

            if (objtype == "string")
            {
                attname = tet.pcos_get_string(doc,
                    "names/EmbeddedFiles[" + file + "]/UF");
            }
            else
            {
                objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles["
                        + file + "]/F");
    
                if (objtype == "string")
                {
                    attname = tet.pcos_get_string(doc, "names/EmbeddedFiles["
                            + file + "]/F");
                }
                else
                {
                    attname = "(unnamed)";
                }
            }
            /* fetch the contents of the file attachment and process it */
            objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles["
                    + file + "]/EF/F");

            if (objtype == "stream")
            {
                outfp.Write(unicode.GetBytes("----- File attachment '" + attname + "':\n"));
                byte[] attdata = tet.pcos_get_stream(doc, "",
                        "names/EmbeddedFiles[" + file + "]/EF/F");

                process_document(outfp, null, attname, attdata);
                outfp.Write(unicode.GetBytes("----- End file attachment '" + attname + "'\n"));
            }
        }

        // -------------------- Process all page-level file attachments

        int pagecount = (int) tet.pcos_get_number(doc, "length:pages");

        // Check all pages for annotations of type FileAttachment
        for (int page = 0; page < pagecount; page++)
        {
            int annotcount = (int) tet.pcos_get_number(doc, "length:pages["
                    + page + "]/Annots");

            for (int annot = 0; annot < annotcount; annot++)
            {
                String val;
                String attname;

                val = tet.pcos_get_string(doc, "pages[" + page + "]/Annots["
                        + annot + "]/Subtype");

                attname = "page " + (page + 1) + ", annotation " + (annot + 1);
                if (val == "FileAttachment")
                {
                    String attpath = "pages[" + page
                            + "]/Annots[" + annot + "]/FS/EF/F";
                    /*
                     * fetch the contents of the attachment and process it
                     */
                    objtype = tet.pcos_get_string(doc, "type:" + attpath);

                    if (objtype == "stream")
                    {
                        outfp.Write(unicode.GetBytes("----- Page level attachment '" + attname + "':\n"));
                        byte[] attdata = tet.pcos_get_stream(doc, "", attpath);
                        process_document(outfp, null, attname, attdata);
                        outfp.Write(unicode.GetBytes("----- End page level attachment '" + attname + "'\n"));
                    }
                }
            }
        }

        tet.close_document(doc);
    }
  static int Main(string[] args) {
        FileStream outfile;
        BinaryWriter outfp;
        UnicodeEncoding unicode = new UnicodeEncoding(false, true);
        Byte[] byteOrderMark = unicode.GetPreamble();
        int ret = 0;

        if (args.Length != 2)
        {
            Console.WriteLine("usage: get_attachments <infilename> <outfilename>");
            return(2);
        }

        try
        {

            outfile = File.Create(args.GetValue(1).ToString());
            outfp = new BinaryWriter(outfile);
            outfp.Write(byteOrderMark);


            ret = process_document(outfp, args.GetValue(0).ToString(), args.GetValue(0).ToString(), null);

            outfp.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("General Exception: " + e.ToString());
            ret = 1;
        }

      return(ret);
  }
}
