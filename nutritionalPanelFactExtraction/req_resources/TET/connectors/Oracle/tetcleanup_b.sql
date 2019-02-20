Rem SQL script for cleaning up the TET test data, example B
Rem $Id: tetcleanup.sql,v 1.2 2008/11/26 10:15:38 stm Exp $
drop index tetindex_b;
execute ctx_ddl.drop_preference('pdf_filter_b')
drop table pdftable_b;
commit;
