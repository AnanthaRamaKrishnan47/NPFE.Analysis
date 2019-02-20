<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Collections" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' $Id: tetml.vb.aspx,v 1.17 2015/08/01 19:29:50 rjs Exp $
  '
  ' Extract text from PDF document as XML. If an output filename is specified,
  ' write the XML to the output file. Otherwise fetch the XML in memory, parse
  ' it and print some information to System.out.
  '

  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

    Dim searchpath As String = Server.MapPath("data")

    ' Global option list.
    Dim globaloptlist As String = ""

    ' Document specific option list.
    Dim basedocoptlist As String = ""

    ' Page-specific option list.
    ' Remove the tetml= option if you don't need font and geometry information
    Dim pageoptlist As String = "granularity=word tetml={glyphdetails={all}}"

    Dim separator As String = Environment.NewLine()

    Dim pdffilename As String = "TET-datasheet.pdf"
    Dim xmlfilename As String = Server.MapPath("TET-datasheet.tetml")

    Dim inmemory As boolean = 1

    Dim tet As TET = nothing

    Try
        Dim optlist As String

        tet = new TET()

        optlist = "searchpath={{" + searchpath + "}}"
        tet.set_option(optlist)

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
          docoptlist = "tetml={filename={" & xmlfilename & "}} " & basedocoptlist
        End If

        Response.Write("<PRE>")
        if (inmemory) Then
            Response.Write("Processing TETML output for document " _
                & pdffilename & " in memory..." & separator)
        End If

        Dim doc As Integer = tet.open_document(pdffilename, docoptlist)
        If (doc = -1) Then
          Response.Write("Error " & tet.get_errnum() & " in " _
                  & tet.get_apiname() & "(): " & tet.get_errmsg())
          return
        End If

        Dim n_pages As Integer = tet.pcos_get_number(doc, "length:pages")

        ' Loop over pages in the document
        Dim pageno As Integer
        for pageno = 1 To n_pages Step 1
          tet.process_page(doc, pageno, pageoptlist)
        Next pageno

        ' This could be combined with the last page-related call.
        tet.process_page(doc, 0, "tetml={trailer}")

        if (inmemory) Then
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

            'Create an XmlNamespaceManager for resolving namespaces.
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
                Response.Write("Font " & name_attr.Value & " " _
                              & type_attr.Value & separator)
            Loop
            nodeList = root.SelectNodes("//tet:Word", nsmgr)
            Response.Write("Found " & nodeList.Count _
                          & " words in document" & separator)
        End If

        tet.close_document(doc)
    Catch ex As TETException
        ' caught exception thrown by TET
        Response.Write("Error " & ex.get_errnum() & " in " _
            & ex.get_apiname() & "(): " & ex.get_errmsg() & "<br><br>")
        Return
    Catch ex As System.Exception
	Response.Write("General Exception: " & ex.ToString())
    Finally
        If Not tet Is Nothing Then
            tet.Dispose()
        End If
        tet = Nothing
    End Try
  End Sub
  </script>
</html>
