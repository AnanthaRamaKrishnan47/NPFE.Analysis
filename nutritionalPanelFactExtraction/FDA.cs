using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PDFlib_dotnet;

namespace nutritionalPanelFactExtraction
{
    class FDA
    {     
        public bool Init()
        {
            bool returnStatus = true;
            int tablePos = -1;
            string annotatedFile = string.Empty;
            List<XElement> tables = null;
            List<NPF_Annotate> wordObjects = null;
            NPFE_Object npf_Object = null;
            try
            {
                if(HasTables(TETData, out tables))
                {
                    if (KeyTextLocation(tables, out tablePos))
                    {
                        npf_Object = new NPFE_Object();
                        npf_Object.LLX = float.Parse(tables[tablePos].Attribute("llx").Value);
                        npf_Object.LLY = float.Parse(tables[tablePos].Attribute("lly").Value);
                        npf_Object.URX = float.Parse(tables[tablePos].Attribute("urx").Value);
                        npf_Object.URY = float.Parse(tables[tablePos].Attribute("ury").Value);

                        if (GetTextAndLocation(tables[tablePos], npf_Object, out wordObjects)) {
                            npf_Object.textDetails = wordObjects;

                            if (!Annotate_NF(npf_Object, out annotatedFile))
                                throw new Exception("Error while annotating target PDF");
                            else if(File.Exists(annotatedFile) && new FileInfo(annotatedFile).Length > 0)
                            {
                                System.Diagnostics.Process.Start(annotatedFile);
                            }

                        }
                    }
                    else
                        throw new Exception("No Key information found to process document. Killing process");
                }
                else
                {

                }

                return returnStatus;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        private bool Annotate_NF(NPFE_Object nPanelObject, out string outPath)
        {
            outPath = string.Empty;
            bool returnStatus = true;            
            string annotatedDocPath = string.Empty;
            string optionList = string.Empty;
            int inDoc = -1;
            int status = -1;
            int pageStatus = -1;
            PDFlib annotate = null;
            try
            {
                optionList = " annotcolor ="+ColorCode.GENERIC+ " opacity=0.9";
                annotate = new PDFlib();
                annotatedDocPath = Path.Combine(Path.GetDirectoryName(filePath), (Path.GetFileNameWithoutExtension(filePath) + "_highlighted.pdf"));
                
                annotate.set_option("errorpolicy=return");
                annotate.set_option("license=W900202-010077-142367-MPGCD2-22DW62");

                status = annotate.begin_document(annotatedDocPath, "");

                if (status != 1)
                    throw new Exception("Error while creating annotated PDF");

                outPath = annotatedDocPath;

                inDoc = annotate.open_pdi_document(filePath, "");
                if(inDoc != 0)
                    throw new Exception("Error while opening source PDF");

                pageStatus = annotate.open_pdi_page(inDoc, 1, "");
                if (pageStatus != 0)
                    throw new Exception("Error while opening source PDF page");

                annotate.begin_page_ext(10, 10, "");

                annotate.create_bookmark(filePath, "");

                annotate.fit_pdi_page(pageStatus, 0, 0, "adjustpage");

                foreach(NPF_Annotate X in nPanelObject.textDetails)
                {
                    annotate.create_annotation(X.location.LLX, X.location.LLY, X.location.URX, X.location.URY, "highlight", optionList);
                }

                annotate.close_pdi_page(pageStatus);
                annotate.end_page_ext("");

                annotate.close_pdi_document(inDoc);
                annotate.end_document("");


                return returnStatus;
            }
            catch(PDFlibException pdf_err)
            {
                Console.WriteLine(pdf_err.StackTrace);
                return false;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        private bool GetTextAndLocation(XElement table,NPFE_Object wData, out List<NPF_Annotate> wordObjects)
        {
            bool returnStatus = true;
            wordObjects = new List<NPF_Annotate>();
            List<XElement> wDetail = null;
            float llx = 0;
            float lly = 0;
            float urx = 0;
            float ury = 0;
            try
            {
                GetBoundryLimit(table, out llx, out lly, out urx, out ury);

                foreach (XElement B in table.Descendants(XName.Get("Row", TETData.Root.GetDefaultNamespace().NamespaceName)))
                {
                    wDetail = new List<XElement>();
                    wDetail = B.Descendants(XName.Get("Word", TETData.Root.GetDefaultNamespace().NamespaceName)).ToList();

                    foreach(XElement C in wDetail){
                        
                        var text = C.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).ToList()[0].Value;
                        var box = C.Descendants(XName.Get("Box", TETData.Root.GetDefaultNamespace().NamespaceName)).ToList()[0];

                        if((float.Parse(box.Attribute("llx").Value) >= llx) && (float.Parse(box.Attribute("lly").Value) >=lly) 
                            && (float.Parse(box.Attribute("urx").Value) <= urx) && (float.Parse(box.Attribute("ury").Value) <= ury))
                        {
                            NPF_Annotate eachWord = new NPF_Annotate();
                            eachWord.text = text;
                            eachWord.location = new BOX_Coords();
                            eachWord.location.LLX = float.Parse(box.Attribute("llx").Value);
                            eachWord.location.LLY = float.Parse(box.Attribute("lly").Value);
                            eachWord.location.URX = float.Parse(box.Attribute("urx").Value);
                            eachWord.location.URY = float.Parse(box.Attribute("ury").Value);
                            wordObjects.Add(eachWord);
                        }
                    }
                }

                return returnStatus;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        private bool GetBoundryLimit(XElement table, out float llx, out float lly, out float urx, out float ury)
        {
            llx = 0;
            lly = 0;
            urx = 0;
            ury = 0;
            float tempLLY = 0;
            bool returnStatus = true;
            bool hasMatchingPattern = false;
            XElement nutritionText = null;
            XElement factsText = null;
            List<XElement> dailyList = new List<XElement>();
            List<XElement> valueList = new List<XElement>();
            try
            {
                nutritionText = (XElement)table.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).First(x => x.Value == "Nutrition");
                llx = float.Parse((nutritionText.NextNode as XElement).Attribute("llx").Value);
                factsText = (XElement)table.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).First(x => x.Value == "Facts");
                urx = float.Parse((factsText.NextNode as XElement).Attribute("urx").Value);
                ury = float.Parse((factsText.NextNode as XElement).Attribute("ury").Value);

                dailyList = table.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).Where(x => x.Value == "Daily").ToList();
                valueList = table.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).Where(x => x.Value == "Values" /*|| x.Value == "Value"*/).ToList();

                for(int i = 0;i < dailyList.Count; i++)
                {
                    if(i == 0)
                        tempLLY = float.Parse((dailyList[i].NextNode as XElement).Attribute("lly").Value);

                    if(float.Parse((dailyList[i].NextNode as XElement).Attribute("lly").Value) < tempLLY)
                        tempLLY = float.Parse((dailyList[i].NextNode as XElement).Attribute("lly").Value);
                }

                for(int i = 0; i< valueList.Count; i++)
                {
                    if(tempLLY == float.Parse((valueList[i].NextNode as XElement).Attribute("lly").Value))
                    {
                        hasMatchingPattern = true;
                        break;
                    }
                }

                if (hasMatchingPattern)
                    lly = tempLLY;
                else
                    throw new Exception("Cannot find possible boundry");

                return returnStatus;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        private bool HasTables (XDocument TETData, out List<XElement> tables)
        {
            int count =0;
            tables = new List<XElement>();
            try
            {
                count = TETData.Descendants(XName.Get("Table", TETData.Root.GetDefaultNamespace().NamespaceName)).Count();
                if (count > 0)
                {
                    tables = TETData.Descendants(XName.Get("Table", TETData.Root.GetDefaultNamespace().NamespaceName)).ToList();
                    hasTables = true;
                }
                    
                return count > 0;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        private bool KeyTextLocation(List<XElement> tables, out int location)
        {
            location = -1;
            bool returnStatus = true;
            try
            {
                foreach(XElement A in tables)
                {
                    location++;
                    if (A.Descendants(XName.Get("Text", TETData.Root.GetDefaultNamespace().NamespaceName)).Any(x => x.Value == "Nutrition" || x.Value == "Facts"))
                    {
                        break;
                    }
                }

                if (location == -1)
                    throw new Exception("No required key text found!");

                return returnStatus;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        #region KeyVariables start
        private bool hasTables = false;
        private bool hasKeyText = false;
        private bool hasTemplateMatched = false;
        public string filePath { get; set; }
        public XDocument TETData { get; set; }        
        #endregion KeyVariables end
    }

    public class NPFE_Object {
        public int tPageNumber { get; set; }
        public float URX { get; set; }
        public float URY { get; set; }
        public float LLX { get; set; }
        public float LLY { get; set; }
        public List<NPF_Annotate> textDetails { get; set; }
    }

    public class NPF_Annotate {
        public string text { get; set; }
        public BOX_Coords location { get; set; }
    }

    public class BOX_Coords {
        public float URX { get; set; }
        public float URY { get; set; }
        public float LLX { get; set; }
        public float LLY { get; set; }
    }
}