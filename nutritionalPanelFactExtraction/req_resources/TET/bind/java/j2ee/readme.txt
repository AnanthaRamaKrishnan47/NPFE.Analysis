Using TET with J2EE servers 
==============================


Using the JSP and Servlet examples under the "j2ee" subdirectory
================================================================

A prerequisite for running the JSP and Servlet examples in the "j2ee"
directory is that the desired application server has been prepared
for TET usage as described below.


Using TET with J2EE-compliant servers
========================================

Since TET is based on a native library, the JAR file and native library
must be loaded when starting the server and not from individual
server applications.

In order to use TET with J2EE do the following:

- Use the deployment tool to add TET.jar to the project as an external
  JAR. TET.jar must be placed in the %J2EE_HOME%/lib directory;
  It is crucial to place TET.jar into the global class path instead
  of the classpath of the Web application (see below).

- tet_java.dll or libtet_java.so must be accessible in some system path,
  e.g. \winnt\system32 or /usr/local/lib, or via PATH/LD_LIBRARY_PATH, or it
  must be put in a path that is configured for the JVM at startup with the
  "java.library.path" system property.

If the native library is loaded from individual server applications,
an error occurs because the native library must only be loaded
once within the server. In this case you will run into the following
exception:

java.lang.UnsatisfiedLinkError: Native Library
C:\WINNT\system32\tet_java.dll already loaded in another classloader

This is caused by multiple classloaders trying to load the TET class,
and therefore the TET native libary, into the server's address space
simultaneously.


Tomcat
------
For Tomcat consider the following directories for TET.jar: 
bad:    WEB-INF/lib           (inside the web application)
good:   $CATALINA_HOME/lib

Where Tomcat looks for native libraries is platform-dependent. So
either put the TET native library into the default native library search
path of the JVM, or override the search path with the "CATALINA_OPTS"
variable when starting Tomcat, e.g.:

CATALINA_OPTS="-Djava.library.path=<tet-install-dir>/bind/java" \
                <tomcat-install-dir>/bin/catalina.sh run  
Sun Glassfish
-------------
In order to use TET with Glassfish, put the TET shared library or DLL
(e.g. tet_java.dll) and the TET.jar file into the directory
<glassfish-install-dir>/lib. Glassfish will pick up the TET JNI library
from the lib directory without further configuration. 


Jetty
-----
In order to use TET with Jetty 9, put the TET.jar file into the
directory <jetty-install-dir>/lib/ext. The TET shared library or DLL can
be put in any convenient location, but the location must be made known to
Jetty at start-up time. So if the TET shared library or DLL is for 
example also put into <jetty-install-dir>/lib/ext, start Jetty like this:

java -Djava.library.path=<jetty-install-dir>/lib/ext -jar start.jar --module=ext


IBM WebSphere
-------------
To locate the TET.jar file in the app server's classpath, place the jar 
file in the <WAS app-server's path>\lib directory and edit the 
admin.config file.  In the admin.config file add the path to the jar file 
to the setting labeled:

com.ibm.ejs.sm.adminserver.classpath

If you use the Websphere Application Assembly Tool you can add TET.jar
to your project. 

The DLL or .so must be located somewhere on the machine's path.
The winnt\system32 directory works for Windows, the bin directory
of WAS on Solaris.

On iSeries make sure that the TET and TET_JAVA SRVPGM can be
found in the library list of the jobs running your Java apps. The
easiest way to achieve this is to copy these SRVPGMs to the QGPL
library. In most cases this library is found in every LIBL.


Development Tools
=================

Ant
---
With the files under the "j2ee" directory comes an Ant "build.xml" file
that can be used to create a deployable WAR file. For this to work, edit 
build.xml and supply the path to the JAR file that contains the Java
Servlet API in the property named "servlet.jar". Then run "ant". This will
build a WAR file "tet-j2ee-samples.war" that can be deployed to the application
server.


Eclipse
-------
The "j2ee" directory is also prepared to be loaded directly into the Eclipse
IDE as a "Dynamic Web Project". In order to load the project correctly,
the "Eclipse IDE for Java EE Developers" edition must be used:

- Copy the "<tet-install-dir>/bind/data" directory to 
  "<tet-install-dir>/bind/java/j2ee/WebContent/WEB-INF/data".

- Copy the "<tet-install-dir>/resource" directory to 
  "<tet-install-dir>/bind/java/j2ee/WebContent/WEB-INF/resource".

- Copy the file "<tet-install-dir>/bind/xslt/tetml2html.xsl" to 
  "<tet-install-dir>/bind/java/j2ee/WebContent/WEB-INF/tetml2html.xsl".
    
- Open up the desired Eclipse workspace, click "File", "Import...". In the
  dialog that opens, select "General","Existing Projects into Workspace...",
  then click "Next". Check the "Select root directory" option, and browse to
  the directory "<tet-install-dir>/bind/java/j2ee". The 
  "tet-j2ee-samples" project will show up under "Projects:". Check the 
  project and click "Finish".

- The project will show compilation errors on all Java Servlet and JSP files
  because the build path must be configured to include the TET JAR file 
  and the Servlet API. To fix this do the following:
  
  a) Open the Eclipse Preferences, click on "Java", "Build Path", 
     "User Libraries". Click "New" and enter "TET" in the "User library 
     name" field in the "New User Library" dialog that pops up. Click "Ok".
     Keep the "TET" entry selected and click on "Add External Jars...".
     Browse to the directory "<tet-install-dir>/bind/java", select the 
     "TET.jar" file and click "OK".
    
  b) To resolve the references to the Servlet API, add a "Server Runtime 
     Environment" to the Java build path of the "tet-j2ee-samples" project.
     If no server runtime environment is configured yet, this must be done by
     opening the Eclipse preferences, clicking on "Server", "Runtime
     Environments" and by then adding a runtime environment for the desired 
     Java servlet container by clicking "Add...". For adding the desired 
     runtime environment to the Java build path of the "tet-j2ee-samples"
     project, open the properties of the project, click on "Java Build Path",
     switch to the "Libraries" tab, click "Add Library...". In the dialog
     that pops up, select "Server Runtime" and press "Next". In the next
     step, select the desired Server Runtime and press "Finish".

  Make sure the "Build Automatically" option is checked in the "Project"
  pulldown menu. Then after completing steps a) and b) all compile errors 
  should vanish. In the "Java EE" Eclipse perspective, configure a server
  in the "Servers" view, and add the "tet-j2ee-samples" to the server.

  Note that it is still necessary to configure the used application server
  runtime correctly so the TET JAR file and native library can be loaded
  according to the remarks in the section "Using TET with J2EE-compliant
  servers" above.
