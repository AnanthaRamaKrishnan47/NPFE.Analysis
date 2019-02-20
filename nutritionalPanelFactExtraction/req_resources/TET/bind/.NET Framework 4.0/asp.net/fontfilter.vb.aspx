<%@ Import Namespace="System" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' $Id: fontfilter.vb.aspx,v 1.10 2010/08/11 13:03:01 rjs Exp $
  '
  ' Extract text from PDF and filter according to font name and size. This can
  ' be used to identify headings in the document and create a table of contents.
  '

  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Dim searchpath As String = Server.MapPath("data")

    ' Global option list.
    Dim globaloptlist As String = ""

    ' Document specific option list.
    Dim docoptlist As String = ""

    ' Page-specific option list.
    Dim pageoptlist As String = "granularity=line"

    ' Search text with at least this size (use 0 to catch all sizes).
    Dim fontsizetrigger As String = 10

    ' Catch text where the font name contains this string (use empty string to
    ' catch all font names).
    Dim fontnametrigger As String = "Bold"

    Dim separator As String = Environment.NewLine()

    Dim tet As TET
    Dim pageno As Integer = 0
    Dim infilename As String = "TET-datasheet.pdf"

    try
        Dim optlist As String

        tet = New TET()

        Response.Write("<pre>")
        
        optlist = "searchpath={{" + searchpath + "}}"
        tet.set_option(optlist)


        tet.set_option(globaloptlist)

        Dim doc as Integer = tet.open_document(infilename, docoptlist)

        If doc = -1 Then
            Response.Write("Error " & tet.get_errnum() & " in " _
                    & tet.get_apiname() & "(): " & tet.get_errmsg())
            Return
        End If

        ' Loop over pages in the document
        Dim n_pages As Integer
        Dim result As String

        n_pages = tet.pcos_get_number(doc, "length:pages")
        For pageno = 1 To n_pages Step 1
            Dim page As Integer = tet.open_page(doc, pageno, pageoptlist)

            If page = -1 Then
                Response.Write("Error " & tet.get_errnum() & " in " _
                        & tet.get_apiname() & "(): " & tet.get_errmsg())
                Return ' try next page
            End If

            ' Retrieve all text fragments for the page
            Dim text As String
            text = tet.get_text(page)
            Do While text <> nothing
                ' Loop over all characters
                Dim ci as Integer
                ci = tet.get_char_info(page)
                Do While ci <> -1
                    ' We need only the font name and size the text
                    ' position could be fetched from ci->x and ci->y.
                    Dim fontname As String
                    fontname = tet.pcos_get_string(doc, _
                            "fonts[" & tet.fontid & "]/name")

                    ' Check whether we found a match
                    If tet.FontSize >= fontsizetrigger _
                            And fontname.IndexOf(fontnametrigger) <> -1 Then
                        ' print the retrieved font name, size, and text
                        result = result & "[" & fontname & " " _
                                & tet.FontSize & "] " & text & separator
                    End If

                    ' In this sample we check only the first character of
                    ' each fragment.
                    ci = -1
                    'ci = tet.get_char_info(page)
                Loop
                text = tet.get_text(page)
            Loop

            If tet.get_errnum() <> 0 Then
                Response.Write("Error " & tet.get_errnum() & " in " _
                & tet.get_apiname() & "(): " & tet.get_errmsg() & "<br><br>")
            End If

            tet.close_page (page)
        Next

        Response.Write(result)

        tet.close_document(doc)

    Catch ex As TETException
        ' caught exception thrown by TET
        if (pageno = 0) Then
            Response.Write("Error " & ex.get_errnum() & " in " _
                & ex.get_apiname() & "(): " & ex.get_errmsg() & "<br><br>")
        Else
            Response.Write("Error " & ex.get_errnum() & " in " _
                & ex.get_apiname() & "()on page " & pageno _
                & ": " & ex.get_errmsg() & "<br><br>")
        End If
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
