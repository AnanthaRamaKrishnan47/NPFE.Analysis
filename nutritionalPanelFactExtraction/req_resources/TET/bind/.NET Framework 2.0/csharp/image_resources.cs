// $Id: image_resources.cs,v 1.6 2015/08/05 13:30:44 rp Exp $
//
// Resource-based image extractor based on PDFlib TET
//

using System;
using System.IO;
using System.Text;
using TET_dotnet;

class image_resources {

  static int Main(string[] args) {

    /* global option list */
    string globaloptlist = "searchpath={{../data} {../../data}}";

    /* document-specific  option list */
    string docoptlist = "";

    /* page-specific option list  e.g
     * "imageanalysis={merge={gap=1} smallimages={maxwidth=20}}"
     */
    string pageoptlist = "";


    TET tet;
    int pageno = 0;
    string outfilebase;

    if (args.Length != 1)
    {
        Console.WriteLine("usage: image_resources <filename>");
        return(2);
    }

    outfilebase = args.GetValue(0).ToString();
    if ((outfilebase.Length > 4) && (outfilebase.Substring(outfilebase.Length - 4).Equals(".pdf")) || (outfilebase.Substring(outfilebase.Length - 4).Equals(".PDF")))
    {
        outfilebase = outfilebase.Substring(0, outfilebase.Length - 4);
      }

    tet = new TET();

    try
    {
        int n_pages;

        tet.set_option(globaloptlist);

        int doc = tet.open_document(args.GetValue(0).ToString(),docoptlist);

        if (doc == -1)
        {
            Console.WriteLine("Error {0} in {1}(): {2}",
                tet.get_errnum(), tet.get_apiname(), tet.get_errmsg());
            return(2);
        }


        /* Images will only be merged upon opening a page.
         * In order to enumerate all merged image resources
         * we open all pages before extracting the images.
         */

        /* get number of pages in the document */
        n_pages = (int) tet.pcos_get_number(doc, "length:pages");

        /* Loop over all pages to trigger image merging */
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
                continue;                        /* process next page */
            }

            if (tet.get_errnum() != 0)
            {
                Console.WriteLine("Error {0} in {1}() on page {2}: {3}",
                    tet.get_errnum(), tet.get_apiname(), pageno,
                    tet.get_errmsg());
            }
            tet.close_page(page);
        }

        int imageid, n_images;

        /* Get the number of images in the document */
        n_images = (int) tet.pcos_get_number(doc, "length:images");

        /* Loop over image resources in the document */
        for (imageid = 0; imageid < n_images; ++imageid)
        {
            string imageoptlist;
            /* Skiop images which have been consumed by merging */
            int mergetype = (int) tet.pcos_get_number(doc,
                                    "images[" + imageid + "]/mergetype");

            if (mergetype == 2)
            {
                continue;
            }

            /* Skip small images (see "smallimages" option) */
            if (tet.pcos_get_number(doc, "images[" + imageid + "]/small") > 0){
                continue;
            }
            /* Report image details: pixel geometry, color space etc . */
            report_image_info(tet, doc, imageid);
            
            /* Write image data to file */
            
            imageoptlist = " filename={" + outfilebase +  "_I" + imageid + "}";

            if (tet.write_image_file(doc, imageid, imageoptlist) == -1)
            {
                Console.WriteLine(
                    "Error {0} in {1}(): {2}",
                    tet.get_errnum(), tet.get_apiname(), tet.get_errmsg());
                continue;                  /* process next image */
            }


        }
        tet.close_document(doc);
    }
    catch (TETException e) {
        /* caught exception thrown by TET */
        Console.WriteLine("Error {0} in {1}(): {2}",
                e.get_errnum(), e.get_apiname(), e.get_errmsg());
        return(2);
    }
    catch (Exception e)
    {
        Console.WriteLine("General Exception: " + e.ToString());
        return(2);
    }
    finally
    {
        if (tet != null) {
            tet.Dispose();
        }
    }

    return 0;
  }

  /* Print the following information for each image:
   * - image number
   * - pCOS id (required for indexing the images[] array)
   * - physical size of the placed image on the page
   * - pixel size of the underlying PDF Image XObject
   * - number of components, bits per component,and colorspace
   * - mergetype if different from "normal", i.e. "artificial"
   *   (=merged) or "consumed"
   *   - "stencilmask" property, i.e. /ImageMask in PDF
   *   - pCOS id of mask image, i.e. /Mask or /SMask
   */
  static void report_image_info(TET tet, int doc, int imageid)
  {

      int width, height, bpc, cs, components, mergetype, stencilmask, maskid;
      String csname;


      width = (int)tet.pcos_get_number(doc,
                      "images[" + imageid + "]/Width");
      height = (int)tet.pcos_get_number(doc,
                      "images[" + imageid + "]/Height");
      bpc = (int)tet.pcos_get_number(doc,
                      "images[" + imageid + "]/bpc");
      cs = (int)tet.pcos_get_number(doc,
                      "images[" + imageid + "]/colorspaceid");
      components = (int)tet.pcos_get_number(doc,
                "colorspaces[" + cs + "]/components");


      Console.Write("image {0}: {1}x{2} pixel, ", imageid, width, height);

      csname = tet.pcos_get_string(doc, "colorspaces[" + cs + "]/name");
      Console.Write( components + "x" + bpc + " bit " + csname);

      if (csname == "Indexed")
      {
          int basecs = 0;
          String basecsname;
          basecs = (int)tet.pcos_get_number(doc,
              "colorspaces[" + cs + "]/baseid");
          basecsname = tet.pcos_get_string(doc,
              "colorspaces[" + basecs + "]/name");
          Console.Write(" " + basecsname);
      }
      /* Check whether this image has been created by merging smaller images*/
        mergetype = (int) tet.pcos_get_number(doc,
            "images[" + imageid + "]/mergetype");
        if (mergetype == 1)
            Console.Write(", mergetype=artificial");

	    stencilmask = (int) tet.pcos_get_number(doc,
            "images[" + imageid + "]/stencilmask");
        if (stencilmask == 1)
            Console.Write(", used as stencil mask");

        /* Check whether the image has an attached mask */
    	maskid = (int) tet.pcos_get_number(doc,
            "images[" + imageid + "]/maskid");
        if (maskid != -1)
            Console.Write(", masked with image " + maskid);

        Console.WriteLine("");
 
  }

}
