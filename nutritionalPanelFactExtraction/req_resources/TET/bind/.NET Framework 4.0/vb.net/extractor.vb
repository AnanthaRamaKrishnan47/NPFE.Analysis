' $Id: extractor.vb,v 1.22.2.1 2010/07/27 12:50:23 rjs Exp $
'
' PDF text extractor based on PDFlib TET
'

Imports System
Imports System.IO
Imports System.Text
Imports TET_dotnet

Class Extractor
  Shared Function Main(ByVal args As String()) As Integer
    ' global option list
    Dim globaloptlist As String = "searchpath={{../data} {../../data}}"

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

    Dim uni_code As UnicodeEncoding = new UnicodeEncoding(false, true)
    Dim byteOrderMark() As Byte = uni_code.GetPreamble()


    If (args.Length <> 2) Then
        Console.WriteLine("usage: extractor <infilename> <outfilename>")
        Return(2)
    End If

    outfile = File.Create(args.GetValue(1).ToString())
    w = new BinaryWriter(outfile)
    w.Write(byteOrderMark)

    tet = new TET()

    Try
        Dim n_pages As String

        tet.set_option(globaloptlist)

        Dim doc As String = tet.open_document(args.GetValue(0).ToString(), _
                    docoptlist)

        If (doc = -1) Then
            Console.WriteLine("Error {0} in {1}(): {2}", _
                tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
            Return(2)
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
                Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                    tet.get_errnum(), tet.get_apiname(), pageno, _
                    tet.get_errmsg())
                GoTo [Continue]                        ' try next page
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
                Console.WriteLine("Error {0} in {1}(): {3}", _
                    tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
            End If
            tet.close_page(page)

[Continue]:
            pageno += 1
        Loop
        tet.close_document(doc)
    Catch e As TETException
        ' caught Exception thrown by TET
	Console.WriteLine("Error {0} in {1}(): {2}", _
                    e.get_errnum(), e.get_apiname(), e.get_errmsg())
    Catch ee As Exception
        Console.WriteLine("General Exception: " & ee.ToString())
        Return (2)
    Finally
	outfile.Close()
	If Not tet Is Nothing Then
	    tet.Dispose()
	End If
	tet = Nothing
    End Try
    Return 0
  End Function
End Class
