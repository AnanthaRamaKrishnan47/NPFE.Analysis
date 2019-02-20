<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Collections" %>
<%@ Import Namespace="Microsoft.VisualBasic" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
  ' $Id: glyphinfo.vb.aspx,v 1.7 2015/08/06 10:05:47 rp Exp $
  '
  ' Simple PDF glyph dumper based on PDFlib TET
  '
      Private Function print_color_value(ByVal str As String, ByVal tet As TET, ByVal doc As Integer, ByVal colorid As Integer) As String
          Dim colorinfo As Integer
          Dim csname As String        ' color space name 
          Dim i As Integer

          ' We handle only the fill color, but ignore the stroke color.
          ' The stroke color can be retrieved analogously with the
          ' keyword "stroke".

          colorinfo = tet.get_color_info(doc, colorid, "usage=fill")
          If (tet.colorspaceid = -1 And tet.patternid = -1) Then
              str = str & String.Format(" (not filled)")
              Return str
          End If

          str = str & String.Format(" (")

          If (tet.patternid <> -1) Then
              Dim patterntype As Integer
              patterntype = tet.pcos_get_number(doc, "patterns[" & tet.patternid & "]/PatternType")

              If (patterntype = 1) Then   ' Tiling pattern 
                  Dim painttype As Integer
                  painttype = tet.pcos_get_number(doc, "patterns[" & tet.patternid & "]/PaintType")
                  If (painttype = 1) Then

                      str = str & String.Format("colored Pattern)")
                      Return str
                  ElseIf (painttype = 2) Then
                      str = str & String.Format("uncolored Pattern, base color: ")
                      ' FALLTHROUGH to colorspaceid output 
                  End If
              ElseIf (patterntype = 2) Then ' Shading pattern 
                  Dim shadingtype As Integer
                  shadingtype = tet.pcos_get_number(doc, "patterns[" & tet.patternid & "]/Shading/ShadingType")

                  str = str & String.Format("shading Pattern, ShadingType={0})", shadingtype.ToString)
                  Return str
              End If
          End If

          csname = tet.pcos_get_string(doc, "colorspaces[" & tet.colorspaceid & "]/name")

          str = str & String.Format("{0}", csname)

          ' Emit more details depending on the colorspace type 
          If (csname.Equals("ICCBased")) Then
              Dim iccprofileid As Integer
              Dim profilename As String
              Dim profilecs As String
              Dim errormessage As String

              iccprofileid = tet.pcos_get_number(doc,
                          "colorspaces[" & tet.colorspaceid & "]/iccprofileid")

              errormessage = tet.pcos_get_string(doc,
                      "iccprofiles[" & iccprofileid & "]/errormessage")

              ' Check whether the embedded profile is damaged 
              If (errormessage.Equals("")) Then
                  str = str & String.Format(" ({0})", errormessage)
              Else
                  profilename =
                      tet.pcos_get_string(doc,
                      "iccprofiles[" & iccprofileid & "]/profilename")
                  str = str & String.Format(" '{0}'", profilename)

                  profilecs = tet.pcos_get_string(doc,
                      "iccprofiles[" & iccprofileid & "]/profilecs")
                  str = str & String.Format(" '{0}'", profilecs)
              End If
          ElseIf (csname.Equals("Separation")) Then
              Dim colorantname As String
              colorantname =
              tet.pcos_get_string(doc, "colorspaces[" & tet.colorspaceid & "]/colorantname")
              str = str & String.Format(" '{0}'", colorantname)
          ElseIf (csname.Equals("DeviceN")) Then
              str = str & String.Format(" ")


              For i = 0 To tet.components.Length
                  Dim colorantname As String
                  colorantname = tet.pcos_get_string(doc,
                      "colorspaces[" & tet.colorspaceid & "]/colorantnames[" & i & "]")

                  str = str & String.Format("{0}", colorantname)

                  If (i <> tet.components.Length - 1) Then
                      str = str & String.Format("/")
                  End If
              Next
          ElseIf (csname.Equals("Indexed")) Then
              Dim baseid As Integer
              baseid = tet.pcos_get_number(doc, "colorspaces[" & tet.colorspaceid & "]/baseid")
              csname = tet.pcos_get_string(doc, "colorspaces[" & baseid & "]/name")
              str = str & String.Format(" {0}", csname)
          End If

          str = str & String.Format(" ")
          For i = 0 To tet.components.Length - 1

              str = str & String.Format("{0}", tet.components(i).ToString)

              If (i <> tet.components.Length - 1) Then
                  str = str & String.Format("/")
              End If
          Next
          str = str & String.Format(")")
          Return str
      End Function

      
      Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)

          Dim infilename As String = "TET-datasheet.pdf"

          Dim searchpath As String = Server.MapPath("data")
    
          Dim optlist As String = ""

          ' Document specific option list.
          Dim docoptlist As String = ""

          ' Page-specific option list.
          Dim pageoptlist As String = "granularity=word"

          Const globaloptlist As String = ""

          Dim tet As TET = Nothing

          Try
              tet = New TET()
              ' double brackets are necessary for handling paths containing blanks
              optlist = "searchpath={{" + searchpath + "}}"
              tet.set_option(optlist)

              tet.set_option(globaloptlist)

              Dim doc As Integer = tet.open_document(infilename, docoptlist)
              If (doc = -1) Then
                  Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "():" & tet.get_errmsg())
                  Return
              End If

              Dim n_pages As Integer = tet.pcos_get_number(doc, "length:pages")
              Dim pageno As Integer

              ' loop over pages in the document
              pageno = 1
              Do While pageno <= n_pages
                  Dim text As String
                  Dim page As Integer
                  Dim previouscolorid As Integer
                  previouscolorid = -1

                  page = tet.open_page(doc, pageno, pageoptlist)

                  If (page = -1) Then
                      Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "() on page " & pageno & ": " & tet.get_errmsg())
                      GoTo [Continue]                        ' try next page
                  End If

                  ' Administrative information
                  Response.Write("<html><body><pre>[ Document: '" + _
                          tet.pcos_get_string(doc, "filename") + "' ]<br>")

                  Response.Write("[ Document options: '" + docoptlist + "' ]<br>")

                  Response.Write("[ Page options: '" + pageoptlist + "' ]<br>")

                  Response.Write("[ ----- Page " & pageno & " ----- ]<br>")

                  ' Retrieve all text fragments
                  text = tet.get_text(page)
                  Do While text <> Nothing
                      ' print the retrieved text
                      Response.Write("[" + text + "]<br>")

                      ' Loop over all glyphs and print their details
                      Do While (tet.get_char_info(page) <> -1)
                          Dim str As String
                          Dim fontname As String

                          ' Fetch the font name with pCOS (based on its ID)
                          fontname = tet.pcos_get_string(doc, _
                                      "fonts[" & tet.fontid & "]/name")

                          ' Print the character
                          str = "U+" + tet.uv.ToString("X4")

                          ' ...and its UTF8 representation
                          str = str + " '" + ChrW(tet.uv) + "'"

                          ' Print font name, size, and position
                          str = str + String.Format(" {0} size={1} x={2} y={3}", _
                              fontname, tet.fontsize.ToString("f2"), _
                              tet.x.ToString("f2"), tet.y.ToString("f2"))

                          ' Print the color id
                          str = str & String.Format(" colorid={0}", tet.colorid)

                          ' check wheather the text color changes 
                          If (tet.colorid <> previouscolorid) Then
                              str = print_color_value(str, tet, doc, tet.colorid)
                              previouscolorid = tet.colorid
                          End If
                          
                          ' Examine the "type" member
                          If (tet.type = 1) Then
                              str = str + " ligature_start"
                          ElseIf (tet.type = 10) Then
                              str = str + " ligature_cont"
                              ' Separators are only inserted for granularity > word
                          ElseIf (tet.type = 12) Then
                              str = str + " inserted"
                          End If

                          ' Examine the bit flags in the "attributes" member
                          Const ATTR_NONE As Integer = 0
                          Const ATTR_SUB As Integer = 1
                          Const ATTR_SUP As Integer = 2
                          Const ATTR_DROPCAP As Integer = 4
                          Const ATTR_SHADOW As Integer = 8
                          Const ATTR_DH_PRE As Integer = 16
                          Const ATTR_DH_ARTF As Integer = 32
                          Const ATTR_DH_POST As Integer = 64

                          If (tet.attributes <> ATTR_NONE) Then
                              If ((tet.attributes And ATTR_SUB) = ATTR_SUB) Then
                                  str = str + "/sub"
                              End If
                              If ((tet.attributes And ATTR_SUP) = ATTR_SUP) Then
                                  str = str + "/sup"
                              End If
                              If ((tet.attributes And ATTR_DROPCAP) = ATTR_DROPCAP) Then
                                  str = str + "/dropcap"
                              End If
                              If ((tet.attributes And ATTR_SHADOW) = ATTR_SHADOW) Then
                                  str = str + "/shadow"
                              End If
                              If ((tet.attributes And ATTR_DH_PRE) = ATTR_DH_PRE) Then
                                  str = str + "/dehyphenation_pre"
                              End If
                              If ((tet.attributes And ATTR_DH_ARTF) = ATTR_DH_ARTF) Then
                                  str = str + "/dehyphenation_artifact"
                              End If
                              If ((tet.attributes And ATTR_DH_POST) = ATTR_DH_POST) Then
                                  str = str + "/dehyphenation_post"
                              End If
                          End If
                          Response.Write(str + "<br>")
                      Loop

                      Response.Write("<br>")
                      text = tet.get_text(page)
                  Loop

                  If (tet.get_errnum() <> 0) Then
                      Response.Write("Error " & tet.get_errnum() & " in " & tet.get_apiname() & "() on page " & pageno & ": " & tet.get_errmsg())
                  End If

                  tet.close_page(page)

[Continue]:
                  pageno += 1
              Loop
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
