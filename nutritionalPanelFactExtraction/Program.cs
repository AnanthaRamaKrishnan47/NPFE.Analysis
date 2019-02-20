using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace nutritionalPanelFactExtraction
{
    class nutritionalPanelFactExtraction
    {
        static void Main(string[] args)
        {
            string filePath = string.Empty;
            string tetOutPath = string.Empty;
            bool isTETExtarctSuccess = false;
            int regulation = 0;
            Validation val = null;
            nutritionalPanelFactExtraction NPFE = null;
            Console.WriteLine("Karomi Technology :: NPFE");
            XDocument TETML = null;
            XNamespace TETNS = Constants.TETNS;
            try
            {
                val = new Validation();
                NPFE = new nutritionalPanelFactExtraction();
                Console.WriteLine("Enter valid file path : ");
                filePath = val.CheckForValidFilePath(Console.ReadLine());
                Console.WriteLine("Valid file path : " + val);

                Console.WriteLine("Enter valid regulation");
                regulation = val.ValidateRegulation(Console.ReadLine());                

                isTETExtarctSuccess = NPFE.GetTETML(filePath, out tetOutPath);
                if (!(isTETExtarctSuccess && File.Exists(tetOutPath)))
                    throw new Exception("Extracting TETML file failed, killing process");
                else
                {
                    TETML = XDocument.Load(tetOutPath);                    

                     NPFE.ExtractByRegulation(regulation, TETML, filePath);
                }

            }
            catch (Exception err)
            {
                Console.WriteLine(err.StackTrace);
            }
        }

        public bool GetTETML(string filePath, out string tetOutPath, [Optional] string pageOptOverride)
        {
            tetOutPath = string.Empty;
            try
            {
                tetOutPath = Path.Combine(Path.GetDirectoryName(filePath) , Path.GetFileNameWithoutExtension(filePath) + ".tetml");
                Process cmd = new Process();
                cmd.StartInfo.FileName = Constants.tetPath;//"cmd.exe";
                cmd.StartInfo.Arguments = string.Format(@" --tetml {1}  --pageopt " + '"' + "{2}" + '"' + "  -o {3}   {4}",
                    Constants.tetPath, Constants.modularity, string.IsNullOrEmpty(pageOptOverride) ? Constants.pageOpt : pageOptOverride, tetOutPath, filePath);
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                Console.WriteLine("Command tobe executed : " + cmd.StartInfo.Arguments);
                cmd.Start();
                
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
                Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }

        public bool ExtractByRegulation(int S, XDocument TETData, string fPath)
        {
            bool returnStatus = false;
            try
            {
                switch (S) {
                    case (int)Regulation.FDA:
                        FDA fda = new FDA
                        {
                            TETData = TETData,
                            filePath = fPath
                        };
                        if (!fda.Init())
                            throw new Exception("Error While annnotating text. Please restart process");
                        break;
                    default:
                        return false;
                }

                return returnStatus;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                return false;
            }
        }
    }
}
