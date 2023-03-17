﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

public class XUnitLogChecker
{
    private static class Patterns
    {
        public const string OpenTag = @"(\B<\w+)|(\B<!\[CDATA\[)";
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
    private const int FAILURE = -2;

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

        // If there are no logs, then this work item was probably entirely skipped.
        // This can happen under certain specific circumstances, such as with the
        // JIT Hardware Intrinsics tests with DOTNET_GCStress enabled. See Github
        // Issue dotnet/runtime #82143 for more info.
        //
        // The other possibility would be that something went very badly with
        // the work item in question. It will need a developer/engineer to look
        // at it urgently.

        if (!File.Exists(tempLogPath))
        {
            Console.WriteLine("[XUnitLogChecker]: No logs were found. This work"
                              + " item was skipped.");
            Console.WriteLine($"[XUnitLogChecker]: If this is a mistake, then"
                              + " something went very wrong. The expected temp"
                              + $" log name would be: '{tempLogName}'");
            return SUCCESS;
        }

        // If the final results log file is present, then we can assume everything
        // went fine, and it's ready to go without any further processing. We just
        // check the stats csv file to know how many tests were run, and display a
        // brief summary of the work item.

        if (File.Exists(finalLogPath))
        {
            Console.WriteLine($"[XUnitLogChecker]: Item '{wrapperName}' did"
                              + " complete successfully!");
            return SUCCESS;
        }

        // If we're here, then that means we've got something to fix.
        // First, read the stats csv file. If it doesn't exist, then we can
        // assume something went very badly and will likely cause more issues
        // later on, so we exit now.

        if (!File.Exists(statsCsvPath))
        {
            Console.WriteLine("[XUnitLogChecker]: An error occurred. No stats csv"
                            + $" was found. The expected name would be '{statsCsvPath}'.");
            return FAILURE;
        }

        // Declaring the enumerable to contain the log lines first because we
        // might not be able to read on the first try due to locked resources
        // on Windows. We will retry for up to one minute when this case happens.
        IEnumerable<string>? workItemStats = TryReadFile(statsCsvPath);

        if (workItemStats is null)
        {
            Console.WriteLine("[XUnitLogChecker]: Timed out trying to read the"
                            + $" stats file '{statsCsvPath}'.");
            return FAILURE;
        }

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

        // Here goes the main core of the XUnit Log Checker :)
        Console.WriteLine($"[XUnitLogChecker]: Item '{wrapperName}' did not"
                        + " finish running. Checking and fixing the log...");

        bool success = FixTheXml(tempLogPath);
        if (!success)
        {
            Console.WriteLine("[XUnitLogChecker]: Fixing the log failed.");
            return FAILURE;
        }

        PrintWorkItemSummary(numExpectedTests, workItemEndStatus);

        // Rename the temp log to the final log, so that Helix can use it without
        // knowing what transpired here.
        File.Move(tempLogPath, finalLogPath);
        return SUCCESS;
    }

    static IEnumerable<string> TryReadFile(string filePath)
    {
        IEnumerable<string>? fileContents = null;
        Stopwatch fileReadStopwatch = Stopwatch.StartNew();

        while (fileReadStopwatch.ElapsedMilliseconds < 60000)
        {
            // We were able to read the file, so we can finish this loop.
            if (fileContents is not null)
                break;

            try
            {
                fileContents = File.ReadLines(filePath);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("[XUnitLogChecker]: Could not read the"
                                + $" file {filePath}. Retrying...");

                // Give it a couple seconds before trying again.
                Thread.Sleep(2000);
            }
        }
        return fileContents;
    }

    static void PrintWorkItemSummary(int numExpectedTests, int[] workItemEndStatus)
    {
        Console.WriteLine($"\n{workItemEndStatus[0]}/{numExpectedTests} tests run.");
        Console.WriteLine($"* {workItemEndStatus[1]} tests passed.");
        Console.WriteLine($"* {workItemEndStatus[2]} tests failed.");
        Console.WriteLine($"* {workItemEndStatus[3]} tests skipped.\n");
    }

    static bool FixTheXml(string xFile)
    {
        var tags = new Stack<string>();
        string tagText = string.Empty;
        IEnumerable<string>? logLines = TryReadFile(xFile);

        if (logLines is null)
        {
            Console.WriteLine("[XUnitLogChecker]: Timed out trying to read the"
                            + $" log file '{xFile}'.");
            return false;
        }

        // Flag to ensure we don't process tag-like-looking things while reading through
        // a test's output.
        bool inOutput = false;
        bool inCData = false;

        foreach (string line in logLines)
        {
            // Get all XML tags found in the current line and sort them in order
            // of appearance.
            Match[] opens = Regex.Matches(line, Patterns.OpenTag).ToArray();
            Match[] closes = Regex.Matches(line, Patterns.CloseTag).ToArray();
            TagResult[] allTags = GetOrderedTagMatches(opens, closes);

            foreach (TagResult tr in allTags)
            {
                // Found an opening tag. Push into the stack and move on to the next one.
                if (tr.Category == TagCategory.OPENING)
                {
                    // Get the name of the next tag. We need solely the text, so we
                    // ask LINQ to lend us a hand in removing the symbols from the string.
                    tagText = new String(tr.Value.Where(c => char.IsLetter(c)).ToArray());

                    // We are beginning to process a test's output. Set the flag to
                    // treat everything as such, until we get the closing output tag.
                    if (tagText.Equals("output") && !inOutput && !inCData)
                        inOutput = true;
                    else if (tagText.Equals("CDATA") && !inCData)
                        inCData = true;

                    tags.Push(tagText);
                    continue;
                }

                // Found a closing tag. If we're currently in an output state, then
                // check whether it's the output closing tag. Otherwise, ignore it.
                // This is because in that case, it's just part of the output's text,
                // rather than an actual XML log tag.
                if (tr.Category == TagCategory.CLOSING)
                {
                    // As opposed to the usual XML tags we can find in the logs,
                    // the CDATA closing one doesn't have letters, so we treat it
                    // as a special case.
                    tagText = tr.Value.Equals("]]>")
                              ? "CDATA"
                              : new String(tr.Value
                                           .Where(c => char.IsLetter(c))
                                           .ToArray());

                    if (inCData)
                    {
                        if (tagText.Equals("CDATA"))
                        {
                            tags.Pop();
                            inCData = false;
                        }
                        else continue;
                    }

                    if (inOutput)
                    {
                         if (tagText.Equals("output"))
                         {
                             tags.Pop();
                             inOutput = false;
                         }
                         else continue;
                    }

                    if (tagText.Equals(tags.Peek()))
                    {
                        tags.Pop();
                    }
                }
            }
        }

        if (tags.Count == 0)
        {
            Console.WriteLine($"[XUnitLogChecker]: XUnit log file '{xFile}' was A-OK!");
            return true;
        }

        // Write the missing closings for all the opened tags we found.
        using (StreamWriter xsw = File.AppendText(xFile))
        while (tags.Count > 0)
        {
            string tag = tags.Pop();
            if (tag.Equals("CDATA"))
                xsw.WriteLine("]]>");
            else
                xsw.WriteLine($"</{tag}>");
        }

        Console.WriteLine("[XUnitLogChecker]: XUnit log file has been fixed!");
        return true;
    }

    static TagResult[] GetOrderedTagMatches(Match[] openingTags, Match[] closingTags)
    {
        var result = new TagResult[openingTags.Length + closingTags.Length];
        int resIndex = 0;
        int opIndex = 0;
        int clIndex = 0;

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

