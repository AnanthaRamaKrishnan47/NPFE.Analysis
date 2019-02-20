/**
 * Extract text from PDF and filter according to font name and size. This can be
 * used to identify headings in the document and create a table of contents.
 * 
 * @version $Id: fontfilter.cs,v 1.8 2009/01/21 20:08:10 rjs Exp $
 */

using System;
using System.IO;
using TET_dotnet;

class fontfilter
{
  public static void Main(String[] args)
  {
      /* Global option list. */
      string globaloptlist = "searchpath={{../data} {../../data}}";

      /* Document specific option list. */
      string docoptlist = "";

      /* Page-specific option list. */
      string pageoptlist = "granularity=line";

      /* Search text with at least this size (use 0 to catch all sizes). */
      double fontsizetrigger = 10;

      /* Catch text where the font name contains this string (use empty string 
       * to catch all font names).
       */
      String fontnametrigger = "Bold";

      TET tet = null;
      int pageno = 0;

      if (args.Length != 1)
      {
          Console.WriteLine("usage: fontfilter <infilename>");
          return;
      }

      try
      {
          tet = new TET();
          tet.set_option(globaloptlist);

          int doc = tet.open_document(args[0], docoptlist);
          if (doc == -1)
          {
              Console.WriteLine("Error " + tet.get_errnum() + " in "
                      + tet.get_apiname() + "(): " + tet.get_errmsg());
              return;
          }

          /* Loop over pages in the document */
          int n_pages = (int) tet.pcos_get_number(doc, "length:pages");
          for (pageno = 1; pageno <= n_pages; ++pageno)
          {
              int page = tet.open_page(doc, pageno, pageoptlist);

              if (page == -1)
              {
                  Console.WriteLine("Error " + tet.get_errnum() + " in "
                          + tet.get_apiname() + "(): " + tet.get_errmsg());
                  return; /* try next page */
              }

              /* Retrieve all text fragments for the page */
              String text;
              while((text = tet.get_text(page)) != null)
              {
                  /* Loop over all characters */
                  int ci;
                  while((ci = tet.get_char_info(page)) != -1)
                  {
                      /* We need only the font name and size; the text
                       * position could be fetched from tet.x and tet.y.
                       */
                      String fontname = tet.pcos_get_string(doc,
                              "fonts[" + tet.fontid + "]/name");

                      /* Check whether we found a match */
                      if (tet.fontsize >= fontsizetrigger
                              && fontname.IndexOf(fontnametrigger) != -1)
                      {
                          /* print the retrieved font name, size, and text */
                          Console.WriteLine("[{0} {1:0.00}] {2}", fontname,
                                tet.fontsize, text);
                      }

                      /* In this sample we check only the first character of
                       * each fragment.
                       */
                      break;
                  }
              }

              if (tet.get_errnum() != 0)
              {
                  Console.WriteLine("Error " + tet.get_errnum() + " in "
                          + tet.get_apiname() + "(): " + tet.get_errmsg());
              }

              tet.close_page(page);
          }

          tet.close_document(doc);
      }
      catch (TETException e)
      {
          if (pageno == 0)
          {
              Console.WriteLine("Error " + e.get_errnum() + " in "
                      + e.get_apiname() + "(): " + e.get_errmsg() + "\n");
          }
          else
          {
              Console.WriteLine("Error " + e.get_errnum() + " in "
                      + e.get_apiname() + "() on page " + pageno + ": "
                      + e.get_errmsg() + "\n");
          }
      }
      catch (Exception e)
      {
          Console.WriteLine("General Exception: " + e.ToString());
      }
      finally
      {
          tet.Dispose();
      }
  }
}
