using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

public class XUnitLogChecker
{
    private static class Patterns
    {
        public const string OpenTag = @"(\B<\w+)|(\B<!\[CDATA\[)";
        public const string CloseTag = @"(\B</\w+>)|(\]\]>)";
    }

    static int Main(string[] args)
    {
        if (args.Count() < 1)
        {
            Console.WriteLine("The path to the XML log file to be checked is required.");
            return -1;
        }

        // Check if the XUnit log is well-formed. If yes, then we have nothing
        // else to do :)
        if (TryLoadXml(args[0]))
        {
            Console.WriteLine($"The given XML '{args[0]}' is well formed! Exiting...");
            return 0;
        }

        FixTheXml(args[0]);
        return 0;
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

    static void FixTheXml(string xFile)
    {
        var tags = new Stack<string>();

        foreach (string line in File.ReadLines(xFile))
        {
            // Get all XML tags found in the current line.
            var opens = Regex.Matches(line, Patterns.OpenTag).ToList();
            var closes = Regex.Matches(line, Patterns.CloseTag).ToList();

            foreach (Match m in opens)
            {
                // Push the next opening tag to the stack. We need only the actual
                // tag without the opening and closing symbols, so we ask LINQ to
                // lend us a hand.
                tags.Push(new String(m.Value.Where(c => char.IsLetter(c)).ToArray()));
            }

            // If we found any closing tags, then ensure they match their respective
            // opening ones before continuing the analysis.
            while (closes.Count > 0)
            {
                string nextClosing = closes.First().Value.Equals("]]>")
                                     ? "CDATA"
                                     : new String(closes.First()
                                                        .Value
                                                        .Where(c => char.IsLetter(c))
                                                        .ToArray());

                if (nextClosing.Equals(tags.Peek()))
                {
                    tags.Pop();
                    closes.RemoveAt(0);
                }
            }
        }

        if (tags.Count == 0)
        {
            Console.WriteLine($"\nXUnit log file '{xFile}' was A-OK!\n");
        }

        // Write the closings for all the lone opened tags we found.
        using (StreamWriter xsw = File.AppendText(xFile))
        while (tags.Count > 0)
        {
            string tag = tags.Pop();
            if (tag.Equals("CDATA"))
                xsw.WriteLine("]]>");
            else
                xsw.WriteLine($"</{tag}>");
        }

        Console.WriteLine("\nXUnit log file has been fixed!\n");
    }
}

