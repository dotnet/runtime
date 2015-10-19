using System.IO;
using Xunit;

namespace System.Xml.Tests
{
    public class LoadTests
    {
        // Issue reported on https://github.com/dotnet/corefx/issues/1899
        [Fact]
        public void LoadDocumentFromFile()
        {
            TextReader textReader = File.OpenText(@"example.xml");
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.DtdProcessing = DtdProcessing.Ignore;
            XmlDocument doc = new XmlDocument();

            using (StringReader sr = new StringReader(textReader.ReadToEnd()))
            using (XmlReader reader = XmlReader.Create(sr, settings))
            {
                doc.Load(reader);
            }
        }
    }
}
