' $Id: images_per_page.vb,v 1.4 2015/08/07 21:47:15 rp Exp $
'
' Page-based image extractor based on PDFlib TET

Imports System
Imports System.IO
Imports System.Text
Imports TET_dotnet

Class images_per_page
    Shared Function Main(ByVal args As String()) As Integer
        ' global option list
        Dim globaloptlist As String = "searchpath={{../data} {../../data}}"

        ' document-specific  option list
        Dim docoptlist As String = ""

        ' page-specific option list, e.g.
        ' "imageanalysis={merge={gap=1} smallimages={maxwidth=20}}"
        Dim pageoptlist As String = ""

        Dim tet As TET
        Dim pageno As Integer = 1

        Dim outfilebase As String

        If (args.Length <> 1) Then
            Console.WriteLine("usage: images_per_pages <filename>")
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

            ' Loop over pages and extract images  
            Do While pageno <= n_pages


                Dim page As Integer
                Dim imagecount As Integer
                imagecount = 0

                page = tet.open_page(doc, pageno, pageoptlist)

                If (page = -1) Then
                    Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                        tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                    Continue Do
                End If


                ' Retrieve all images on the page 

                Do While ((tet.get_image_info(page)) = 1)

                    Dim imageoptlist As String
                    Dim maskid As Integer

                    imagecount = imagecount + 1

                    ' Report image details: pixel geometry, color space etc. 
                    report_image_info(tet, doc, tet.imageid)

                    ' Report placement geometry 
                    Console.WriteLine("  placed on page {0} at position ({1}, {2}): {3}x{4}pt, alpha={5}, beta={6}", _
                                      CInt(pageno), tet.x.ToString("f2"), tet.y.ToString("f2"), CInt(tet.width), _
                                      CInt(tet.height), tet.alpha, tet.beta)
                    ' Write image data to file 
                    imageoptlist = "filename={" & outfilebase & "_p" & pageno & "_" & imagecount & "_I" & tet.imageid & "}"

                    If (tet.write_image_file(doc, tet.imageid, imageoptlist) = -1) Then
                        Console.WriteLine("Error {0} in {1}(): {2}", _
                            tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                        Continue Do
                    End If

                    ' Check whether the image has a mask attached...
                    maskid = CInt(tet.pcos_get_number(doc, "images[" & tet.imageid & "]/maskid"))

                    ' and retrieve it if present 
                    If (maskid <> -1) Then

                        Console.WriteLine("  masked with ")
                        report_image_info(tet, doc, maskid)

                        imageoptlist = "filename={" & outfilebase & "_p" & pageno & "_" & imagecount & "_I" & tet.imageid & "mask_I" & maskid & "}"

                        If (tet.write_image_file(doc, tet.imageid, imageoptlist) = -1) Then
                            Console.WriteLine("Error {0} in {1}() for mask image: {2}", _
                                              tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                            Continue Do
                        End If
                    End If

                    If (tet.get_errnum() <> 0) Then
                        Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                            tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                    End If


                Loop
                tet.close_page(page)
                pageno += 1
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
        Dim width, height, bpc, cs, components, mergetype, stencilmask As Integer

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

        Console.Write("image {0}: {1}x{2} pixel, ", imageid, width, height)

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

        Console.WriteLine("")

        Return 0
    End Function
End Class

