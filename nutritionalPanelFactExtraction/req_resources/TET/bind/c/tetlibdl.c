/*---------------------------------------------------------------------------*
 |          Copyright (c) 1997-2010 PDFlib GmbH. All rights reserved.        |
 +---------------------------------------------------------------------------+
 |    This software may not be copied or distributed except as expressly     |
 |    authorized by PDFlib GmbH's general license agreement or a custom      |
 |    license agreement signed by PDFlib GmbH.                               |
 |    For more information about licensing please refer to www.pdflib.com.   |
 *---------------------------------------------------------------------------*/

/* $Id: tetlibdl.c,v 1.2.24.2 2017/01/11 08:46:52 stm Exp $
 *
 * C wrapper code for dynamically loading the TET DLL at runtime.
 *
 * This module is not supported on all platforms.
 *
 */

#include <stdlib.h>
#include <stdio.h>

#include "tetlibdl.h"

/* enable this to avoid error messages */
/*
#define PDF_SILENT
*/

#ifdef WIN32

#define TET_DLLNAME                     "libtet.dll"

/* ---------------------------------- MVS ----------------------------- */

#elif defined(__MVS__)

#define TET_DLLNAME                     "TET"

/* ---------------------------------- Linux  ----------------------------- */

#elif defined(__linux__) || defined(linux)

#define TET_DLLNAME                     "libtet.so"

/* ---------------------------------- Mac OS X  ----------------------------- */

#elif defined(__APPLE__)

#define TET_DLLNAME                     "libtet.dylib"

/* ---------------------------------- AS/400  ----------------------------- */

#elif defined __ILEC400__

#define TET_DLLNAME                     "TET"

/* ---------------------------------- unknown  ----------------------------- */

#else

#error No DLL loading code for this platform available!

#endif

#include "dl_dl.h"

static void
tet_dlerror(const char *msg)
{
#ifndef PDF_SILENT
    fputs(msg, stderr);
#endif
}

/* Load the TET DLL and fetch the API structure */
PDFLIB_API const TET_api * PDFLIB_CALL
TET_new_dl(TET **p_tet_obj)
{
    const TET_api *tet_api, *(PDFLIB_CALL *get_api)(void);
    char buf[256];

    /* load the TET DLL... */
    void *handle = pdfdl_dlopen(TET_DLLNAME);

    if (!handle)
    {
	tet_dlerror("Error: couldn't load TET DLL\n");
	return NULL;
    }

    /* ...and fetch function pointers */
    *(void**) (&get_api) = pdfdl_dlsym(handle, "TET_get_api");

    if (get_api == NULL)
    {
	tet_dlerror(
	    "Error: couldn't find function TET_get_api in TET DLL\n");
	pdfdl_dlclose(handle);
	return NULL;
    }

    /* Fetch the API structure. */
    tet_api = (*get_api)();

    /*
     * Check the version number of the loaded DLL against that of
     * the included header file to avoid version mismatch.
     */
    if (tet_api->sizeof_TET_api != sizeof(TET_api) ||
            tet_api->major != TET_MAJORVERSION ||
            tet_api->minor != TET_MINORVERSION) {
	sprintf(buf,
	"Error: loaded wrong version of TET DLL\n"
	"Expected version %d.%d (API size %lu), loaded %d.%d (API size %lu)\n",
	TET_MAJORVERSION, TET_MINORVERSION, (unsigned long) sizeof(TET_api),
	tet_api->major, tet_api->minor,
	(unsigned long) tet_api->sizeof_TET_api);
	tet_dlerror(buf);
	pdfdl_dlclose(handle);
	return NULL;
    }

    /*
     * Create a new TET object; use TET_new2() so that we can store
     * the DLL handle within TET and later retrieve it.
     */
    if ((*p_tet_obj = tet_api->TET_new2(NULL, handle)) == (TET *) NULL)
    {
        tet_dlerror("Couldn't create TET object (out of memory)!\n");
        pdfdl_dlclose(handle);
        return NULL;
    }

    return tet_api;
}

/* delete the TET object and unload the previously loaded TET DLL */
PDFLIB_API void PDFLIB_CALL
TET_delete_dl(const TET_api *tet_api, TET *tet)
{
    if (tet_api && tet)
    {
        /* fetch the DLL handle (previously stored in the TET object) */
        void *handle = tet_api->TET_get_opaque(tet);

        tet_api->TET_delete(tet);

        if (handle)
            pdfdl_dlclose(handle);
    }
}
