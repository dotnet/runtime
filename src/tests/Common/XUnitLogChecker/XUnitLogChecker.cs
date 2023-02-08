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
        // public const string OpenTag = @"(\B<\w+)|(\B<!\[CDATA\[)";
        public const string OpenTag = @"(\B<\w+)";
        public const string CloseTag = @"(\B</\w+>)|(\]\]>)";
    }

    private readonly struct TagResult
    {
        public TagResult(string matchValue, TagCategory matchCategory)
        {
            Value = matchValue;
            Category = matchCategory;
        }

        public string Value { get; init; }
        public TagCategory Category { get; init; }
    }

    private enum TagCategory { OPENING, CLOSING }

    private const int SUCCESS = 0;
    private const int MISSING_ARGS = -1;
    private const int SOMETHING_VERY_WRONG = -2;

    static int Main(string[] args)
    {
        // Maybe add a '--help' flag that also gets triggered in this case.
        if (args.Length < 2)
        {
            Console.WriteLine("[XUnitLogChecker]: The path to the log file and"
                              + " the name of the wrapper are required for an"
                              + " accurate check and fixing.");
            return MISSING_ARGS;
        }

        // Creating variables for code clarity and ease of understanding.

        string resultsDir = args[0];
        string wrapperName = args[1];

        string tempLogName = $"{wrapperName}.tempLog.xml";
        string finalLogName = $"{wrapperName}.testResults.xml";
        string statsCsvName = $"{wrapperName}.testStats.csv";

        string tempLogPath = Path.Combine(resultsDir, tempLogName);
        string finalLogPath = Path.Combine(resultsDir, finalLogName);
        string statsCsvPath = Path.Combine(resultsDir, statsCsvName);

        // If there is not even the temp log, then something went very badly with
        // the work item in question. It will need a developer/engineer to look
        // at it urgently.

        if (!File.Exists(tempLogPath))
        {
            Console.WriteLine("[XUnitLogChecker]: No logs were found. Something"
                              + " went very wrong with this item...");

            return SOMETHING_VERY_WRONG;
        }

        // Read the stats csv file.
        IEnumerable<string> workItemStats = File.ReadLines(statsCsvPath);

        // The first value at the top of the csv represents the amount of tests
        // that were expected to be run.
        //
        // NOTE: We know for certain the csv only includes numbers. Therefore,
        // we're fine in using Int32.Parse() directly.

        int numExpectedTests = Int32.Parse(workItemStats.First().Split(',').First());

        // The last line of the csv represents the status when the work item
        // finished, successfully or not. It has the following format:
        //     (Tests Run, Tests Passed, Tests Failed, Tests Skipped)

        int[] workItemEndStatus = workItemStats.Last()
                                               .Split(',')
                                               .Select(x => Int32.Parse(x))
                                               .ToArray();

        // If the final results log file is present, then we can assume everything
        // went fine, and it's ready to go without any further processing. We just
        // check the stats csv file to know how many tests were run, and display a
        // brief summary of the work item.

        if (File.Exists(finalLogPath))
        {
            Console.WriteLine($"[XUnitLogChecker]: Item '{wrapperName}' did"
                              + " complete successfully!");

            PrintWorkItemSummary(numExpectedTests, workItemEndStatus);
            return SUCCESS;
        }

        // Here goes the main core of the XUnit Log Checker :)
        Console.WriteLine($"[XUnitLogChecker]: Item '{wrapperName}' did not"
                        + " finish running. Checking and fixing the log...");

        FixTheXml(tempLogPath);
        PrintWorkItemSummary(numExpectedTests, workItemEndStatus);

        // Rename the temp log to the final log, so that Helix can use it without
        // knowing what transpired here.
        File.Move(tempLogPath, finalLogPath);
        return SUCCESS;
    }

    static void PrintWorkItemSummary(int numExpectedTests, int[] workItemEndStatus)
    {
        Console.WriteLine($"\n{workItemEndStatus[0]}/{numExpectedTests} tests run.");
        Console.WriteLine($"* {workItemEndStatus[1]} tests passed.");
        Console.WriteLine($"* {workItemEndStatus[2]} tests failed.");
        Console.WriteLine($"* {workItemEndStatus[3]} tests skipped.\n");
    }

    static void FixTheXml(string xFile)
    {
        var tags = new Stack<string>();

        // Flag to ensure we don't process tag-like-looking things while reading through
        // a test's output.
        bool inOutput = false;

        foreach (string line in File.ReadLines(xFile))
        {
            // Get all XML tags found in the current line and sort them in order
            // of appearance.
            Match[] opens = Regex.Matches(line, Patterns.OpenTag).ToArray();
            Match[] closes = Regex.Matches(line, Patterns.CloseTag).ToArray();
            TagResult[] allTags = GetOrderedTagMatches(opens, closes);

            foreach (TagResult tr in allTags)
            {
                // Get the name of the next tag. We need solely the text, so we
                // ask LINQ to lend us a hand in removing the symbols from the string.
                string tagText = new String(tr.Value.Where(c => char.IsLetter(c)).ToArray());

                // Found an opening tag. Push into the stack and move on to the next one.
                if (tr.Category == TagCategory.OPENING && !inOutput)
                {
                    tags.Push(tagText);
                    continue;
                }

                // Found a closing tag. If we're currently in an output state, then
                // check whether it's the output closing tag. Otherwise, ignore it.
                // This is because in that case, it's just part of the output's text,
                // rather than an actual XML log tag.
                if (tr.Category == TagCategory.CLOSING)
                {
                }
            }
        }

        Console.WriteLine("[XUnitLogChecker]: XUnit log file has been fixed!");
    }

    static TagResult[] GetOrderedTagMatches(Match[] openingTags, Match[] closingTags)
    {
        var result = new TagResult[openingTags.Length + closingTags.Length];
        int resIndex = opIndex = clIndex = 0;

        // Fill up the result array with the tags found, in order of appearance
        // in the original log file line.
        //
        // As long as the result array hasn't been filled, then we know for certain
        // that there's at least one unprocessed tag.

        while (resIndex < result.Length)
        {
            if (opIndex < openingTags.Length)
            {
                // We still have pending tags on both lists, opening and closing.
                // So, we add the one that appears first in the given log line.
                if (clIndex < closingTags.Length)
                {
                    if (openingTags[opIndex].Index < closingTags[clIndex].Index)
                    {
                        result[resIndex++] = new TagResult(openingTags[opIndex].Value,
                                                           TagCategory.OPENING);
                        opIndex++;
                    }
                    else
                    {
                        result[resIndex++] = new TagResult(closingTags[clIndex].Value,
                                                           TagCategory.CLOSING);
                        clIndex++;
                    }
                }

                // Only opening tags remaining, so just add them.
                else
                {
                    result[resIndex++] = new TagResult(openingTags[opIndex].Value,
                                                       TagCategory.OPENING);
                    opIndex++;
                }
            }

            // Only closing tags remaining, so just add them.
            else
            {
                result[resIndex++] = new TagResult(closingTags[clIndex].Value,
                                                   TagCategory.CLOSING);
                clIndex++;
            }
        }
        return result;
    }
}

