using System;
using System.IO;
using System.Xml;

public class XUnitLogChecker
{
    private static class Patterns
    {
        public const string OpenTag = @"(\B<\w+)|(\B<!\[CDATA\[)";
        public const string CloseTag = @"(\B</\w+>)|(\]\]>)";
    }

    const int SUCCESS = 0;
    const int FAILURE = -1;

    static int Main(string[] args)
    {
        if (TryLoadXml(args[0]))
        {
            Console.WriteLine($"The given XML '{args[0]}' is well formed! Exiting...");
            return SUCCESS;
        }

        return FAILURE;
    }

    static bool TryLoadXml(string xFile)
    {
        try
        {
            using (var xReader = XmlReader.Create(new StreamReader(xFile)))
            {
                var xDocument = new XmlDocument();
                xDocument.Load(xReader);
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"The given XML {xFile} was malformed. Fixing now...");
            return false;
        }

        return true;
    }
}

