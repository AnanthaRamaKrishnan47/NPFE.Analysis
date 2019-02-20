// $Id: image_resources.cs,v 1.6 2015/08/05 13:30:44 rp Exp $
//
// Page-based image extractor based on PDFlib TET
//

using System;
using System.IO;
using System.Text;
using TET_dotnet;

class images_per_page
{

    static int Main(string[] args)
    {
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
            return (2);
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

            int doc = tet.open_document(args.GetValue(0).ToString(), docoptlist);

            if (doc == -1)
            {
                Console.WriteLine("Error {0} in {1}(): {2}",
                    tet.get_errnum(), tet.get_apiname(), tet.get_errmsg());
                return (2);
            }
            /* Get number of pages in the document */
            n_pages = (int)tet.pcos_get_number(doc, "length:pages");

            /* Loop over pages and extract images  */
            for (pageno = 1; pageno <= n_pages; ++pageno)
            {
                int page;
                int imagecount = 0;

                page = tet.open_page(doc, pageno, pageoptlist);

                if (page == -1)
                {
                    Console.WriteLine("Error {0} in {1}() on page {2}: {3}",
                        tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg());
                    continue; /* try next page */
                }

                /*
                 * Retrieve all images on the page 
                 */
                while ((tet.get_image_info(page)) == 1)
                {
                    String imageoptlist;
                    int maskid;

                    imagecount++;

                    /* Report image details: pixel geometry, color space etc. */
                    report_image_info(tet, doc, tet.imageid);

                    /* Report placement geometry */
                    Console.WriteLine("  placed on page " + pageno +
                        " at position (" + tet.x.ToString("f2") + ", " + tet.y.ToString("f2") + "): " +
                        (int)tet.width + "x" + (int)tet.height + "pt, alpha=" + tet.alpha + ", beta=" +
                        tet.beta);
                    /* Write image data to file */
                    imageoptlist = "filename={" + outfilebase + "_p" + pageno + "_" + imagecount + "_I" + tet.imageid + "}";

                    if (tet.write_image_file(doc, tet.imageid, imageoptlist) == -1)
                    {
                        Console.WriteLine("\nError [" + tet.get_errnum() +
                        " in " + tet.get_apiname() + "(): " + tet.get_errmsg());
                        continue; /* try next image */
                    }

                    /* Check whether the image has a mask attached... */
                    maskid = (int)tet.pcos_get_number(doc,
                        "images[" + tet.imageid + "]/maskid");

                    /* and retrieve it if present */
                    if (maskid != -1)
                    {
                        Console.WriteLine("  masked with ");
                        report_image_info(tet, doc, maskid);

                        imageoptlist = "filename={" + outfilebase + "_p" + pageno + "_" + imagecount + "_I" + tet.imageid + "mask_I" + maskid + "}";

                        if (tet.write_image_file(doc, tet.imageid, imageoptlist) == -1)
                        {
                            Console.WriteLine("\nError [" + tet.get_errnum() +
                            " in " + tet.get_apiname() +
                            "() for mask image: " + tet.get_errmsg());
                            continue; /* try next image */
                        }
                    }

                    if (tet.get_errnum() != 0)
                    {
                        Console.WriteLine("Error {0} in {1}() on page {2}: {3}",
                            tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg());
                    }


                }
                tet.close_page(page);
            }

            tet.close_document(doc);
        }
        catch (TETException e)
        {
            /* caught exception thrown by TET */
            Console.WriteLine("Error {0} in {1}(): {2}",
                    e.get_errnum(), e.get_apiname(), e.get_errmsg());
            return (2);
        }
        catch (Exception e)
        {
            Console.WriteLine("General Exception: " + e.ToString());
            return (2);
        }
        finally
        {
            if (tet != null)
            {
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
   */
  static void report_image_info(TET tet, int doc, int imageid)
  {

      int width, height, bpc, cs, components, mergetype, stencilmask;
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

        Console.WriteLine("");
 
  }

}
