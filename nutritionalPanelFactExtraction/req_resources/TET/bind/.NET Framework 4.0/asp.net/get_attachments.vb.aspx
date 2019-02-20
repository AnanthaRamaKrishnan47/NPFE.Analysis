<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="TET_dotnet" %>

<html>
  <script language="VB" runat="server">
      ' $Id: get_attachments.vb.aspx,v 1.3 2017/04/28 13:14:51 rp Exp $
      '
      ' PDF text extractor which also searches PDF file attachments.
      '
      ' Global option list.

      Dim globaloptlist As String = "searchpath={{" & Server.MapPath("data") & "}}"


      ' Document specific option list.
      Dim docoptlist As String = ""


      ' Page-specific option list.
      Dim pageoptlist As String = "granularity=page"

      ' Separator to emit after each chunk of text. This depends on the
      ' application's needs for granularity=word a space character may be
      ' useful.
      Dim separator As String = System.Environment.NewLine

      ' Extract text from a document for which a TET handle is already available.
      ' 
      ' @param tet
      '            The TET object
      ' @param doc
      '            A valid TET document handle
      ' @param outfp
      '            Output file handle
      ' 
      ' @throws TETException
      ' @throws IOException
      '/
      Function extract_text(ByVal tet As TET, ByVal doc As Integer, ByVal outfp As BinaryWriter) As Integer
          Dim unicode As UnicodeEncoding = New UnicodeEncoding(False, True)
          '
          ' Get number of pages in the document.
          '
          Dim n_pages As Integer = CInt(tet.pcos_get_number(doc, "length:pages"))
          Dim pageno As Integer

          ' loop over pages 
          For pageno = 1 To n_pages

              Dim text As String = ""
              Dim page As Integer
              page = tet.open_page(doc, pageno, pageoptlist)

              If (page = -1) Then
                  Response.Write("Error " & tet.get_errnum() & " in  " _
                          & tet.get_apiname() & "() on page " & pageno & ": " _
                          & tet.get_errmsg())
                  Continue For
              End If

              '
              ' Retrieve all text fragments This loop is actually not required
              ' for granularity=page, but must be used for other granularities.
              '
              text = tet.get_text(page)
              Do While text <> Nothing
                  outfp.Write(unicode.GetBytes(text)) ' print the retrieved text

                  ' print a separator between chunks of text '
                  outfp.Write(unicode.GetBytes(separator))
                  text = tet.get_text(page)
              Loop

              If (tet.get_errnum() <> 0) Then
                  Response.Write("Error " + tet.get_errnum() + " in  " _
                          + tet.get_apiname() + "() on page " + pageno + ": " _
                          + tet.get_errmsg())
              End If

              tet.close_page(page)
          Next
          Return Nothing
      End Function

      ''
      ' Open a named physical or virtual file, extract the text from it, search
      ' for document or page attachments, and process these recursively. Either
      ' filename must be supplied for physical files, or data+length from which a
      ' virtual file will be created. The caller cannot create the PVF file since
      ' we create a new TET object here in case an exception happens with the
      ' embedded document - the caller can happily continue with his TET object
      ' even in case of an exception here.
      ' 
      ' @param outfp
      ' @param filename
      ' @param realname
      ' @param data
      ' 
      ' @return 0 if successful, otherwise a non-nothing code to be used as exit
      '         status
      '

      Function process_document(ByVal outfp As BinaryWriter, ByVal filename As String, ByVal realname As String, ByVal data() As Byte) As Integer

          Dim retval As Integer = 0
          Dim tet As TET

          tet = New TET()

          Try
              Dim pvfname As String = "/pvf/attachment"

              '
              ' Construct a PVF file if data instead of a filename was provided
              '
              If (filename = Nothing) Then
                  tet.create_pvf(pvfname, data, "")
                  filename = pvfname
              End If

              tet.set_option(globaloptlist)

              Dim doc As Integer = tet.open_document(filename, docoptlist)

              If (doc = -1) Then
                  Response.Write("Error " & tet.get_errnum() & " in  " _
                          & tet.get_apiname() & "() (source: attachment '" _
                          & realname + "'): " & tet.get_errmsg())

                  retval = 5
              Else
                  process_document(outfp, tet, doc)
              End If

              '
              ' If there was no PVF file deleting it won't do any harm
              '
              tet.delete_pvf(pvfname)
          Catch e As TETException
              ' caught Exception thrown by TET
              Response.Write("Error " & e.get_errnum() & " in " _
                & e.get_apiname() & "(): " & e.get_errmsg() & "<br><br>")
          Catch ee As Exception
              Response.Write("General Exception: " & ee.ToString())
              Return (2)
          Finally
              If Not tet Is Nothing Then
                  tet.Dispose()
              End If
              tet = Nothing
          End Try

          Return retval
      End Function

      ''
      ' Process a single file.
      ' 
      ' @param outfp Output stream for messages
      ' @param tet The TET object
      ' @param doc The TET document handle
      ' 
      ' @throws TETException
      ' @throws IOException
      '
      Function process_document(ByVal outfp As BinaryWriter, ByVal tet As TET, ByVal doc As Integer) As Integer
          Dim objtype As String
          Dim unicode As UnicodeEncoding = New UnicodeEncoding(False, True)

          ' -------------------- Extract the document's own page contents
          extract_text(tet, doc, outfp)

          ' -------------------- Process all document-level file attachments

          ' Get the number of document-level file attachments.
          Dim filecount As Integer = CInt(tet.pcos_get_number(doc, _
                                      "length:names/EmbeddedFiles"))

          For file As Integer = 0 To filecount - 1
              Dim attname As String

              '
              ' fetch the name of the file attachment check for Unicode file
              ' name (a PDF 1.7 feature)
              '
              objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles[" _
                  & file & "]/UF")

              If objtype = "string" Then

                  attname = tet.pcos_get_string(doc,
                      "names/EmbeddedFiles[" & file & "]/UF")
              Else
                  objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles[" _
                          & file & "]/F")

                  If objtype = "string" Then

                      attname = tet.pcos_get_string(doc, "names/EmbeddedFiles[" _
                              & file & "]/F")
                  Else
                      attname = "(unnamed)"
                  End If
              End If
              ' fetch the contents of the file attachment and process it '
              objtype = tet.pcos_get_string(doc, "type:names/EmbeddedFiles[" _
                      & file & "]/EF/F")

              If objtype = "stream" Then
                  Dim attdata() As Byte
                  outfp.Write(unicode.GetBytes("----- File attachment '" & attname & "':" & System.Environment.NewLine))
                  attdata = tet.pcos_get_stream(doc, "",
                          "names/EmbeddedFiles[" & file & "]/EF/F")

                  process_document(outfp, Nothing, attname, attdata)
                  outfp.Write(unicode.GetBytes("----- End file attachment '" & attname & "'" & System.Environment.NewLine))
              End If
          Next

          ' -------------------- Process all page-level file attachments

          Dim pagecount As Integer = CInt(tet.pcos_get_number(doc, "length:pages"))

          ' Check all pages for annotations of type FileAttachment
          For page As Integer = 0 To (pagecount - 1)

              Dim annotcount As Integer = CInt(tet.pcos_get_number(doc, "length:pages[" _
                      & page & "]/Annots"))

              For annot As Integer = 0 To (annotcount - 1)
                  Dim val As String
                  Dim attname As String

                  val = tet.pcos_get_string(doc, "pages[" & page & "]/Annots[" _
                          & annot & "]/Subtype")

                  attname = "page " & (page + 1) & ", annotation " & (annot + 1)
                  If val = "FileAttachment" Then

                      Dim attpath As String = "pages[" + page _
                              & "]/Annots[" & annot & "]/FS/EF/F"
                      '
                      ' fetch the contents of the attachment and process it
                      '
                      objtype = tet.pcos_get_string(doc, "type:" + attpath)

                      If (objtype = "stream") Then
                          Dim attdata() As Byte
                          outfp.Write(unicode.GetBytes("----- Page level attachment '" & attname & "':\n"))
                          attdata = tet.pcos_get_stream(doc, "", attpath)
                          process_document(outfp, Nothing, attname, attdata)
                          outfp.Write(unicode.GetBytes("----- End page level attachment '" & attname & "'\n"))
                      End If
                  End If
              Next
          Next

          tet.close_document(doc)
          Return Nothing
      End Function
      
      Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs)
          Dim outfile As FileStream
          Dim outfp As BinaryWriter
          Dim unicode As UnicodeEncoding = New UnicodeEncoding(False, True)
          
          Dim infilename As String = "Portfolio_sample.pdf"
          Dim outfilename As String = Server.MapPath("attachments.txt")

          Dim byteOrderMark() As Byte
          byteOrderMark = unicode.GetPreamble()
          Dim ret As Integer = 0


          Try
              outfile = File.Create(outfilename)
              outfp = New BinaryWriter(outfile)
              outfp.Write(byteOrderMark)


              ret = process_document(outfp, infilename, infilename, Nothing)

              outfp.Close()
              Response.Write("<a href='attachments.txt'>Data extracted to file attachments.txt</a>")
          Catch ee As Exception
              Response.Write("General Exception: " & ee.ToString())
              ret = 1
          End Try
          
      End Sub
      
            
    </script>
</html>
