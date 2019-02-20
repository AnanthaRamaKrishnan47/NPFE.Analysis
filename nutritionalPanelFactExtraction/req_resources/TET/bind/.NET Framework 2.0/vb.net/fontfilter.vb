'* 
' Extract text from PDF and filter according to font name and size. This can be
' used to identify headings in the document and create a table of contents.
' 
' @version $Id: fontfilter.vb,v 1.9 2008/12/23 17:31:10 rjs Exp $
'/

Imports System
Imports System.IO
Imports TET_dotnet


Class Fontfilter
  Shared Sub Main(ByVal args As String())
    ' Global option list.
    Dim globaloptlist As String = "searchpath={{../data} {../../data}}"

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

    If (args.Length <> 1) Then
        Console.WriteLine("usage: fontfilter <infilename>")
        return
    End If

    tet = New TET()

    try
        tet.set_option(globaloptlist)

        Dim doc as Integer = tet.open_document(Args(0), docoptlist)

        If doc = -1 Then
            Console.WriteLine("Error " & tet.get_errnum() & " in " _
                    & tet.get_apiname() & "(): " & tet.get_errmsg())
            Return
        End If

        ' Loop over pages in the document
        Dim n_pages As Integer

        n_pages = tet.pcos_get_number(doc, "length:pages")
        For pageno = 1 To n_pages Step 1
            Dim page As Integer = tet.open_page(doc, pageno, pageoptlist)

            If page = -1 Then
                Console.WriteLine("Error " & tet.get_errnum() & " in " _
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
                        Console.WriteLine("[{0} {1:0.00}] {2}", fontname, _
                                tet.fontsize, text)
                    End If

                    ' In this sample we check only the first character of
                    ' each fragment.
                    ci = -1
                    'ci = tet.get_char_info(page)
                Loop
                text = tet.get_text(page)
            Loop

            If tet.get_errnum() <> 0 Then
                Console.WriteLine("Error {0} in {1}(): {2}", _
                        tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
            End If

            tet.close_page (page)
        Next

        tet.close_document(doc)

    Catch e As TETException
        if (pageno = 0) Then
            ' caught Exception thrown by TET
            Console.WriteLine("Error {0} in {1}(): {2}", _
                    e.get_errnum(), e.get_apiname(), e.get_errmsg())
        Else
            ' caught Exception thrown by TET
            Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                    e.get_errnum(), e.get_apiname(), pageno, e.get_errmsg())
        End If
        Return
    Catch ee As Exception
        Console.WriteLine("General Exception: " & ee.ToString())
        Return
    Finally
        If Not tet Is Nothing Then
            tet.Dispose()
        End If
        tet = Nothing
    End Try
    Return
  End Sub
End Class
