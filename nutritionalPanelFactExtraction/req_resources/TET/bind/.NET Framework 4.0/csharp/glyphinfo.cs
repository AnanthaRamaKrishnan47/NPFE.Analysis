/**
 * Simple PDF glyph dumper based on PDFlib TET
 * 
 * @version $Id: glyphinfo.cs,v 1.5 2015/08/03 11:27:01 rp Exp $
 */

using System;
using System.IO;
using System.Text;
using TET_dotnet;

public class tetml
{
    public static String print_color_value(string str, TET tet, int doc, int colorid) {
        int colorinfo;
        String csname;			/* color space name */
        int i;

        /* We handle only the fill color, but ignore the stroke color.
         * The stroke color can be retrieved analogously with the
         * keyword "stroke".
         */
        colorinfo = tet.get_color_info(doc, colorid, "usage=fill");
        if (tet.colorspaceid == -1 && tet.patternid == -1)
        {
            str = str + String.Format(" (not filled)");
            return str;
        }

        str = str + String.Format(" (");

        if (tet.patternid != -1)
        {
            int patterntype =
            (int)tet.pcos_get_number(doc, "patterns[" + tet.patternid + "]/PatternType");

            if (patterntype == 1)	/* Tiling pattern */
            {
                int painttype =
                    (int)tet.pcos_get_number(doc, "patterns[" + tet.patternid + "]/PaintType");
                if (painttype == 1)
                {
                    str = str + String.Format("colored Pattern)");
                    return str;
                }
                else if (painttype == 2)
                {
                    str = str + String.Format("uncolored Pattern, base color: ");
                    /* FALLTHROUGH to colorspaceid output */
                }
            }
            else if (patterntype == 2)	/* Shading pattern */
            {
                int shadingtype =
                    (int)tet.pcos_get_number(doc,
                        "patterns[" + tet.patternid + "]/Shading/ShadingType");

                str = str + String.Format("shading Pattern, ShadingType={0})", shadingtype);
                return str;
            }
        }

        csname = tet.pcos_get_string(doc, "colorspaces[" + tet.colorspaceid + "]/name");

        str = str + String.Format("{0}", csname);

        /* Emit more details depending on the colorspace type */
        if (csname.Equals("ICCBased"))
        {
            int iccprofileid;
            String profilename;
            String profilecs;
            String errormessage;

            iccprofileid = (int)tet.pcos_get_number(doc,
                        "colorspaces[" + tet.colorspaceid + "]/iccprofileid");

            errormessage = tet.pcos_get_string(doc,
                    "iccprofiles[" + iccprofileid + "]/errormessage");

            /* Check whether the embedded profile is damaged */
            if (errormessage.Equals(""))
            {
                str = str + String.Format(" ({0})", errormessage);
            }
            else
            {
                profilename =
                    tet.pcos_get_string(doc,
                    "iccprofiles[" + iccprofileid + "]/profilename");
                str = str + String.Format(" '{0}'", profilename);

                profilecs = tet.pcos_get_string(doc,
                    "iccprofiles[" + iccprofileid + "]/profilecs");
                str = str + String.Format(" '{0}'", profilecs);
            }
        }
        else if (csname.Equals("Separation"))
        {
            String colorantname =
            tet.pcos_get_string(doc, "colorspaces[" + tet.colorspaceid + "]/colorantname");
            str = str + String.Format(" '{0}'", colorantname);
        }
        else if (csname.Equals("DeviceN"))
        {
            str = str + String.Format(" ");

            for (i = 0; i < tet.components.Length; i++)
            {
                String colorantname =
                    tet.pcos_get_string(doc,
                    "colorspaces[" + tet.colorspaceid + "]/colorantnames[" + i + "]");

                str = str + String.Format("{0}", colorantname);

                if (i != tet.components.Length - 1)
                    str = str + String.Format("/");
            }
        }
        else if (csname.Equals("Indexed"))
        {
            int baseid =
            (int)tet.pcos_get_number(doc, "colorspaces[" + tet.colorspaceid + "]/baseid");

            csname = tet.pcos_get_string(doc, "colorspaces[" + baseid + "]/name");

            str = str + String.Format(" {0}", csname);

        }

        str = str + String.Format(" ");
        for (i = 0; i < tet.components.Length; i++)
        {
            str = str + String.Format("{0}", tet.components[i]);

            if (i != tet.components.Length - 1)
                str = str + String.Format("/");
        }
        str = str + String.Format(")");
        return str;
    }


