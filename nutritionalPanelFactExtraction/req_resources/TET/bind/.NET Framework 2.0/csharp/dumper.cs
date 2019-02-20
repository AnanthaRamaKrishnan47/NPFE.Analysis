/* TET sample application for dumping PDF information with pCOS
 *
 * $Id: dumper.cs,v 1.10 2010/01/21 16:02:14 stm Exp $
 */

using System;
using System.IO;
using System.Text;
using TET_dotnet;

class dumper
{
  static int Main(String[] args)
  {
      int exitstat = 0;
      string searchpath = "{../data} {../../data}";

      if (args.Length != 1)
      {
          Console.WriteLine("usage: dumper <filename>");
          exitstat = 2;
      }
      else
      {
          TET tet = null;
          try
          {
              tet = new TET();
              String docoptlist = "requiredmode=minimum";
              String globaloptlist = "";
              String optlist;

              optlist = "searchpath={" + searchpath + "}";
              tet.set_option(optlist);

              tet.set_option(globaloptlist);

              int doc = tet.open_document(args[0], docoptlist);
              if (doc == -1)
              {
                  Console.WriteLine("ERROR: " + tet.get_errmsg());
              }
              else
              {
                  print_infos(tet, doc);
                  tet.close_document(doc);
              }
          }
          catch (TETException e)
          {
              Console.WriteLine("Error " + e.get_errnum() + " in "
                      + e.get_apiname() + "(): " + e.get_errmsg());
              exitstat = 1;
          }
          catch (Exception e)
          {
              Console.WriteLine("General Exception: " + e.ToString());
              exitstat = 1;
          }
          finally
          {
              if (tet != null)
              {
                  tet.Dispose();
              }
          }
      }

      return(exitstat);
  }

  /**
   * Print infos about the document.
   * 
   * @param tet The TET object
   * @param doc The TET document handle
   * 
   * @throws TETException
   */
  private static void print_infos(TET tet, int doc)
  {
      /* --------- general information (always available) */
      int pcosmode = (int) tet.pcos_get_number(doc, "pcosmode");

      Console.WriteLine("   File name: "
              + tet.pcos_get_string(doc, "filename"));

      Console.WriteLine(" PDF version: " 
          + tet.pcos_get_string(doc, "pdfversionstring"));

      Console.WriteLine("  Encryption: "
          + tet.pcos_get_string(doc, "encrypt/description"));

      Console.WriteLine("   Master pw: "
          + (tet.pcos_get_number(doc, "encrypt/master") != 0 ? "yes" : "no"));

      Console.WriteLine("     User pw: "
          + (tet.pcos_get_number(doc, "encrypt/user") != 0 ? "yes" : "no"));

      Console.WriteLine("Text copying: "
          + (tet.pcos_get_number(doc, "encrypt/nocopy") != 0 ? "no" : "yes"));

      Console.WriteLine("  Linearized: "
          + (tet.pcos_get_number(doc, "linearized") != 0 ? "yes" : "no"));

      if (pcosmode == 0)
      {
          Console.WriteLine("Minimum mode: no more information available\n\n");
      }
      else
      {
          print_userpassword_infos(tet, doc, pcosmode);
      }
  }

  /**
   * Print infos that require at least the user password.
   * 
   * @param tet The tet object
   * @param doc The tet document handle
   * @param pcosmode The pCOS mode for the document
   * 
   * @throws TETException
   */
  private static void print_userpassword_infos(TET tet, int doc, int pcosmode)
  {
      Console.WriteLine("PDF/X status: " + tet.pcos_get_string(doc, "pdfx"));

      Console.WriteLine("PDF/A status: " + tet.pcos_get_string(doc, "pdfa"));

      Console.WriteLine("    XFA data: "
              + (tet.pcos_get_number(doc, "type:/Root/AcroForm/XFA") != 0 ? "yes" : "no"));

      Console.WriteLine("  Tagged PDF: "
              + (tet.pcos_get_number(doc, "tagged") != 0 ? "yes" : "no"));
      Console.WriteLine();

      Console.WriteLine("No. of pages: "
              + (int) tet.pcos_get_number(doc, "length:pages"));

      Console.WriteLine(" Page 1 size: width="
              + tet.pcos_get_number(doc, "pages[0]/width") + ", height="
              + tet.pcos_get_number(doc, "pages[0]/height"));

      int count = (int) tet.pcos_get_number(doc, "length:fonts");
      Console.WriteLine("No. of fonts: " + count);

      for (int i = 0; i < count; i++)
      {
          if (tet.pcos_get_number(doc, "fonts[" + i + "]/embedded") != 0)
              Console.Write("embedded ");
          else
              Console.Write("unembedded ");

          Console.Write(tet
                  .pcos_get_string(doc, "fonts[" + i + "]/type")
                  + " font ");
          Console.WriteLine(tet
                  .pcos_get_string(doc, "fonts[" + i + "]/name"));
      }

      Console.WriteLine();

      bool plainmetadata =
          tet.pcos_get_number(doc, "encrypt/plainmetadata") != 0;

      if (pcosmode == 1 && !plainmetadata
              && tet.pcos_get_number(doc, "encrypt/nocopy") != 0)
      {
          Console.WriteLine("Restricted mode: no more information available");
      }
      else
      {
          print_masterpassword_infos(tet, doc);
      }
  }

  /**
   * Print document info keys and XMP metadata (requires master pw or
   * plaintext metadata).
   * 
   * @param tet
   * @param doc
   * @throws TETException
   */
  private static void print_masterpassword_infos(TET tet, int doc)
  {
      String objtype;
      int count = (int) tet.pcos_get_number(doc, "length:/Info");

      for (int i = 0; i < count; i++)
      {
          objtype = tet.pcos_get_string(doc, "type:/Info[" + i + "]");
          String key = tet.pcos_get_string(doc, "/Info[" + i + "].key");
          Console.Write(String.Empty.PadLeft(12 - key.Length) + key + ": ");

          /* Info entries can be stored as string or name objects */
          if (objtype == "string" || objtype == "name")
          {
              Console.WriteLine("'"
                      + tet.pcos_get_string(doc, "/Info[" + i + "]") + "'");
          } else {
              Console.WriteLine("("
                      + tet.pcos_get_string(doc, "type:/Info[" + i + "]")
                      + "object)");
          }
      }

      Console.WriteLine();
      Console.Write("XMP meta data: ");

      objtype = tet.pcos_get_string(doc, "type:/Root/Metadata");
      if (objtype == "stream")
      {
          byte[] contents = tet.pcos_get_stream(doc, "", "/Root/Metadata");
          Console.Write(contents.Length + " bytes ");

          UTF8Encoding utf8 = new UTF8Encoding();
          String str = utf8.GetString(contents);
          Console.WriteLine("(" + str.Length
                      + " Unicode characters)");
      }
      else
      {
          Console.WriteLine("not present\n\n");
      }
  }
}
