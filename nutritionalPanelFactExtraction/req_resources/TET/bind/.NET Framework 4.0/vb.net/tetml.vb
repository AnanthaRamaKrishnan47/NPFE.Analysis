'
' Extract text from PDF document as XML. If an output filename is specified,
' write the XML to the output file. Otherwise fetch the XML in memory, parse it
' and print some information to System.out.
'
' @version $Id: tetml.vb,v 1.15 2015/07/29 12:32:38 rp Exp $
'

Imports System
Imports System.IO
Imports System.Text
Imports System.Xml
Imports System.Collections
Imports TET_dotnet

Class tetml
  Shared Function Main(ByVal args As String()) As Integer
    ' Global option list.
    Dim globaloptlist As String = "searchpath={{../data} {../../data}}"

    ' Document specific option list.
    Dim basedocoptlist As String = ""

    ' Page-specific option list.
    ' Remove the tetml= option if you don't need font and geometry information
    Dim pageoptlist As String = "granularity=word tetml={glyphdetails={all}}"

    ' set this to true to generate TETML output in memory
    Dim inmemory As boolean = false


    if (args.Length <> 2) Then
      Console.WriteLine("usage: tetml <pdffilename> <xmlfilename>")
      return 2
    End If

    Dim tet As TET = nothing

    Try
        tet = new TET()
        tet.set_option(globaloptlist)

        Dim docoptlist As String
        If inmemory Then
          ' This program fetches the TETML data encoded in UTF-8. Subsequently
          ' the data is converted to a VisualBasic String, which is encoded in
          ' UTF-16.
          ' While it is not strictly necessary in case of this program, it
          ' is more clean to instruct TET to put 'encoding="UTF-16"' into the
          ' XML header.
          docoptlist = "tetml={encodingname=UTF-16} " & basedocoptlist
        Else
          docoptlist = "tetml={filename={" & args(1) & "}} " & basedocoptlist
        End If

        Dim doc As Integer = tet.open_document(args(0), docoptlist)
        If (doc = -1) Then
          Console.WriteLine("Error " & tet.get_errnum() & " in " _
                  & tet.get_apiname() & "(): " & tet.get_errmsg())
          return 1
        End If

        Dim n_pages As Integer = tet.pcos_get_number(doc, "length:pages")

        ' Loop over pages in the document
        Dim pageno As Integer
        For pageno = 1 To n_pages Step 1
          tet.process_page(doc, pageno, pageoptlist)
        Next pageno

        ' This could be combined with the last page-related call.
        tet.process_page(doc, 0, "tetml={trailer}")

        If inmemory Then
          ' Get the XML document as a byte array.
          Dim utf8_enc As UTF8Encoding = new UTF8Encoding()
          Dim tetml() As byte = tet.get_tetml(doc, "")
          Dim stetml As String = utf8_enc.GetString(tetml)

          ' Process the in-memory XML document to print out some
          ' information that is extracted with the sax_handler class.
          Dim xmldoc As XmlDocument = new XmlDocument()
          xmldoc.LoadXml(stetml)

          Dim nodeList As XmlNodeList
          Dim root As XmlElement = xmldoc.DocumentElement

          ' Create an XmlNamespaceManager for resolving namespaces.
          Dim nsmgr As XmlNamespaceManager = _
                    new XmlNamespaceManager(xmldoc.NameTable)
          nsmgr.AddNamespace("tet", _
                    "http://www.pdflib.com/XML/TET5/TET-5.0")

          nodeList = root.SelectNodes("//tet:Font", nsmgr)
          Dim ienum As IEnumerator = nodeList.GetEnumerator()
          Do while (ienum.MoveNext())
              Dim font As XmlNode = ienum.Current
              Dim attrColl As XmlAttributeCollection = font.Attributes

              Dim name_attr As XmlAttribute = attrColl.GetNamedItem("name")
              Dim type_attr As XmlAttribute = attrColl.GetNamedItem("type")
              Console.WriteLine("Font " & name_attr.Value & " " _
                            & type_attr.Value)
          Loop
          nodeList = root.SelectNodes("//tet:Word", nsmgr)
          Console.WriteLine("Found " & nodeList.Count _
                        & " words in document")
        End If

        tet.close_document(doc)
    Catch e As TETException
        ' caught Exception thrown by TET
        Console.WriteLine("Error {0} in {1}(): {2}", _
                e.get_errnum(), e.get_apiname(), e.get_errmsg())
        Return (2)
    Catch ee As Exception
        Console.WriteLine("General Exception: " & ee.ToString())
        Return (2)
    Finally
        If Not tet Is Nothing Then
            tet.Dispose()
        End If
        tet = Nothing
    End Try
    Return 0
  End Function
End Class