    public static void Main(String[] args)
    {
        /* Global option list. */
        string globaloptlist = "searchpath={{../data} {../../data}}";

        /* Document specific option list. */
        string docoptlist = "";

        /* Page-specific option list. */
        string pageoptlist = "granularity=word";

        FileStream outfile;
        StreamWriter outfp;

        if (args.Length != 2)
        {
            Console.WriteLine("usage: glyphinfo <infilename> <outfilename>");
            return;
        }

        outfile = File.Create(args.GetValue(1).ToString());
        outfp = new StreamWriter(outfile, System.Text.Encoding.UTF8);

        TET tet = null;

        try
        {
            tet = new TET();

            tet.set_option(globaloptlist);

            int doc = tet.open_document(args[0], docoptlist);

            if (doc == -1)
            {
                Console.WriteLine("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "(): " + tet.get_errmsg());
                return;
            }

            /* get number of pages in the document */
            int n_pages = (int)tet.pcos_get_number(doc, "length:pages");

            /* Loop over pages in the document */
            for (int pageno = 1; pageno <= n_pages; ++pageno)
            {
                string text;
                int page;
                int previouscolor = -1;

                page = tet.open_page(doc, pageno, pageoptlist);

                if (page == -1)
                {
                    Console.WriteLine("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "() on page " 
                        + pageno + ": " + tet.get_errmsg());
                    continue;                        /* try next page */
                }

                /* Administrative information */
                outfp.WriteLine("[ Document: '" +
                        tet.pcos_get_string(doc, "filename") + "' ]");

                outfp.WriteLine("[ Document options: '" + docoptlist+ "' ]");

                outfp.WriteLine("[ Page options: '" + pageoptlist + "' ]");

                outfp.WriteLine("[ ----- Page " + pageno + " ----- ]");

                /* Retrieve all text fragments */
                while ((text = tet.get_text(page)) != null)
                {
                    /* print the retrieved text */
                    outfp.WriteLine("[" + text + "]");

                    /* Loop over all glyphs and print their details */
                    while (tet.get_char_info(page) != -1)
                    {
                        string str;
                        string fontname;

                        /* Fetch the font name with pCOS (based on its ID) */
                        fontname = tet.pcos_get_string(doc,
                                    "fonts[" + tet.fontid + "]/name");

                        /* Print the character */
                        str = String.Format("U+{0}", tet.uv.ToString("X4"));

                        /* ...and its UTF8 representation */
                        str = str + String.Format(" '" + (char)(tet.uv) + "'");

                        /* Print font name, size, and position */
                        str = str + String.Format( " {0} size={1} x={2} y={3}",
                            fontname, tet.fontsize.ToString("f2"),
                            tet.x.ToString("f2"), tet.y.ToString("f2"));
                        /* Print the color id */
                        str = str + String.Format(" colorid={0}", tet.colorid);

                        /* check wheather the text color changes */
                        if (tet.colorid != previouscolor)
                        {
                             str = print_color_value(str, tet, doc, tet.colorid);
                            previouscolor = tet.colorid;
                        }
                        /* Examine the "type" member */
                        if (tet.type == 1)
                            str = str + " ligature_start";

                        else if (tet.type == 10)
                            str = str + " ligature_cont";

                        /* Separators are only inserted for granularity > word*/
                        else if (tet.type == 12)
                            str = str + " inserted";

                        /* Examine the bit flags in the "attributes" member */
                        const int ATTR_NONE = 0;
                        const int ATTR_SUB = 1;
                        const int ATTR_SUP = 2;
                        const int ATTR_DROPCAP = 4;
                        const int ATTR_SHADOW = 8;
                        const int ATTR_DH_PRE = 16;
                        const int ATTR_DH_ARTF = 32;
                        const int ATTR_DH_POST = 64;

                        if (tet.attributes != ATTR_NONE)
                        {
                            if ((tet.attributes & ATTR_SUB) == ATTR_SUB)
                                str = str + "/sub";
                            if ((tet.attributes & ATTR_SUP) == ATTR_SUP)
                                str = str + "/sup";
                            if ((tet.attributes & ATTR_DROPCAP) == ATTR_DROPCAP)
                                str = str + "/dropcap";
                            if ((tet.attributes & ATTR_SHADOW) == ATTR_SHADOW)
                                str = str + "/shadow";
                            if ((tet.attributes & ATTR_DH_PRE) == ATTR_DH_PRE)
                                str = str + "/dehyphenation_pre";
                            if ((tet.attributes & ATTR_DH_ARTF) == ATTR_DH_ARTF)
                                str = str + "/dehyphenation_artifact";
                            if ((tet.attributes & ATTR_DH_POST) == ATTR_DH_POST)
                                str = str + "/dehyphenation_post";
                        }
                        outfp.WriteLine(str); 
                    }
                    outfp.WriteLine("");
                }

                if (tet.get_errnum() != 0)
                {
                    Console.WriteLine("Error " + tet.get_errnum() + " in "
                        + tet.get_apiname() + "() on page " 
                        + pageno + ": " + tet.get_errmsg());
                }

                tet.close_page(page);

            }

            tet.close_document(doc);
        }
        catch (TETException e)
        {
            Console.WriteLine("Error " + e.get_errnum() + " in "
                    + e.get_apiname() + "(): " + e.get_errmsg());
        }
        catch (Exception e)
        {
            Console.WriteLine("General Exception: " + e.ToString());
        }
        finally
        {
            if (tet != null)
            {
                tet.Dispose();
            }
        }
    }
}
