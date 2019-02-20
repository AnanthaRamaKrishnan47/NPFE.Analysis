<%@ page language="java" contentType="text/html; charset=UTF-8"
	pageEncoding="UTF-8" 
	import="com.pdflib.TET,com.pdflib.TETException,java.io.PrintWriter,java.io.IOException,java.io.UnsupportedEncodingException" 
	errorPage="error.jsp"%>
	
<%--
	TET JSP sample for dumping PDF information with pCOS
	
	$Id: dumper.jsp,v 1.2 2010/06/25 09:58:12 stm Exp $
--%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
<title>TET J2EE Dumper JSP Example</title>
</head>
<body>
<pre>
<%
	final String filename = "TET-datasheet.pdf";
	
	TET tet = new TET();
	
	String docoptlist = "requiredmode=minimum";
	String globaloptlist = "";
	
	/* This is where input files live. Adjust as necessary. */
	ServletContext context = getServletContext();
	String searchpath = context.getRealPath("/WEB-INF/data");
	
	String optlist;
	
	tet.set_option(globaloptlist);
	
	optlist = "searchpath={{" + searchpath + "}}";
	tet.set_option(optlist);
	
	final int doc = tet.open_document(filename, docoptlist);
	
	if (doc == -1) {%>Error when opening document "<%=filename%>: <%=tet.get_errmsg()%><%}
	else {
	    print_infos(tet, doc, out);
	    tet.close_document(doc);
	}
	
        tet.delete();
%>
<%!
	/**
	 * Print infos about the document.
	 * 
	 * @param tet
	 *            The TET object
	 * @param doc
	 *            The TET document handle
	 * @param writer
	 *            Output stream
	 * 
	 * @throws TETException
         * @throws IOException
	 */
	private static void print_infos(TET tet, int doc, JspWriter writer)
	        throws TETException, IOException {
	    /* --------- general information (always available) */
	    int pcosmode = (int) tet.pcos_get_number(doc, "pcosmode");
	
	    writer.println("   File name: " + tet.pcos_get_string(doc, "filename"));
	
	    writer.println(" PDF version: "
	            + tet.pcos_get_string(doc, "pdfversionstring"));
	
	    writer.println("  Encryption: "
	            + tet.pcos_get_string(doc, "encrypt/description"));
	
	    writer.println("   Master pw: "
	            + (tet.pcos_get_number(doc, "encrypt/master") != 0 ? "yes"
	                    : "no"));
	
	    writer.println("     User pw: "
	                    + (tet.pcos_get_number(doc, "encrypt/user") != 0 ? "yes"
	                            : "no"));
	
	    writer.println("Text copying: "
	            + (tet.pcos_get_number(doc, "encrypt/nocopy") != 0 ? "no"
	                    : "yes"));
	
	    writer.println("  Linearized: "
	            + (tet.pcos_get_number(doc, "linearized") != 0 ? "yes" : "no"));
	
	    if (pcosmode == 0) {
	        writer.println("Minimum mode: no more information available\n\n");
	    }
	    else {
	        print_userpassword_infos(tet, doc, pcosmode, writer);
	    }
	}
	
	/**
	 * Print infos that require at least the user password.
	 * 
	 * @param tet
	 *            The tet object
	 * @param doc
	 *            The tet document handle
	 * @param pcosmode
	 *            The pCOS mode for the document
	 * @param writer
	 *            Output stream
	 * 
	 * @throws TETException
	 * @throws IOException
	 */
	private static void print_userpassword_infos(TET tet, int doc,
	        int pcosmode, JspWriter writer) throws TETException, IOException {
	    writer.println("PDF/X status: " + tet.pcos_get_string(doc, "pdfx"));
	
	    writer.println("PDF/A status: " + tet.pcos_get_string(doc, "pdfa"));
	
	    boolean xfa_present = !tet.pcos_get_string(doc,
	            "type:/Root/AcroForm/XFA").equals("null");
	    writer.println("    XFA data: " + (xfa_present ? "yes" : "no"));
	
	    writer.println("  Tagged PDF: "
	            + (tet.pcos_get_number(doc, "tagged") != 0 ? "yes" : "no"));
	    writer.println();
	
	    writer.println("No. of pages: "
	            + (int) tet.pcos_get_number(doc, "length:pages"));
	
	    writer.println(" Page 1 size: width="
	            + (int) tet.pcos_get_number(doc, "pages[0]/width")
	            + ", height="
	            + (int) tet.pcos_get_number(doc, "pages[0]/height"));
	
	    int count = (int) tet.pcos_get_number(doc, "length:fonts");
	    writer.println("No. of fonts: " + count);
	
	    for (int i = 0; i < count; i++) {
	        if (tet.pcos_get_number(doc, "fonts[" + i + "]/embedded") != 0)
	            writer.print("embedded ");
	        else
	            writer.print("unembedded ");
	
	        writer.print(tet.pcos_get_string(doc, "fonts[" + i + "]/type")
	                + " font ");
	        writer.println(tet.pcos_get_string(doc, "fonts[" + i + "]/name"));
	    }
	
	    writer.println();
	
	    boolean plainmetadata = tet.pcos_get_number(doc,
	            "encrypt/plainmetadata") != 0;
	
	    if (pcosmode == 1 && !plainmetadata
	            && tet.pcos_get_number(doc, "encrypt/nocopy") != 0) {
	        writer.println("Restricted mode: no more information available");
	    }
	    else {
	        print_masterpassword_infos(tet, doc, writer);
	    }
	}
	
	/**
	 * Print document info keys and XMP metadata (requires master pw or
	 * plaintext metadata).
	 * 
         * @param tet
         *            The tet object
         * @param doc
         *            The tet document handle
         * @param writer
         *            Output stream
         * 
         * @throws TETException
         * @throws IOException
	 */
	private static void print_masterpassword_infos(TET tet, int doc,
	        JspWriter writer) throws TETException, IOException {
	    String objtype;
	    int count = (int) tet.pcos_get_number(doc, "length:/Info");
	
	    for (int i = 0; i < count; i++) {
	        objtype = tet.pcos_get_string(doc, "type:/Info[" + i + "]");
	        String key = tet.pcos_get_string(doc, "/Info[" + i + "].key");
	
	        int len = 12 - key.length();
	        while (len-- > 0)
	            writer.print(" ");
	        writer.print(key + ": ");
	
	        /*
	         * Info entries can be stored as string or name objects
	         */
	        if (objtype.equals("string") || objtype.equals("name")) {
	            writer.println("'"
	                    + tet.pcos_get_string(doc, "/Info[" + i + "]") + "'");
	        }
	        else {
	            writer.println("("
	                    + tet.pcos_get_string(doc, "type:/Info[" + i + "]")
	                    + "object)");
	        }
	    }
	
	    writer.println();
	    writer.print("XMP meta data: ");
	
	    objtype = tet.pcos_get_string(doc, "type:/Root/Metadata");
	    if (objtype.equals("stream")) {
	        byte contents[] = tet.pcos_get_stream(doc, "", "/Root/Metadata");
	        writer.print(contents.length + " bytes ");
	
	        try {
	            String string = new String(contents, "UTF-8");
	            writer.println("(" + string.length()
	                            + " Unicode characters)\n");
	        }
	        catch (UnsupportedEncodingException e) {
	            writer.println("Internal error: wrong encoding specified");
	        }
	    }
	    else {
	        writer.println("not present\n\n");
	    }
	}
%>
</pre>
</body>
</html>
