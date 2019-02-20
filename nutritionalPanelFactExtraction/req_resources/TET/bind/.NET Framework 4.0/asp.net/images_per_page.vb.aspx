<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="TET_dotnet" %>

<html>

  <script language="VB" runat="server">
  ' $Id: image_extractor.vb.aspx,v 1.2 2013/05/19 16:04:50 rjs Exp $
  '
  ' Page-based image extractor based on PDFlib TET
  '
  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
        ' global option list
        Dim globaloptlist As String
        Dim searchpath As String = Server.MapPath("data")

        ' document-specific  option list
        Dim docoptlist As String = ""

        ' page-specific option list, e.g.
        ' "imageanalysis={merge={gap=1} smallimages={maxwidth=20}}"
        Dim pageoptlist As String = ""

        Dim pageno As Integer = 1

        Dim infilename As String = "TET-datasheet.pdf"

        Dim outfilebase As String

        outfilebase = infilename
        If ((outfilebase.Length > 4) And (outfilebase.Substring(outfilebase.Length - 4).Equals(".pdf")) Or (outfilebase.Substring(outfilebase.Length - 4).Equals(".PDF"))) Then
            outfilebase = outfilebase.Substring(0, outfilebase.Length - 4)
        End If
                

        Dim tet As TET = Nothing

        Try
            Response.Output.Write("<pre>")
            tet = New TET()
            Dim n_pages As String
            globaloptlist = "searchpath={{" + searchpath + "}} "
            tet.set_option(globaloptlist)

            Dim doc As String = tet.open_document(infilename, docoptlist)

            If (doc = -1) Then
                Response.Output.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "(): " & tet.get_errmsg() & "<br/>")
                Return
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
                    Response.Output.Write("Error " &  tet.get_errnum() & " in " & tet.get_apiname() & "() on page " & pageno & ": " & tet.get_errmsg() & "<br/>")
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
                    Response.Output.Write("  placed on page {0} at position ({1}, {2}): {3}x{4}pt, alpha={5}, beta={6}<br/>",
                                      CInt(pageno), tet.x.ToString("f2"), tet.y.ToString("f2"), CInt(tet.width),
                                      CInt(tet.height), tet.alpha, tet.beta)
                    ' Write image data to file 
                    imageoptlist = "filename={" & Server.MapPath(outfilebase) & "_p" & pageno & "_" & imagecount & "_I" & tet.imageid & "}"

                    If (tet.write_image_file(doc, tet.imageid, imageoptlist) = -1) Then
                        Response.Output.Write("Error {0} in {1}(): {2}<br/>",
                        tet.get_errnum(), tet.get_apiname(), tet.get_errmsg())
                        Continue Do
                    End If

                    ' Check whether the image has a mask attached...
                    maskid = CInt(tet.pcos_get_number(doc, "images[" & tet.imageid & "]/maskid"))

                    ' and retrieve it if present 
                    If (maskid <> -1) Then

                        Response.Output.Write("  masked with <br/>")
                        report_image_info(tet, doc, maskid)

                        imageoptlist = "filename={" & outfilebase & "_p" & pageno & "_" & imagecount & "_I" & tet.imageid & "mask_I" & maskid & "}"

                        If (tet.write_image_file(doc, tet.imageid, imageoptlist) = -1) Then
                            Response.Output.Write("Error {0} in {1}() for mask image: {2}<br/>",
                                              tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                            Continue Do
                        End If
                    End If

                    If (tet.get_errnum() <> 0) Then
                        Response.Output.Write("Error {0} in {1}() on page {2}: {3}<br/>",
                            tet.get_errnum(), tet.get_apiname(), pageno, tet.get_errmsg())
                    End If


                Loop
                tet.close_page(page)
                pageno += 1
            Loop

            tet.close_document(doc)
        Catch ex As TETException
            ' caught Exception thrown by TET
            Response.Output.Write("Error {0} in {1}(): {2}<br/>", _
                        ex.get_errnum(), ex.get_apiname(), ex.get_errmsg())
            Return 

        Catch ex As Exception
            Response.Output.Write("General Exception: " & ex.ToString() & "<br/>")
            Return 
        Finally
            If Not tet Is Nothing Then
                tet.Dispose()
            End If
            tet = Nothing
        End Try
        Return 
    End Sub
    private Function report_image_info(ByVal tet As TET, ByVal doc As Integer, ByVal imageid As Integer) As Integer
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

        Response.Output.Write("image I{0}: {0}x{1} pixel, ", _
                                imageid, width, height)

        csname = tet.pcos_get_string(doc, "colorspaces[" & cs & "]/name")

        Response.Output.Write("{0}x{1} bit {2}", components, bpc, csname)

        If (csname = "Indexed") Then
            Dim basecs As Integer
            Dim basecsname As String
            basecs = tet.pcos_get_number(doc, "colorspaces[" & cs & "]/baseid")
            basecsname = tet.pcos_get_string(doc, "colorspaces[" & basecs & "]/name")
            Response.Output.Write(" " & basecsname)
        End If
        ' Check whether this image has been created by merging smaller images
        mergetype = CInt(tet.pcos_get_number(doc, "images[" & imageid & "]/mergetype"))
        If (mergetype = 1) Then
            Response.Output.Write(", mergetype=artificial")
        End If

        stencilmask = CInt(tet.pcos_get_number(doc, "images[" & imageid & "]/stencilmask"))
        If (stencilmask = 1) Then
            Response.Output.Write(", used as stencil mask")
        End If

        Response.Output.Write("<br>")

        Return 0
    End Function


</script>
</html>
