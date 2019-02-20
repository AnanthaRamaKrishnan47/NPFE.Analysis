/**
 * Extract text from PDF document as XML. If an output filename is specified,
 * write the XML to the output file. Otherwise fetch the XML in memory, parse it
 * and print some information to System.out.
 * 
 * @version $Id: tetml.cs,v 1.11 2015/07/29 12:32:38 rp Exp $
 */

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections;
using TET_dotnet;

public class tetml
{
  
  public static void Main(String[] args)
  {
      /* Global option list. */
      string globaloptlist = "searchpath={{../data} {../../data}}";

      /* Document specific option list. */
      string basedocoptlist = "";

      /* Page-specific option list. */
      /* Remove the tetml= option if you don't need font and geometry
         information */
      string pageoptlist = "granularity=word tetml={glyphdetails={all}}";

      /* set this to true to generate TETML output in memory */
      bool inmemory = false;

      if (args.Length != 2)
      {
          Console.WriteLine("usage: tetml <pdffilename> <xmlfilename>");
          return;
      }

      TET tet = null;

      try
      {
          String docoptlist;

          tet = new TET();
          tet.set_option(globaloptlist);

          if (inmemory)
          {
              /*
               * This program fetches the TETML data encoded in UTF-8.
               * Subsequently the data is converted to a VisualBasic String,
               * which is encoded in UTF-16.
               * While it is not strictly necessary in case of this program, it
               * is more clean to instruct TET to put 'encoding="UTF-16"' into
               * the XML header.
               */
              docoptlist = "tetml={encodingname=UTF-16} " + basedocoptlist;
          }
          else
          {
              docoptlist = "tetml={filename={" + args[1] + "}} "
                  + basedocoptlist;
          }

          int doc = tet.open_document(args[0], docoptlist);

          if (doc == -1)
          {
              Console.WriteLine("Error " + tet.get_errnum() + " in "
                      + tet.get_apiname() + "(): " + tet.get_errmsg());
              return;
          }

          int n_pages = (int)tet.pcos_get_number(doc, "length:pages");

          /* Loop over pages in the document */
          for (int pageno = 1; pageno <= n_pages; ++pageno)
          {
              tet.process_page(doc, pageno, pageoptlist);
          }

          /* This could be combined with the last page-related call. */
          tet.process_page(doc, 0, "tetml={trailer}");

          if (inmemory)
          {
              /* Get the XML document as a byte array. */
              byte[] tetml = tet.get_tetml(doc, "");

              if (tetml == null)
              {
                  Console.WriteLine("tetml: couldn't retrieve XML data");
                  return;
              }

              /* Process the in-memory XML document to print out some
               * information that is extracted with the sax_handler class.
               */
              XmlDocument xmldoc = new XmlDocument();
              UTF8Encoding utf8_enc = new UTF8Encoding();
              String stetml = utf8_enc.GetString(tetml);
              xmldoc.LoadXml(stetml);

              XmlNodeList nodeList;
              XmlElement root = xmldoc.DocumentElement;

              /* Create an XmlNamespaceManager for resolving namespaces. */
              XmlNamespaceManager nsmgr =
                        new XmlNamespaceManager(xmldoc.NameTable);
              nsmgr.AddNamespace("tet",
                        "http://www.pdflib.com/XML/TET5/TET-5.0");

              nodeList = root.SelectNodes("//tet:Font", nsmgr);
              IEnumerator ienum = nodeList.GetEnumerator();
              while (ienum.MoveNext())
              {
                  XmlNode font = (XmlNode)ienum.Current;
                  XmlAttributeCollection attrColl = font.Attributes;

                  XmlAttribute name_attr =
                        (XmlAttribute)attrColl.GetNamedItem("name");
                  XmlAttribute type_attr =
                        (XmlAttribute)attrColl.GetNamedItem("type");
                  Console.WriteLine("Font " + name_attr.Value + " "
                                + type_attr.Value);
              }
              nodeList = root.SelectNodes("//tet:Word", nsmgr);
              Console.WriteLine("Found " + nodeList.Count
                            + " words in document");
          }

          tet.close_document(doc);
      }
      catch (TETException e)
      {
          Console.WriteLine("Error " + e.get_errnum() + " in "
                  + e.get_apiname() + "(): " + e.get_errmsg());
      }
      catch (Exception e)
      {
          Console.WriteLine("General Exception: " + e.ToString());
      }
      finally
      {
          if (tet != null)
          {
              tet.Dispose();
          }
      }
  }
}
