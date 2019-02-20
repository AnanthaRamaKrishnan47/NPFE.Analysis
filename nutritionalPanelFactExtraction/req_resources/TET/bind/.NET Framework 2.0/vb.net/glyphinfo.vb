'
' Simple PDF glyph dumper based on PDFlib TET
'
' @version $Id: glyphinfo.vb,v 1.5 2015/08/12 09:29:23 rjs Exp $
'

Imports System
Imports System.IO
Imports System.Text
Imports System.Collections
Imports Microsoft.VisualBasic
Imports TET_dotnet

Class Glyphinfo

    Shared Function print_color_value(ByVal str As String, ByVal tet As TET, ByVal doc As Integer, ByVal colorid As Integer) As String
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

                str = str & String.Format("shading Pattern, ShadingType={0})", shadingtype.toString)
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

            iccprofileid = tet.pcos_get_number(doc, "colorspaces[" & tet.colorspaceid & "]/iccprofileid")

            errormessage = tet.pcos_get_string(doc, "iccprofiles[" & iccprofileid & "]/errormessage")

            ' Check whether the embedded profile is damaged 
            If (errormessage.Equals("")) Then
                str = str & String.Format(" ({0})", errormessage)
            Else
                profilename = tet.pcos_get_string(doc, "iccprofiles[" & iccprofileid & "]/profilename")
                str = str & String.Format(" '{0}'", profilename)

                profilecs = tet.pcos_get_string(doc, "iccprofiles[" & iccprofileid & "]/profilecs")
                str = str & String.Format(" '{0}'", profilecs)
            End If
        ElseIf (csname.Equals("Separation")) Then
            Dim colorantname As String
            colorantname = _
            tet.pcos_get_string(doc, "colorspaces[" & tet.colorspaceid & "]/colorantname")
            str = str & String.Format(" '{0}'", colorantname)
        ElseIf (csname.Equals("DeviceN")) Then
            str = str & String.Format(" ")


            For i = 0 To tet.components.Length
                Dim colorantname As String
                colorantname = tet.pcos_get_string(doc, _
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

    Shared Function Main(ByVal args As String()) As Integer
        ' Global option list.
        Dim globaloptlist As String = "searchpath={{../data} {../../data}}"

        ' Document specific option list.
        Dim docoptlist As String = ""

        ' Page-specific option list.
        Dim pageoptlist As String = "granularity=word"

        Dim outfile As FileStream
        Dim outfp As StreamWriter

        If (args.Length <> 2) Then
            Console.WriteLine("usage: glyphinfo <infilename> <outfilename>")
            Return 2
        End If

        outfile = File.Create(args.GetValue(1).ToString())
        outfp = New StreamWriter(outfile, System.Text.Encoding.UTF8)

        Dim tet As TET = Nothing

        Try
            tet = New TET()
            tet.set_option(globaloptlist)

            Dim doc As Integer = tet.open_document(args(0), docoptlist)
            If (doc = -1) Then
                Console.WriteLine("Error " & tet.get_errnum() & " in " _
                        & tet.get_apiname() & "(): " & tet.get_errmsg())
                Return 1
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
                    Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                        tet.get_errnum(), tet.get_apiname(), pageno, _
                        tet.get_errmsg())
                    GoTo [Continue]                        ' try next page
                End If

                ' Administrative information
                outfp.WriteLine("[ Document: '" & _
                        tet.pcos_get_string(doc, "filename") & "' ]")

                outfp.WriteLine("[ Document options: '" & docoptlist & "' ]")

                outfp.WriteLine("[ Page options: '" & pageoptlist & "' ]")

                outfp.WriteLine("[ ----- Page " & pageno & " ----- ]")

                ' Retrieve all text fragments
                text = tet.get_text(page)
                Do While text <> Nothing
                    ' print the retrieved text
                    outfp.WriteLine("[" & text & "]")

                    ' Loop over all glyphs and print their details
                    Do While (tet.get_char_info(page) <> -1)
                        Dim str As String
                        Dim fontname As String

                        ' Fetch the font name with pCOS (based on its ID)
                        fontname = tet.pcos_get_string(doc, _
                                    "fonts[" & tet.fontid & "]/name")

                        ' Print the character
                        str = "U+" & tet.uv.ToString("X4")

                        ' ...and its UTF8 representation
                        str = str & " '" & ChrW(tet.uv) & "'"

                        ' Print font name, size, and position
                        str = str & String.Format(" {0} size={1} x={2} y={3}", _
                            fontname, tet.fontsize.ToString("f2"), _
                            tet.x.ToString("f2"), tet.y.ToString("f2"))
                        ' print the color id
                        str = str & String.Format(" colorid={0}", tet.colorid)

                        ' check wheather the text color changes 
                        If (tet.colorid <> previouscolorid) Then
                            str = print_color_value(str, tet, doc, tet.colorid)
                            previouscolorid = tet.colorid
                        End If
                        ' Examine the "type" member
                        If (tet.type = 1) Then
                            str = str & " ligature_start"
                        ElseIf (tet.type = 10) Then
                            str = str & " ligature_cont"
                            ' Separators are only inserted for granularity > word
                        ElseIf (tet.type = 12) Then
                            str = str & " inserted"
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
                                str = str & "/sub"
                            End If
                            If ((tet.attributes And ATTR_SUP) = ATTR_SUP) Then
                                str = str & "/sup"
                            End If
                            If ((tet.attributes And ATTR_DROPCAP) = ATTR_DROPCAP) Then
                                str = str & "/dropcap"
                            End If
                            If ((tet.attributes And ATTR_SHADOW) = ATTR_SHADOW) Then
                                str = str & "/shadow"
                            End If
                            If ((tet.attributes And ATTR_DH_PRE) = ATTR_DH_PRE) Then
                                str = str & "/dehyphenation_pre"
                            End If
                            If ((tet.attributes And ATTR_DH_ARTF) = ATTR_DH_ARTF) Then
                                str = str & "/dehyphenation_artifact"
                            End If
                            If ((tet.attributes And ATTR_DH_POST) = ATTR_DH_POST) Then
                                str = str & "/dehyphenation_post"
                            End If
                        End If
                        outfp.WriteLine(str)
                    Loop

                    outfp.WriteLine("")
                    text = tet.get_text(page)
                Loop

                If (tet.get_errnum() <> 0) Then
                    Console.WriteLine("Error {0} in {1}() on page {2}: {3}", _
                        tet.get_errnum(), tet.get_apiname(), pageno, _
                        tet.get_errmsg())
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
End Class
