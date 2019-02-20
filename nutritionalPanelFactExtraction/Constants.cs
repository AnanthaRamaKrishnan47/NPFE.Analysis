namespace nutritionalPanelFactExtraction
{
    class Constants
    {
        public const string tetPath = "C:\\Users\\arkma\\source\\repos\\nutritionalPanelFactExtraction\\nutritionalPanelFactExtraction\\req_resources\\TET\\bin\\tet.exe";

        public const string modularity = "word";

        public const string pageOpt = "vectoranalysis={structures=tables}";

        public const string TETNS = "http://www.pdflib.com/XML/TET5/TET-5.0";

    }

    public enum Regulation {
        FDA = 0
    }

    public class ColorCode{
        public const string CORRECT = "green";
        public const string WRONG = "red";
        public const string GENERIC = "pink";
    }


}
