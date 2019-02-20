/*---------------------------------------------------------------------------*
 |          Copyright (c) 1997-2010 PDFlib GmbH. All rights reserved.        |
 +---------------------------------------------------------------------------+
 |    This software may not be copied or distributed except as expressly     |
 |    authorized by PDFlib GmbH's general license agreement or a custom      |
 |    license agreement signed by PDFlib GmbH.                               |
 |    For more information about licensing please refer to www.pdflib.com.   |
 *---------------------------------------------------------------------------*/

/* $Id: tetlibdl.h,v 1.2.24.2 2017/01/11 08:12:11 stm Exp $
 *
 * Function prototypes for dynamically loading the TET DLL at runtime
 */

#ifdef __cplusplus
typedef struct TET_s TET_cpp;
#define TET TET_cpp
#endif

#include "tetlib.h"

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Notes for using the TET DLL loading mechanism:
 *
 * - TET_TRY_DL()/TET_CATCH_DL() must be used instead of the standard
 *   exception handling macros.
 * - TET_new_dl() must be used instead of TET_new()/TET_new2().
 * - TET_delete_dl() must be used instead of TET_delete().
 * - TET_get_opaque() must not be used.
 */

/* Load the TET DLL, and fetch pointers to all exported functions. */
PDFLIB_API const TET_api * PDFLIB_CALL
TET_new_dl(TET **p_tet);

/* Delete the TET object and unload the previously loaded TET DLL */
PDFLIB_API void PDFLIB_CALL
TET_delete_dl(const TET_api *PDFlib, TET *tet);

#define TET_TRY_DL(TETAPI, TETOBJ)	\
    if (TETOBJ) { if (setjmp(TETAPI->tet_jbuf(TETOBJ)->jbuf) == 0)

/* Inform the exception machinery that a TET_TRY_DL() will be left without
   entering the corresponding TET_CATCH_DL() clause. */
#define TET_EXIT_TRY_DL(TETAPI, TETOBJ) TETAPI->tet_exit_try(TETOBJ)

/* Catch an exception; must always be paired with TET_TRY_DL(). */
#define TET_CATCH_DL(TETAPI, TETOBJ) } if (TETAPI->tet_catch(TETOBJ))

/* Re-throw an exception to another handler. */
#define TET_RETHROW_DL(TETAPI, TETOBJ) TETAPI->tet_rethrow(TETOBJ)

#ifdef __cplusplus
#undef TET
}	/* extern "C" */
#endif
