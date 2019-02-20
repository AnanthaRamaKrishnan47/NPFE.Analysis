' $Id: image_resources.vb,v 1.6 2015/08/06 06:47:09 rp Exp $
'
' Resource-based image extractor based on PDFlib TET
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
        Dim docoptlist As String = ""

        ' page-specific option list, e.g.
        ' "imageanalysis={merge={gap=1} smallimages={maxwidth=20}}"
        Dim pageoptlist As String = ""

        Dim tet As TET
        Dim pageno As Integer = 0

        Dim outfilebase As String

        If (args.Length <> 1) Then
            Console.WriteLine("usage: image_resources <filename>")
            Return (2)
        End If

        outfilebase = args.GetValue(0).ToString()
        If ((outfilebase.Length > 4) And (outfilebase.Substring(outfilebase.Length - 4).Equals(".pdf")) Or (outfilebase.Substring(outfilebase.Length - 4).Equals(".PDF"))) Then
            outfilebase = outfilebase.Substring(0, outfilebase.Length - 4)
        End If

        tet = New TET()

        Try
            Dim n_pages As String

            tet.set_option(globaloptlist)

            Dim doc As String = tet.open_document(args.GetValue(0).ToString(), _
                        docoptlist)

            If (doc = -1) Then
                Console.WriteLine("Error {0} in {1}(): {2}", _
                    tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
                Return (2)
            End If

            ' Get number of pages in the document
            n_pages = CInt(tet.pcos_get_number(doc, "length:pages"))

            ' Loop over all pages to trigger image merging 
            pageno = 1
            Do While pageno <= n_pages
                Dim page As String

                page = tet.open_page(doc, pageno, pageoptlist)

                If (page = -1) Then
                    Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                        tet.get_errnum(), tet.get_apiname(), pageno, _
                        tet.get_errmsg())
                    GoTo [Continue]                        ' process next page
                End If

                If (tet.get_errnum() <> 0) Then
                    Console.WriteLine("Error {0} in {1}(): {2}", _
                        tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
                End If
                tet.close_page(page)

[Continue]:
                pageno += 1
            Loop

            Dim n_images As Integer
            Dim imageid As Integer

            ' Get the number of images in the document
            n_images = CInt(tet.pcos_get_number(doc, "length:images"))

            ' Loop over all image resources
            imageid = 0
            Do While imageid < n_images
                Dim mergetype As Integer
                Dim imageoptlist As String

                ' Skip images which have been consumed by merging
                mergetype = CInt(tet.pcos_get_number(doc, _
                                "images[" & imageid & "]/mergetype"))
                If (mergetype = 2) Then
                    Continue Do
                End If

                ' Skip small images (see "smallimages" option)
                If (tet.pcos_get_number(doc, "images[" & imageid & "]/small") = 1) Then
                    Continue Do
                End If

                ' Report image details: pixel geometry, color space etc.
                report_image_info(tet, doc, imageid)

                '
                ' Fetch the image data and write it to a disk file. The
                ' output filenames are generated from the input
                ' filename by appending page number and image number.
                '/
                imageoptlist = " filename={" & outfilebase & "_I" & imageid & "}"

                If (tet.write_image_file(doc, imageid, imageoptlist) = -1) Then
                    Console.WriteLine( _
                        "Error {0} in {1}(): {2}", _
                        tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
                End If

                imageid += 1
            Loop


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

    Shared Function report_image_info(ByVal tet As TET, ByVal doc As Integer, ByVal imageid As Integer) As Integer
        Dim csname As String
        Dim width, height, bpc, cs, components, mergetype, stencilmask, maskid As Integer

        ' Print the following information for each image:
        ' - image number
        ' - pCOS id (required for indexing the images[] array)
        ' - physical size of the placed image on the page
        ' - pixel size of the underlying PDF Image Object
        ' - number of components, bits per component,and colorspace
        ' - mergetype if different from "normal", i.e. "artificial"
        '   (=merged) or "consumed"
        ' - "stencilmask" property, i.e. /ImageMask in PDF
        ' - pCOS id of mask image, i.e. /Mask or /SMask in PDF

        width = CInt(tet.pcos_get_number(doc, _
                        "images[" & imageid & "]/Width"))
        height = CInt(tet.pcos_get_number(doc, _
                        "images[" & imageid & "]/Height"))
        bpc = CInt(tet.pcos_get_number(doc, _
                        "images[" & imageid & "]/bpc"))
        cs = CInt(tet.pcos_get_number(doc, _
                        "images[" & imageid & "]/colorspaceid"))
        components = CInt(tet.pcos_get_number(doc, _
                        "colorspaces[" & cs & "]/components"))

        Console.Write("image I{0}: {0}x{1} pixel, ", _
                                imageid, width, height)

        csname = tet.pcos_get_string(doc, "colorspaces[" & cs & "]/name")

        Console.Write("{0}x{1} bit {2}", components, bpc, csname)

        If (csname = "Indexed") Then
            Dim basecs As Integer
            Dim basecsname As String
            basecs = tet.pcos_get_number(doc, "colorspaces[" & cs & "]/baseid")
            basecsname = tet.pcos_get_string(doc, "colorspaces[" & basecs & "]/name")
            Console.Write(" " & basecsname)
        End If
        ' Check whether this image has been created by merging smaller images
        mergetype = CInt(tet.pcos_get_number(doc, "images[" & imageid & "]/mergetype"))
        If (mergetype = 1) Then
            Console.Write(", mergetype=artificial")
        End If

        stencilmask = CInt(tet.pcos_get_number(doc, "images[" & imageid & "]/stencilmask"))
        If (stencilmask = 1) Then
            Console.Write(", used as stencil mask")
        End If

        ' Check whether the image has an attached mask 
        maskid = CInt(tet.pcos_get_number(doc, "images[" & imageid & "]/maskid"))
        If (maskid <> -1) Then
            Console.Write(", masked with image " & maskid)
        End If
        Console.WriteLine("")

        Return 0
    End Function
End Class
