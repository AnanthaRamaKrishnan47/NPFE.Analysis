@echo off
REM Wrapper script for using the TET command line tool as a filter for
REM PDF documents in Oracle Text.
REM
REM Put this file into %ORACLE_HOME%\bin.
REM
REM $Id: tetfilter.bat,v 1.5 2010/08/11 13:03:03 rjs Exp $

REM Change TETDIR to the installation directory of TET:
SET TETDIR=C:\Program Files\PDFlib\TET 5.0 64-bit

REM Option list for TET_open_document():
SET TETOPT=

REM Option list for TET_open_document():
SET DOCOPT=

REM Option list for TET_open_page() or TET_process_page():
SET PAGEOPT=

"%TETDIR%\bin\tet.exe" --searchpath "%TETDIR%\resource\cmap" ^
	--searchpath "%TETDIR%\resource" ^
	--tetopt "%TETOPT%" ^
	--docopt "%DOCOPT%" ^
	--pageopt "%PAGEOPT%" ^
	-o %2 %1
