<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' $Id: image_resources.vb.aspx,v 1.5 2013/09/20 15:47:05 tm Exp $
  '
  ' PDF text extractor based on PDFlib TET
  '

  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
    ' global option list
    Dim globaloptlist As String = ""
    Dim searchpath As String = Server.MapPath("data")

    ' document-specific  option list
    Dim docoptlist As string = ""

    ' page-specific option list
    Dim pageoptlist As string = ""

    ' here you can insert basic image extract options (more below)
    Dim baseimageoptlist As string = ""

    Dim tet As TET
    Dim pageno As Integer= 0

    Dim infilename = "TET-datasheet.pdf"
    Dim outfilebase As String

    outfilebase = Server.MapPath(infilename)

    tet = new TET()
    
    Try
        Dim n_pages As String
        Dim optlist As String

        optlist = "searchpath={{" + searchpath + "}}"
        tet.set_option(optlist)
        tet.set_option(globaloptlist)
        
        Response.Write("<pre>")

        Dim doc As String = tet.open_document(infilename, docoptlist)

        If (doc = -1) Then
           Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "():" & tet.get_errmsg())
           Return 
        End If

        ' Images will only be merged upon opening a page.
        ' In order to enumerate all merged image resources
        ' we open all pages before extracting the images.
        '/

        ' get number of pages in the document
        n_pages = CInt(tet.pcos_get_number(doc, "length:pages"))

        ' loop over pages in the document
        pageno = 1
        Do While pageno <= n_pages
            Dim page As String

            page = tet.open_page(doc, pageno, pageoptlist)

            If (page = -1) Then
                Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "() on page " & pageno & ": " & tet.get_errmsg())
                GoTo [Continue]                        ' try next page
            End If

            If (tet.get_errnum() <> 0) Then
                Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "():" & tet.get_errmsg())
            End If
            tet.close_page(page)

[Continue]:
            pageno += 1
        Loop

        Dim n_images as Integer
        Dim imageid as Integer

	' Get the number of pages in the document.
	' This includes plain images as well as image masks.
        n_images = CInt(tet.pcos_get_number(doc, "length:images"))

        ' loop over image resources in the document
        imageid = 0
        Do While imageid < n_images
            Dim mergetype as Integer

            ' examine image type
            mergetype = CInt(tet.pcos_get_number(doc, _
                            "images[" & imageid & "]/mergetype"))

            ' Retrieve all images on the page
            if (mergetype = 0 or mergetype = 1) Then
                Dim imageoptlist As String
                Dim width, height, bpc, cs As Integer

                ' Print the following information for each image:
                ' - image number
                ' - pCOS id (required for indexing the images[] array)
                ' - physical size of the placed image on the page
                ' - pixel size of the underlying PDF image
                ' - number of components, bits per component,and colorspace
                ' - mergetype if different from "normal", i.e. "artificial"
                '   (=merged) or "consumed"

                width = CInt(tet.pcos_get_number(doc, _
                                "images[" & imageid & "]/Width"))
                height = CInt(tet.pcos_get_number(doc, _
                                "images[" & imageid & "]/Height"))
                bpc = CInt(tet.pcos_get_number(doc, _
                                "images[" & imageid & "]/bpc"))
                cs = CInt(tet.pcos_get_number(doc, _
                                "images[" & imageid & "]/colorspaceid"))

                Response.Write("image I" & imageid & " " & width & "x" & height &" pixel, ")

                if (cs <> -1) Then
                    Response.Write(CInt(tet.pcos_get_number(doc, "colorspaces[" & cs & "]/components")) & "x" & bpc & "bit " & tet.pcos_get_string(doc, "colorspaces[" & cs & "]/name"))
                else
                    ' cs==-1 may happen for some JPEG 2000 images. bpc,
                    ' colorspace name and number of components are not
                    ' available in this case.
                    Response.Write("JPEG2000")
                End If

                ' mergetype==0 means normal image
                if (mergetype <> 0) Then
                    Response.Write(", mergetype=")

                    if (mergetype = 1) Then
                        Response.Write("artificial")
                    else
                        Response.Write("consumed")
                    End If
                End If
                Response.Write("<br>")


                '
                ' Fetch the image data and write it to a disk file. The
                ' output filenames are generated from the input
                ' filename by appending page number and image number.
                '/
                imageoptlist = baseimageoptlist & " filename={" & _
                    outfilebase &  "_p" & pageno & "_" & imageid & "}"

                If (tet.write_image_file(doc, imageid, imageoptlist) = -1) Then
                    Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "():" & tet.get_errmsg())
                End If
            End If

            imageid += 1
        Loop


        tet.close_document(doc)
    Catch ex As TETException
        ' caught exception thrown by TET
        If (pageno = 0)
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
