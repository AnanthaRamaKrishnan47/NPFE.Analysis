/*
 * Simple PDF text extractor based on PDFlib TET, that uses the tetlibdl.c module
 * to load the TET DLL dynamically.
 *
 * $Id: extractordl.c,v 1.3.24.1 2017/01/09 13:26:59 tm Exp $
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#include "tetlibdl.h"

/* global option list */
static const char *globaloptlist =
    "searchpath={{../data} {../../../resource/cmap}}";

/* document-specific option list */
static const char *docoptlist = "";

/* page-specific option list */
static const char *pageoptlist = "granularity=page";

/* separator to emit after each chunk of text. This depends on the
 * application's needs; for granularity=word a space character may be useful.
 */
#define SEPARATOR  "\n"

int main(int argc, char **argv)
{
    TET *tet;
    const TET_api *tet_api;
    FILE *outfp;
    volatile int pageno = 0;

    if (argc != 3)
    {
        fprintf(stderr, "usage: %s <infilename> <outfilename>\n", argv[0]);
        return(2);
    }

    if ((tet_api = TET_new_dl(&tet)) == (TET_api *) 0)
    {
        fprintf(stderr, "%s: unable to load TET DLL\n", argv[0]);
        return(2);
    }

    if ((outfp = fopen(argv[2], "w")) == NULL)
    {
	fprintf(stderr, "Couldn't open output file '%s'\n", argv[2]);
	tet_api->TET_delete(tet);
	return(2);
    }
    
    TET_TRY_DL(tet_api, tet)
    {
        int n_pages;
        int doc;

        tet_api->TET_set_option(tet, globaloptlist);

        doc = tet_api->TET_open_document(tet, argv[1], 0, docoptlist);

        if (doc == -1)
        {
            fprintf(stderr, "Error %d in %s(): %s\n",
                tet_api->TET_get_errnum(tet), tet_api->TET_get_apiname(tet),
                tet_api->TET_get_errmsg(tet));
            TET_EXIT_TRY_DL(tet_api, tet);
            tet_api->TET_delete(tet);
            return(2);
        }

        /* get number of pages in the document */
        n_pages = (int) tet_api->TET_pcos_get_number(tet, doc, "length:pages");

	/* loop over pages in the document */
        for (pageno = 1; pageno <= n_pages; ++pageno)
        {
            const char *text;
            int page;
            int len;

            page = tet_api->TET_open_page(tet, doc, pageno, pageoptlist);

            if (page == -1)
            {
                fprintf(stderr, "Error %d in %s() on page %d: %s\n",
                    tet_api->TET_get_errnum(tet), tet_api->TET_get_apiname(tet),
                    pageno, tet_api->TET_get_errmsg(tet));
                continue;                        /* try next page */
            }

            /* Retrieve all text fragments; This is actually not required
	     * for granularity=page, but must be used for other granularities.
	     */
            while ((text = tet_api->TET_get_text(tet, page, &len)) != 0)
            {
		fprintf(outfp, "%s", text);  /* print the retrieved text */

		/* print a separator between chunks of text */
		fprintf(outfp, SEPARATOR);
            }

            if (tet_api->TET_get_errnum(tet) != 0)
            {
                fprintf(stderr, "Error %d in %s() on page %d: %s\n",
                    tet_api->TET_get_errnum(tet), tet_api->TET_get_apiname(tet),
                    pageno, tet_api->TET_get_errmsg(tet));
            }

            tet_api->TET_close_page(tet, page);
        }

        tet_api->TET_close_document(tet, doc);
    }
    TET_CATCH_DL(tet_api, tet)
    {
        if (pageno == 0)
            fprintf(stderr, "Error %d in %s(): %s\n",
                tet_api->TET_get_errnum(tet), tet_api->TET_get_apiname(tet),
                tet_api->TET_get_errmsg(tet));
        else
            fprintf(stderr, "Error %d in %s() on page %d: %s\n",
                tet_api->TET_get_errnum(tet), tet_api->TET_get_apiname(tet),
                pageno, tet_api->TET_get_errmsg(tet));
    }

    TET_delete_dl(tet_api, tet);
    fclose(outfp);

    return 0;
}
