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
        if (args.Count() < 2)
        {
            Console.WriteLine("The path to the log file and the name of the wrapper"
                              + " are required for an accurate check and fixing.");
            return -1;
        }

        string resultsDir = args[0];
        string wrapperName = args[1];

        string tempLogName = $"{wrapperName}.templog.xml";
        string finalLogName = $"{wrapperName}.testResults.xml";

        string tempLogPath = Path.Combine(resultsDir, tempLogName);
        string finalLogPath = Path.Combine(resultsDir, finalLogName);
        
        if (File.Exists(finalLogPath))
        {
            Console.WriteLine($"Item '{wrapperName}' did complete successfully!");
            return 0;
        }

        if (!File.Exists(tempLogPath))
        {
            Console.WriteLine("No logs were found. Something went very wrong"
                              + " with this item...");
            return -2;
        }

        FixTheXml(tempLogPath);

        // Rename the temp log to the final log, so that Helix can use it without
        // knowing what transpired here.
        File.Move(tempLogPath, finalLogPath);
        return 0;
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
                string nextOpen = new String(m.Value
                                              .Where(c => char.IsLetter(c))
                                              .ToArray());

                // During testing, I encountered a case where one of the labels
                // a 'test' tag, had incomplete quotes. Since this can only happen
                // with 'test' tags, we handle this case separately. It's also
                // worth noting that in theory, this case can only happen if it's
                // the last line in the log. But to be safe, we'll check all the
                // 'test' tags.
                if (nextOpen.Equals("test"))
                {
                    int quoteCount = 0;

                    // Good ol' simple loop showed better performance in counting
                    // the quotes.
                    foreach (char c in line.AsSpan())
                    {
                        if (c == '"')
                            quoteCount++;
                    }

                    if ((quoteCount % 2) != 0)
                        nextOpen = "testWithMissingQuote";
                }

                tags.Push(nextOpen);
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
            else if (tag.Equals("testWithMissingQuote"))
                xsw.WriteLine("\"</test>");
            else
                xsw.WriteLine($"</{tag}>");
        }

        Console.WriteLine("\nXUnit log file has been fixed!\n");
    }
}

