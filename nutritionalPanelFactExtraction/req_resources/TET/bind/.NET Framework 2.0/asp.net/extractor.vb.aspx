<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' $Id: extractor.vb.aspx,v 1.21 2010/08/11 13:03:01 rjs Exp $
  '
  ' PDF text extractor based on PDFlib TET
  '

  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
    ' global option list
    Dim searchpath As String = Server.MapPath("data")

    Dim globaloptlist As String = ""

    ' document-specific  option list
    Dim docoptlist As string = ""

    ' page-specific option list
    Dim pageoptlist As string = "granularity=page"

    ' separator to emit after each chunk of text. This depends on the
    ' applications needs; for granularity=word a space character may be useful.
    Dim separator As String = Environment.NewLine()

    Dim tet As TET
    Dim outfile As FileStream 
    Dim w As BinaryWriter
    Dim pageno As Integer= 0

    Dim infilename As String = "TET-datasheet.pdf"
    Dim outfilename As String = Server.MapPath("TET-datasheet.txt")

    Dim uni_code As UnicodeEncoding = new UnicodeEncoding(false, true)
    Dim byteOrderMark() As Byte = uni_code.GetPreamble()


    outfile = File.Create(outfilename.ToString())
    w = new BinaryWriter(outfile)
    w.Write(byteOrderMark)

    tet = new TET()

    Try
        Dim n_pages As String
        Dim optlist As String

        optlist = "searchpath={{" + searchpath + "}}"
        tet.set_option(optlist)

        tet.set_option(globaloptlist)

        Dim doc As String = tet.open_document(infilename, docoptlist)

        If (doc = -1) Then
            Response.Write("Error " & tet.get_errnum() & " in " _
                & tet.get_apiname() & "(): " & tet.get_errmsg() & "<br><br>")
            Return
        End If

        ' get number of pages in the document
        n_pages = CInt(tet.pcos_get_number(doc, "length:pages"))

        ' loop over pages in the document
        pageno = 1
        Do While pageno <= n_pages
            Dim text As String
            Dim page As String
            Dim imageno As Integer = -1

            page = tet.open_page(doc, pageno, pageoptlist)

            If (page = -1) Then
                Response.Write("Error " & tet.get_errnum() & " in " _
                    & tet.get_apiname() & "()on page " & pageno _
                    & ": " & tet.get_errmsg() & "<br><br>")
                GoTo Continue_mark                        ' try next page
            End If

            ' Retrieve all text fragments; This is actually not required
            ' for granularity=page, but must be used for other
            ' granularities.
            text = tet.get_text(page)
            Do While text <> nothing
                ' print the retrieved text
                w.Write(uni_code.GetBytes(text))

                ' print a separator between chunks of text
                w.Write(uni_code.GetBytes(separator))
                text = tet.get_text(page)
            Loop


            If (tet.get_errnum() <> 0) Then
                Response.Write("Error " & tet.get_errnum() & " in " _
                    & tet.get_apiname() & "(): " & tet.get_errmsg() _
		    & "<br><br>")
            End If
            tet.close_page(page)

Continue_mark:
            pageno += 1
        Loop
        tet.close_document(doc)
    Catch ex As TETException
        ' caught exception thrown by TET
	Response.Write("Error " & ex.get_errnum() & " in " _
                & ex.get_apiname() & "(): " & ex.get_errmsg() & "<br><br>")
    Catch ex As System.Exception
	Response.Write("General Exception: " & ex.ToString())
    Finally
        outfile.Close()
        If Not tet Is Nothing Then
            tet.Dispose()
        End If
        tet = Nothing
    End Try
  End Sub
  </script>
</html>
