// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SuperPmi.Mcs;

internal static class FileHelpers
{
    public static FileStream CreateOutput(string fileName)
    {
        try
        {
            return new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, FileOptions.SequentialScan);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open output file '{fileName}'. GetLastError()={ErrorHandling.LastError(e)}");
            return null;
        }
    }

    public static string Rate(long bytes, Stopwatch timer)
        => $"Read/Wrote {bytes / (1000 * 1000)} MB @ {bytes / (1000.0 * 1000) / timer.Elapsed.TotalSeconds,4:F2} MB/s.";
}

internal static class VerbConcat
{
    private const int BufferSize = 0xFFFFFF;

    // Appends file2 to file1, without regard to the internal format.
    public static int DoWork(string nameOfFile1, string nameOfFile2)
    {
        Logger.LogVerbose($"Concatenating '{nameOfFile1}'+'{nameOfFile2}' into {nameOfFile1}");

        FileStream file1;
        try
        {
            file1 = new FileStream(nameOfFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.SequentialScan);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open input 1 '{nameOfFile1}'. GetLastError()={ErrorHandling.LastError(e)}");
            return -1;
        }

        using (file1)
        {
            file1.Seek(0, SeekOrigin.End);

            FileStream file2;
            try
            {
                file2 = new FileStream(nameOfFile2, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.LogError($"Failed to open input 2 '{nameOfFile2}'. GetLastError()={ErrorHandling.LastError(e)}");
                return -1;
            }

            using (file2)
            {
                long size = file2.Length;
                var timer = Stopwatch.StartNew();
                byte[] buffer = new byte[BufferSize];
                int read;
                while ((read = file2.Read(buffer)) > 0)
                    file1.Write(buffer, 0, read);
                timer.Stop();

                Logger.LogInfo(FileHelpers.Rate(size, timer) + "\n");
            }
        }
        return 0;
    }
}

internal static class VerbStrip
{
    // Handles both "-copy" and "-strip": both copy from the input file to the output file, but
    // treat the indexes in opposite ways.
    public static int DoWork(string nameOfInput, string nameOfOutput, int[] indexes, bool strip, bool stripCR)
    {
        if (strip)
            return DoWorkTheOldWay(nameOfInput, nameOfOutput, indexes ?? [], stripCR);

        Logger.LogVerbose($"Reading from '{nameOfInput}' removing Mc Indexes and writing into '{nameOfOutput}'");

        // The reader skips unrequested method contexts, using the TOC if there is one.
        using var reader = new MethodContextReader(nameOfInput, indexes);
        if (!reader.isValid())
            return -1;

        using FileStream output = FileHelpers.CreateOutput(nameOfOutput);
        if (output == null)
            return -1;

        var timer = Stopwatch.StartNew();
        int loadedCount = 0;
        int savedCount = 0;
        while (true)
        {
            MethodContextBuffer mcb = reader.GetNextMethodContext();
            if (mcb.Error())
                return -1;
            if (mcb.allDone())
                break;

            loadedCount++;
            if (loadedCount % 500 == 0)
            {
                Logger.LogVerbose($"{reader.PercentComplete(),2:F1}% - Loaded {loadedCount} at {(int)(500 / timer.Elapsed.TotalSeconds)} per second");
                timer.Restart();
            }

            if (!MethodContext.Initialize(loadedCount, mcb.buff, (uint)mcb.size, !stripCR, out MethodContext mc))
                return -1;

            mc.saveToFile(output);
            savedCount++;
        }

        Logger.LogInfo($"Loaded {loadedCount}, Saved {savedCount}");
        return 0;
    }

