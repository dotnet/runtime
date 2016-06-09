using System.IO;

namespace System.Xml.Tests
{
    public class XmlTestSettings
    {
        public string DataPath
        {
            get { return ""; }
        }

        public int MaxPriority
        {
            get { return 3; }
        }
    }

	public static class FilePathUtil
	{
		public static XmlTestSettings TestSettings = new XmlTestSettings();

	    public static string GetDataPath()
        {
            return TestSettings.DataPath;
        }

        public static string GetTestDataPath()
        {
            return Path.Combine(GetDataPath(), "TestFiles", "TestData");
        }
        public static string GetStandardPath()
        {
            return Path.Combine(GetDataPath(), "TestFiles", "StandardTests");
        }
	}
}
