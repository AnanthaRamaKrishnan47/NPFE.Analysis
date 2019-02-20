<%@ page language="java" contentType="text/html; charset=ISO-8859-1"
	pageEncoding="ISO-8859-1" isErrorPage="true"
	import="com.pdflib.TETException"%>
<%-- $Id: error.jsp,v 1.1 2009/09/16 08:29:20 stm Exp $ --%>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1">
<title>Error Occurred In TET Sample</title>
</head>
<body>
<%
    if (exception instanceof TETException) {
        TETException tetException = (TETException) exception;
%>
<p>TET exception occurred in TET J2EE sample:</p>
<p>[ <%=tetException.get_errnum()%> ] <%=tetException.get_apiname()%>: <%=tetException.get_errmsg()%></p>
<%
    }
    else {
%>
<p>Exception occurred in TET J2EE sample:</p>
<p>[ <%=exception.toString()%> ]</p>
<%
    }
%>
</body>
</html>