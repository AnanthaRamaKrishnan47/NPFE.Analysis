<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' TET sample application for dumping PDF information with pCOS
  '
  ' $Id: dumper.vb.aspx,v 1.11 2010/08/11 13:03:01 rjs Exp $
  '

  Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Dim tet As TET
    Dim s As String
    Dim count, pcosmode, plainmetadata As Integer
    Dim i, doc As Integer
    Dim objtype As String
    Const docoptlist As String = "requiredmode=minimum"
    Const globaloptlist As String = ""
    Dim separator As String = Environment.NewLine()

    Dim searchpath As String = Server.MapPath("data")
    Dim filename As String = "TET-datasheet.pdf"

    tet = New TET()

    Try
        Dim optlist As String

        ' double brackets are necessary for handling paths containing blanks
        optlist = "searchpath={{" + searchpath + "}}"
        tet.set_option(optlist)

        tet.set_option(globaloptlist)

        doc = tet.open_document(filename, docoptlist)
        If doc = -1 Then
            Response.Write("<PRE>")
            Response.Write("Error: " & tet.get_errmsg())
            return
        End If

        ' --------- general information (always available)
        pcosmode = tet.pcos_get_number(doc, "pcosmode")

        s = "   File name: " & tet.pcos_get_string(doc, "filename") & separator

        s = s & " PDF version: " & _
                tet.pcos_get_string(doc, "pdfversionstring") & separator

        s = s & "  Encryption: " & _
                tet.pcos_get_string(doc, "encrypt/description") & separator

        If tet.pcos_get_number(doc, "encrypt/master") <> 0 Then
            s = s & "   Master pw: yes" & separator
        Else
            s = s & "   Master pw: no" & separator
        End If

        If tet.pcos_get_number(doc, "encrypt/user") <> 0 Then
            s = s & "     User pw: yes" & separator
        Else
            s = s & "     User pw: no" & separator
        End If

        If tet.pcos_get_number(doc, "encrypt/nocopy") <> 0 Then
            s = s & "Text copying: no" & separator
        Else
            s = s & "Text copying: yes" & separator
        End If

        If tet.pcos_get_number(doc, "linearized") <> 0 Then
            s = s & "  Linearized: yes" & separator
        Else
            s = s & "  Linearized: no" & separator
        End If

        If pcosmode = 0 Then
            s = s & "Minimum mode: no more information available" & separator
            Response.Write("<PRE>")
            Response.Write(s)
            return
        End If

        ' --------- more details (requires at least user password)
        s = s & "PDF/X status: " & tet.pcos_get_string(doc, "pdfx") & separator

        s = s & "PDF/A status: " & tet.pcos_get_string(doc, "pdfa") & separator

        If tet.pcos_get_number(doc, "type:/Root/AcroForm/XFA") <> 0 Then
            s = s & "    XFA data: yes" & separator
        Else
            s = s & "    XFA data: no" & separator
        End If
              
        If tet.pcos_get_number(doc, "tagged") <> 0 Then
            s = s & "  Tagged PDF: yes" & separator
        Else
            s = s & "  Tagged PDF: no" & separator
        End If

        s = s & separator

        s = s & "No. of pages: " & _
                tet.pcos_get_number(doc, "length:pages") & separator

        s = s & " Page 1 size:" & _
                " width=" & tet.pcos_get_number(doc, "pages[0]/width") & _
                ", height=" & tet.pcos_get_number(doc, "pages[0]/height") & _
                separator

        count = tet.pcos_get_number(doc, "length:fonts")
        s = s & "No. of fonts: " & count & separator

        For i = 0 To count - 1
            Dim fonts As String

            fonts = "fonts[" & i & "]/embedded"

            If tet.pcos_get_number(doc, fonts) <> 0 Then
                s = s & "embedded "
            Else
                s = s & "unembedded "
            End If

            fonts = "fonts[" & i & "]/type"
            s = s & tet.pcos_get_string(doc, fonts) & " font "
            fonts = "fonts[" & i & "]/name"
            s = s & tet.pcos_get_string(doc, fonts) & separator
        Next

        s = s & separator

        plainmetadata = tet.pcos_get_number(doc, "encrypt/plainmetadata")

        If pcosmode = 1 And Not plainmetadata _
                And tet.pcos_get_number(doc, "encrypt/nocopy") Then
            s = s & "Restricted mode: no more information available" & separator
            Response.Write(s)
            return
        End If


        ' ----- document info keys and XMP metadata (requires master pw)
        count = tet.pcos_get_number(doc, "length:/Info")

        For i = 0 To count - 1
            Dim info As String
            Dim key As String

            info = "type:/Info[" & i & "]"
            objtype = tet.pcos_get_string(doc, info)

            info = "/Info[" & i & "].key"
            key = tet.pcos_get_string(doc, info)
            s = s & String.Empty.PadLeft(12 - key.Length) & key & ": "

            ' Info entries can be stored as string or name objects
            If objtype = "name" Or objtype = "string" Then
                info = "/Info[" & i & "]"
                s = s & "'" & tet.pcos_get_string(doc, info) & "'"
            Else
                info = "type:/Info[" & i & "]"
                s = s & "(" & tet.pcos_get_string(doc, info) & " object)"
            End If
            s = s & separator
        Next

        s = s & separator
        s = s & "XMP meta data: "

        objtype = tet.pcos_get_string(doc, "type:/Root/Metadata")
        If objtype = "stream" Then
            Dim contents() As Byte

            ' This returns an array of bytes
            contents = tet.pcos_get_stream(doc, "", "/Root/Metadata")
            s = s & contents.Length & " bytes"

            Dim utf8 As UTF8Encoding = new UTF8Encoding()
            Dim str As String = utf8.GetString(contents)
            s = s & " (" & str.Length & " Unicode characters)" & separator

        Else
            s = s & "not present"
        End If

        Response.Write("<PRE>")
        Response.Write(s)

        tet.close_document(doc)

    Catch ex As TETException
        ' caught exception thrown by TET
        Response.Write("Error " & ex.get_errnum() & " in " & ex.get_apiname() _
            & "(): " & ex.get_errmsg() & "<br><br>")
    Catch ex As System.Exception
	Response.Write("General Exception: " & ex.ToString())
    Finally
        If Not tet Is Nothing Then
            tet.Dispose()
        End If
        Response.End
    End Try
  End Sub
  </script>
</html>
