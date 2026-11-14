// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperPmi;

// Lists of method context numbers: either a range list like "1-5,9" or a .mcl file with one number per line.
public sealed class MCList
{
    private const long MAXMCLFILESIZE = 0xffffff;

    private FileStream hMCLFile;

    // True if 'input' should be treated as a list rather than as a file name.
    public static bool LooksLikeList(string input)
    {
        if (input.EndsWith(".mcl", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (char c in input)
        {
            if (c != '-' && c != ',' && !char.IsAsciiDigit(c))
                return false;
        }
        return input.Length > 0;
    }

    // Parses a range list or reads an .mcl file. The numbers must start at 1 and be increasing.
    // Input that is neither fails without an error message.
    public static bool TryParse(string input, out int[] list, out string error)
    {
        list = null;
        error = null;

        if (input.AsSpan().ContainsAnyExcept("0123456789-,"))
        {
            if (!input.EndsWith(".mcl", StringComparison.OrdinalIgnoreCase) || !getLineData(input, out list, out error))
                return false;
        }
        else if (!TryParseRangeList(input, out list, out error))
        {
            return false;
        }

        if (list.Length > 0 && list[0] == 0)
        {
            error = "MCL list needs to start from 1!";
            return false;
        }
        for (int i = 1; i < list.Length; i++)
        {
            if (list[i - 1] >= list[i])
            {
                error = $"MCL list must be increasing.. found {list[i - 1]} -> {list[i]}";
                return false;
            }
        }
        return true;
    }

    private static bool TryParseRangeList(string input, out int[] list, out string error)
    {
        list = null;
        error = null;

        var result = new List<int>();
        bool inRange = false;
        uint rangeStart = 0;
        for (int head = 0; head <= input.Length; head++)
        {
            uint scratch = 0;
            bool foundDigit = false;
            while (head < input.Length && char.IsAsciiDigit(input[head]))
            {
                scratch = (scratch * 10) + (uint)(input[head] - '0');
                foundDigit = true;
                head++;
            }
            if (foundDigit)
            {
                if (inRange)
                {
                    inRange = false;
                    if (rangeStart >= scratch)
                    {
                        error = $"Invalid range in '{input}'";
                        return false;
                    }
                    for (uint i = rangeStart + 1; i <= scratch; i++)
                        result.Add((int)i);
                }
                else
                {
                    rangeStart = scratch;
                    result.Add((int)scratch);
                }
            }
            if (head < input.Length && input[head] == '-')
                inRange = true;
        }

        if (result.Count == 0)
        {
            error = "Didn't find a list!";
            return false;
        }
        if (inRange)
        {
            error = $"Found invalid external range in '{input}'";
            return false;
        }

        list = result.ToArray();
        return true;
    }

    // Reads one number per line. Lines without digits are skipped.
    public static bool getLineData(string nameOfInput, out int[] indexes, out string error)
    {
        indexes = null;
        error = null;
        byte[] buff;
        try
        {
            long size = new FileInfo(nameOfInput).Length;
            if (size > MAXMCLFILESIZE)
            {
                error = $"Size {size} exceeds max size of {MAXMCLFILESIZE}";
                return false;
            }
            buff = File.ReadAllBytes(nameOfInput);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            error = $"Unable to open '{nameOfInput}'. GetLastError()={ErrorHandling.LastError(e)}";
            return false;
        }

        var result = new List<int>();
        int i = 0;
        while (i < buff.Length)
        {
            while (i < buff.Length && !char.IsAsciiDigit((char)buff[i]))
                i++;
            if (i >= buff.Length)
                break;

            long value = 0;
            while (i < buff.Length && char.IsAsciiDigit((char)buff[i]))
            {
                value = Math.Min(value * 10 + (buff[i] - '0'), int.MaxValue);
                i++;
            }
            result.Add((int)value);

            while (i < buff.Length && buff[i] != '\n')
                i++;
            i++;
        }

        indexes = result.ToArray();
        return true;
    }

    public void InitializeMCL(string filename)
    {
        try
        {
            hMCLFile = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open output file '{filename}'. GetLastError()={ErrorHandling.LastError(e)}");
        }
    }

    public void AddMethodToMCL(int methodIndex)
    {
        hMCLFile?.Write(Encoding.ASCII.GetBytes($"{methodIndex}\r\n"));
    }

    public void CloseMCL()
    {
        hMCLFile?.Dispose();
        hMCLFile = null;
    }
}
