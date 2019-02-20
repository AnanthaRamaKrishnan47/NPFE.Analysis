using System;
using System.IO;

namespace nutritionalPanelFactExtraction
{
    class Validation
    {
        public string CheckForValidFilePath(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new Exception("Non existant file, Please enter a valid file path");
                else
                    return filePath;
            }
            catch(Exception err)
            {
                Console.WriteLine(err.StackTrace);
                CheckForValidFilePath(Console.ReadLine());
                return null;
            }
        }

        public int ValidateRegulation(string REG){
            int returnVal = -1;
            try{
                returnVal = (int)Enum.Parse(typeof(Regulation), REG);
                Console.WriteLine("REgulation valaue : " + returnVal);

                if(returnVal != (int)returnVal)
                    throw new Exception("Invalid regulation, Enter valid regulation");
                else
                    return returnVal;
            }
            catch(Exception  err){
                Console.WriteLine(err.StackTrace);
                ValidateRegulation(Console.ReadLine());
                return -1;
            }
        }
    }
}
