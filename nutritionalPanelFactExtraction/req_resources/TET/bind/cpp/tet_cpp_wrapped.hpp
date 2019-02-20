

/* Release a document handle and all internal resources related to that document */
void
close_document(int doc)
{
    TETCPP_TRY {
	m_TETlib_api->TET_close_document(tet, doc);
    }
    TETCPP_CATCH;
}


/* Release a page handle and all related resources. */
void
close_page(int page)
{
    TETCPP_TRY {
	m_TETlib_api->TET_close_page(tet, page);
    }
    TETCPP_CATCH;
}


/* Create a named virtual read-only file from data provided in memory. */
void
create_pvf(const pstring& filename, const void * data, size_t size, const pstring& optlist)
{
    std::string filename_param;
    const char *p_filename_param;
    int len_filename;
    param_to_0utf16(filename, filename_param, p_filename_param, len_filename);
    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	m_TETlib_api->TET_create_pvf(tet, p_filename_param, len_filename, data, size, p_optlist_param);
    }
    TETCPP_CATCH;
}


/* Delete a named virtual file and free its data structures. */
int
delete_pvf(const pstring& filename)
{
    volatile int retval = 0;

    std::string filename_param;
    const char *p_filename_param;
    int len_filename;
    param_to_0utf16(filename, filename_param, p_filename_param, len_filename);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_delete_pvf(tet, p_filename_param, len_filename);
    }
    TETCPP_CATCH;

    return retval;
}


/* Get the name of the API function which caused an exception or failed. */
pstring
get_apiname()
{
    const char * volatile retval = NULL;
    pstring pstring_retval;

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_apiname(tet);
    }
    TETCPP_CATCH;

    apiretval_to_pstring(retval, pstring_retval);

    return pstring_retval;
}


/* Get the text of the last thrown exception or the reason for a failed function call. */
pstring
get_errmsg()
{
    const char * volatile retval = NULL;
    pstring pstring_retval;

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_errmsg(tet);
    }
    TETCPP_CATCH;

    apiretval_to_pstring(retval, pstring_retval);

    return pstring_retval;
}


/* Get the number of the last thrown exception or the reason for a failed function call. */
int
get_errnum()
{
    volatile int retval = 0;

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_errnum(tet);
    }
    TETCPP_CATCH;

    return retval;
}


/* Write image data to memory. */
const char *
get_image_data(int doc, size_t *outputlen, int imageid, const pstring& optlist)
{
    const char *volatile retval = NULL;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_image_data(tet, doc, outputlen, imageid, p_optlist_param);
    }
    TETCPP_CATCH;

    return (const char*)retval;
}


/* Get the next text fragment from a page's content. */
pstring
get_text(int page)
{
    const char * volatile retval = NULL;
    int len;
    int *outputlen = &len;
    pstring pstring_retval;

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_text(tet, page, outputlen);
    }
    TETCPP_CATCH;

      if (retval)
      {
          if (conv::do_conversion())
          {
              apiretval_to_pstring(retval, pstring_retval);
          }
          else
          {
              switch (sizeof(typename pstring::value_type))
              {
              case sizeof(char):
                  /*
                   * Legacy case: Put UTF-16 string into std::string.
                   */
                  pstring_retval.assign(
                          reinterpret_cast<
                                const typename pstring::value_type *>(retval),
                          len * 2);
                  break;

              case utf16_wchar_t_size:
              case utf32_wchar_t_size:
                  outputstring_to_pstring(retval, pstring_retval, len);
                  break;

              default:
                  bad_wchar_size("basic_TET<pstring, conv>::get_text");
              }
          }
      }

      return pstring_retval;
}


/* Query properties of a virtual file or the PDFlib Virtual Filesystem (PVF). */
double
info_pvf(const pstring& filename, const pstring& keyword)
{
    volatile double retval = 0;

    std::string filename_param;
    const char *p_filename_param;
    int len_filename;
    param_to_0utf16(filename, filename_param, p_filename_param, len_filename);
    std::string keyword_param;
    const char *p_keyword_param;
    param_to_utf8(keyword, keyword_param, p_keyword_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_info_pvf(tet, p_filename_param, len_filename, p_keyword_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Open a disk-based or virtual PDF document for content extraction. */
int
open_document(const pstring& filename, const pstring& optlist)
{
    volatile int retval = 0;

    std::string filename_param;
    const char *p_filename_param;
    int len_filename;
    param_to_0utf16(filename, filename_param, p_filename_param, len_filename);
    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_open_document(tet, p_filename_param, len_filename, p_optlist_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Open a page for text extraction. */
int
open_page(int doc, int pagenumber, const pstring& optlist)
{
    volatile int retval = 0;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_open_page(tet, doc, pagenumber, p_optlist_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Get the value of a pCOS path with type number or boolean. */
double
pcos_get_number(int doc, const pstring& path)
{
    volatile double retval = 0;

    std::string path_param;
    const char *p_path_param;
    param_to_utf8(path, path_param, p_path_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_pcos_get_number(tet, doc, "%s", p_path_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Get the value of a pCOS path with type name, number, string, or boolean. */
pstring
pcos_get_string(int doc, const pstring& path)
{
    const char * volatile retval = NULL;
    pstring pstring_retval;

    std::string path_param;
    const char *p_path_param;
    param_to_utf8(path, path_param, p_path_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_pcos_get_string(tet, doc, "%s", p_path_param);
    }
    TETCPP_CATCH;

    apiretval_to_pstring(retval, pstring_retval);

    return pstring_retval;
}


/* Get the contents of a pCOS path with type stream, fstream, or string. */
const unsigned char *
pcos_get_stream(int doc, int *outputlen, const pstring& optlist, const pstring& path)
{
    const unsigned char *volatile retval = NULL;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);
    std::string path_param;
    const char *p_path_param;
    param_to_utf8(path, path_param, p_path_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_pcos_get_stream(tet, doc, outputlen, p_optlist_param, "%s", p_path_param);
    }
    TETCPP_CATCH;

    return (const unsigned char*)retval;
}


/* Set one or more global options for TET. */
void
set_option(const pstring& optlist)
{
    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	m_TETlib_api->TET_set_option(tet, p_optlist_param);
    }
    TETCPP_CATCH;
}


/* Write image data to disk. */
int
write_image_file(int doc, int imageid, const pstring& optlist)
{
    volatile int retval = 0;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_write_image_file(tet, doc, imageid, p_optlist_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Process a page and create TETML output. */
int
process_page(int doc, int pageno, const pstring& optlist)
{
    volatile int retval = 0;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_process_page(tet, doc, pageno, p_optlist_param);
    }
    TETCPP_CATCH;

    return retval;
}


/* Deprecated, use TET_get_tetml(). */
PDFLIB_TET_DEPRECATED(
const char *
get_xml_data(int doc, size_t *outputlen, const pstring& optlist)
)
{
    const char *volatile retval = NULL;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_xml_data(tet, doc, outputlen, p_optlist_param);
    }
    TETCPP_CATCH;

    return (const char*)retval;
}


/* Retrieve TETML data from memory. */
const char *
get_tetml(int doc, size_t *outputlen, const pstring& optlist)
{
    const char *volatile retval = NULL;

    std::string optlist_param;
    const char *p_optlist_param;
    param_to_utf8(optlist, optlist_param, p_optlist_param);

    TETCPP_TRY {
	retval = m_TETlib_api->TET_get_tetml(tet, doc, outputlen, p_optlist_param);
    }
    TETCPP_CATCH;

    return (const char*)retval;
}
