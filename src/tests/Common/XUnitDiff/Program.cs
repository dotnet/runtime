// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Numerics;

public class Program
{
    private readonly List<string> _leftFiles;
    private readonly List<string> _rightFiles;

    public static int Main(string[] args)
    {
        try
        {
            return new Program().TryMain(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private Program()
    {
        _leftFiles = new List<string>();
        _rightFiles = new List<string>();
    }

    private int TryMain(string[] args)
    {
        ParseCommandLine(args);

        XUnitResultSummary leftSummary = new XUnitResultSummary();
        leftSummary.AppendXmlResults(_leftFiles);

        XUnitResultSummary rightSummary = new XUnitResultSummary();
        rightSummary.AppendXmlResults(_rightFiles);

        leftSummary.DumpStatistics("LEFT STATISTICS");
        rightSummary.DumpStatistics("RIGHT STATISTICS");
        XUnitResultSummary.DiffTestSet(leftSummary, rightSummary);

        return 0;
    }

    private void ParseCommandLine(string[] args)
    {
        bool isRight = false;
        foreach (string arg in args)
        {
            if (arg == "-right" || arg == "-r")
            {
                isRight = true;
            }
            else
            {
                List<string> target = (isRight ? _rightFiles : _leftFiles);
                string dir = Path.GetDirectoryName(arg)!;
                string pattern = Path.GetFileName(arg);
                foreach (string file in Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly))
                {
                    target.Add(file);
                }
            }
        }
    }
}