    private static int DoWorkTheOldWay(string nameOfInput, string nameOfOutput, int[] indexes, bool stripCR)
    {
        Logger.LogVerbose($"Reading from '{nameOfInput}' removing MC Indexes and writing into '{nameOfOutput}'");

        using var mci = new MethodContextIterator(progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int savedCount = 0;
        int index = 0;

        using (FileStream output = FileHelpers.CreateOutput(nameOfOutput))
        {
            if (output == null)
                return -1;

            while (mci.MoveNext())
            {
                MethodContext mc = mci.Current;

                if (index < indexes.Length && indexes[index] == mci.MethodContextNumber)
                {
                    index++;
                    continue;
                }

                if (stripCR)
                    mc.cr = new CompileResult();
                mc.saveToFile(output);
                savedCount++;
            }
        }

        if (index < indexes.Length)
            Logger.LogWarning($"Didn't use all of index count input {index} < {indexes.Length}  (i.e. didn't see MC #{indexes[index]})");

        Logger.LogInfo($"Loaded {mci.MethodContextNumber}, Saved {savedCount}");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static class VerbFracture
{
    public static int DoWork(string nameOfInput, string nameOfOutput, int[] indexes, bool stripCR)
    {
        if (indexes.Length == 0)
        {
            Logger.LogError("-fracture requires a range.");
            return -1;
        }
        if (indexes.Length > 1)
            Logger.LogWarning("-fracture found multiple ranges, we'll use the first one.");

        int rangeSize = indexes[0];

        Logger.LogVerbose($"Reading from '{nameOfInput}' copying {rangeSize} MethodContexts files into each output file of '{nameOfOutput}'");

        using var mci = new MethodContextIterator(progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int fileCount = 0;
        FileStream output = null;
        try
        {
            while (mci.MoveNext())
            {
                MethodContext mc = mci.Current;

                if (output == null || ((mci.MethodContextNumber - 1) % rangeSize) == 0)
                {
                    output?.Dispose();
                    output = FileHelpers.CreateOutput($"{nameOfOutput}-{fileCount++:D5}.mch");
                    if (output == null)
                        return -1;
                }

                if (stripCR)
                    mc.cr = new CompileResult();
                mc.saveToFile(output);
            }
        }
        finally
        {
            output?.Dispose();
        }

        Logger.LogInfo($"Output fileCount {fileCount}");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static class VerbDumpToc
{
    public static int DoWork(string nameOfInput)
    {
        var tf = new TOCFile();
        tf.LoadToc(nameOfInput, validate: false);

        var sb = new StringBuilder();
        for (int i = 0; i < tf.GetTocCount(); i++)
        {
            TOCElement te = tf.GetElement(i);
            sb.Clear();
            sb.Append($"{(uint)te.Number,4}: {te.Offset:X16} ");
            foreach (byte b in te.Hash)
                sb.Append(((int)(sbyte)b).ToString("x2")).Append(' ');
            sb.Append('\n');
            Logger.Out.Write(sb);
        }

        return 0;
    }
}

internal static class VerbInteg
{
    public static int DoWork(string nameOfInput)
    {
        Logger.LogVerbose($"Checking the integrity of '{nameOfInput}'");

        var timer = Stopwatch.StartNew();

        using var mci = new MethodContextIterator(progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return -1;

        // Nothing to do except load each one.
        while (mci.MoveNext())
        {
        }

        timer.Stop();
        Logger.LogInfo($"Checked the integrity of {mci.MethodContextNumber} methodContexts at {(int)(mci.MethodContextNumber / timer.Elapsed.TotalSeconds)} per second");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static class VerbStat
{
    public static int DoWork(string nameOfInput, string nameOfOutput, int[] indexes)
    {
        Logger.LogVerbose($"Stat'ing from '{nameOfInput}' and writing output into '{nameOfOutput}'");

        using var mci = new MethodContextIterator(indexes, progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int savedCount = 0;
        using (FileStream output = FileHelpers.CreateOutput(nameOfOutput))
        {
            if (output == null)
                return -1;

            var sb = new StringBuilder("Title,MC#,");
            MethodContext.dumpStatTitleToBuffer(sb);
            sb.Append("\r\n");
            output.Write(Encoding.UTF8.GetBytes(sb.ToString()));

            while (mci.MoveNext())
            {
                MethodContext mc = mci.Current;

                sb.Clear();
                if (mc.cr.ProcessName != null && mc.cr.ProcessName.GetCount() > 0)
                    sb.Append(mc.cr.repProcessName());
                sb.Append(',');
                sb.Append(mci.MethodContextNumber).Append(',');
                mc.dumpStatToBuffer(sb);
                sb.Append("\r\n");
                output.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                savedCount++;
            }
        }

        Logger.LogInfo($"Loaded {mci.MethodContextNumber}, Stat'd {savedCount}");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static class VerbPrintJITEEVersion
{
    // Tools use this to pick the MCH files that match a JIT built from the same source tree.
    public static int DoWork()
    {
            Logger.Out.Write(JitEEVersion.Identifier.ToString("D") + "\n");
        return 0;
    }
}

internal static class VerbTOC
{
    public static int DoWork(string nameOfInput)
    {
        Logger.LogVerbose($"Indexing from '{nameOfInput}' into '{nameOfInput}.mct'");

        using var mci = new MethodContextIterator();
        if (!mci.Initialize(nameOfInput))
            return -1;

        var elements = new System.Collections.Generic.List<TOCElement>();
        while (mci.MoveNext())
        {
            MethodContext mc = mci.Current;
            elements.Add(new TOCElement(mci.MethodContextNumber, mci.CurrentPos, mc.dumpMethodHashToBuffer()));
        }

        string nameOfOutput = nameOfInput + ".mct";
        using (FileStream output = FileHelpers.CreateOutput(nameOfOutput))
        {
            if (output == null)
                return -1;

            byte[] data = new byte[8 + elements.Count * TOCElement.Size + 4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data, TOCFile.Sentinel);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), elements.Count);
            for (int i = 0; i < elements.Count; i++)
                elements[i].Write(data.AsSpan(8 + i * TOCElement.Size));
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(data.Length - 4), TOCFile.Sentinel);
            output.Write(data);
        }

        Logger.LogInfo($"Loaded {mci.MethodContextNumber}, added {elements.Count} to Table of Contents");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static class VerbRemoveDup
{
    public static int DoWork(string nameOfInput, string nameOfOutput, bool stripCR, bool legacyCompare)
    {
        Logger.LogVerbose($"Removing duplicates from '{nameOfInput}', writing to '{nameOfOutput}'");

        var removeDups = new RemoveDup(stripCR, legacyCompare);

        using FileStream output = FileHelpers.CreateOutput(nameOfOutput);
        if (output == null)
            return -1;

            if (!removeDups.CopyAndRemoveDups(nameOfInput, output))
            {
                Logger.LogError("Failed to remove dups");
                return -1;
            }
            return 0;
    }
}
