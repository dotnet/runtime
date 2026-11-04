// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperPmi;

// The raw bytes of one method context: 'size' bytes of packet data followed by the 2-byte end canary.
// Without a buffer, the size is 0 for a skipped method context, Completed when all are done, or else an error code.
public readonly struct MethodContextBuffer
{
    private const int Completed = 0x1234abcd;

    public static MethodContextBuffer Done => new MethodContextBuffer(Completed);

    public readonly byte[] buff;
    public readonly int size;

    public MethodContextBuffer(byte[] buff, int size)
    {
        this.buff = buff;
        this.size = size;
    }

    public MethodContextBuffer(int error)
    {
        buff = null;
        size = error;
    }

    public bool allDone() => size == Completed && buff == null;

    public bool Error() => size != 0 && size != Completed && buff == null;
}

// Reads method contexts from an .mch file, optionally restricted to a list of indexes, a hash,
// or an offset/increment combination, using the paired .mct table of contents when there is one.
public sealed class MethodContextReader : IDisposable
{
    private readonly object _lock = new object();
    private readonly FileStream fileHandle;
    private readonly long fileSize;
    private readonly TOCFile tocFile = new TOCFile();
    private int[] Indexes;
    private readonly string Hash;
    private readonly int Offset;
    private readonly int Increment;
    private HashSet<string> excludedMethods;

    private int curMCIndex;
    private int curIndexPos;
    private int curTOCIndex;

    public MethodContextReader(string inputFileName, int[] indexes = null, string hash = null, int offset = -1, int increment = -1)
    {
        Indexes = indexes;
        Hash = hash;
        Offset = offset;
        Increment = increment;

        string mchFileName;
        string tocFileName = CheckForPairedFile(inputFileName, ".mch", ".mct");
        if (tocFileName != null)
        {
            mchFileName = inputFileName;
        }
        else
        {
            mchFileName = CheckForPairedFile(inputFileName, ".mct", ".mch");
            if (mchFileName != null)
                tocFileName = inputFileName;
            else
                mchFileName = inputFileName;
        }

        if (tocFileName != null)
            tocFile.LoadToc(tocFileName);

        try
        {
            FileOptions options = hasTOC() && hasIndex() ? FileOptions.None : FileOptions.SequentialScan;
            fileHandle = new FileStream(mchFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, options);
            fileSize = fileHandle.Length;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open file '{mchFileName}'. GetLastError()={ErrorHandling.LastError(e)}");
        }

        ReadExcludedMethods(mchFileName);
    }

    public void Dispose() => fileHandle?.Dispose();

    public bool isValid() => fileHandle != null;

    public bool hasIndex() => Indexes != null && Indexes.Length > 0;

    public bool hasTOC() => tocFile.GetTocCount() > 0;

    public int GetMethodContextIndex() => curMCIndex;

    private static bool IsRegularFile(string path) => File.Exists(path);

    // Looks for foo.origSuffix.newSuffix or foo.newSuffix, but only if foo.origSuffix exists.
    public static string CheckForPairedFile(string fileName, string origSuffix, string newSuffix)
    {
        int suffixOffset = fileName.LastIndexOf('.');
        if (suffixOffset <= 0 || !string.Equals(fileName.Substring(suffixOffset), origSuffix, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!IsRegularFile(fileName))
            return null;

        string tmp = fileName + newSuffix;
        if (IsRegularFile(tmp))
            return tmp;

        tmp = fileName.Substring(0, suffixOffset) + newSuffix;
        if (IsRegularFile(tmp))
            return tmp;

        return null;
    }

    private bool atEof() => fileHandle.Position == fileSize;

    private MethodContextBuffer ReadMethodContextNoLock(bool justSkip)
    {
        if (atEof())
            return MethodContextBuffer.Done;

        Span<byte> header = stackalloc byte[6];
        ErrorHandling.AssertMsg(fileHandle.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length,
            "ReadFile(...) == TRUE", "Truncated method context header");
        ErrorHandling.AssertMsg(header[0] == 'm' && header[1] == 'c', "(buff[0] == 'm') && (buff[1] == 'c')", "Didn't find magic number");
        uint totalLen = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(2));

        curMCIndex++;
        if (justSkip)
        {
            fileHandle.Seek(totalLen + 2, SeekOrigin.Current);
            return new MethodContextBuffer(0);
        }

        byte[] buff2 = new byte[(long)totalLen + 2];
        fileHandle.ReadAtLeast(buff2, buff2.Length, throwOnEndOfStream: false);
        return new MethodContextBuffer(buff2, (int)totalLen);
    }

    private MethodContextBuffer ReadMethodContext(bool justSkip = false)
    {
        lock (_lock)
        {
            return ReadMethodContextNoLock(justSkip);
        }
    }

    public MethodContextBuffer GetNextMethodContext()
    {
        try
        {
            return GetNextMethodContextHelper();
        }
        catch (SpmiException)
        {
            Logger.LogError($"Method {GetMethodContextIndex()} is of low integrity.");
            return new MethodContextBuffer(-1);
        }
    }

    private MethodContextBuffer GetNextMethodContextHelper()
    {
        if (Offset > 0 && Increment > 0)
            return GetNextMethodContextFromOffsetIncrement();

        if (hasIndex())
        {
            if (curIndexPos < Indexes.Length)
                return GetNextMethodContextFromIndexes();
            return MethodContextBuffer.Done;
        }

        if (Hash != null)
            return GetNextMethodContextFromHash();

        return ReadMethodContext();
    }

    private MethodContextBuffer GetNextMethodContextFromIndexes()
    {
        if (hasTOC())
            return GetSpecificMethodContext(Indexes[curIndexPos++]);

        int curMethod = curIndexPos != 0 ? Indexes[curIndexPos - 1] : 0;
        int nextMethod = Indexes[curIndexPos++];
        while (++curMethod < nextMethod)
        {
            MethodContextBuffer mcb = ReadMethodContext(justSkip: true);
            if (mcb.allDone() || mcb.Error())
                return mcb;
        }
        return ReadMethodContext();
    }

    private MethodContextBuffer GetNextMethodContextFromHash()
    {
        if (hasTOC())
        {
            for (; curTOCIndex < tocFile.GetTocCount(); curTOCIndex++)
            {
                TOCElement element = tocFile.GetElement(curTOCIndex);
                if (string.Equals(Hash, element.HashString, StringComparison.OrdinalIgnoreCase))
                {
                    curTOCIndex++;
                    return GetSpecificMethodContext(element.Number);
                }
            }
            return MethodContextBuffer.Done;
        }

        while (true)
        {
            MethodContextBuffer mcb = ReadMethodContext();
            if (mcb.allDone() || mcb.Error())
                return mcb;

            if (!MethodContext.Initialize(-1, mcb.buff, (uint)mcb.size, readCompileResults: true, out MethodContext mc))
                return new MethodContextBuffer(-1);

            if (string.Equals(Hash, mc.dumpMethodHashToBuffer(), StringComparison.OrdinalIgnoreCase))
                return mcb;
        }
    }

    private MethodContextBuffer GetNextMethodContextFromOffsetIncrement()
    {
        int methodNumber = curMCIndex > 0 ? curMCIndex + Increment : Offset;

        if (hasTOC())
        {
            if (tocFile.GetTocCount() >= methodNumber)
                return GetSpecificMethodContext(methodNumber);
            return MethodContextBuffer.Done;
        }

        while (curMCIndex + 1 < methodNumber)
        {
            MethodContextBuffer mcb = ReadMethodContext(justSkip: true);
            if (mcb.allDone() || mcb.Error())
                return mcb;
        }
        return ReadMethodContext();
    }

    // Progress through the method contexts, compared against TotalWork(): the index position with
    // an index list, the method context number with a TOC, or else the byte offset in the file.
    public double Progress()
    {
        if (hasIndex())
            return curIndexPos;
        if (hasTOC())
            return curMCIndex;
        lock (_lock)
        {
            return fileHandle.Position;
        }
    }

    public double TotalWork()
    {
        if (hasIndex())
            return Indexes.Length;
        if (hasTOC())
            return tocFile.GetTocCount();
        return fileSize;
    }

    public double PercentComplete() => 100.0 * Progress() / TotalWork();

    // Note that the TOC is 0-based and method context numbers are 1-based.
    // Returns -1 for not found, or -2 for not indexed.
    private long GetOffset(int methodNumber)
    {
        if (!hasTOC())
            return -2;

        int low = 0;
        int high = tocFile.GetTocCount() - 1;
        while (low <= high)
        {
            int pos = (high + low) / 2;
            TOCElement element = tocFile.GetElement(pos);
            if (element.Number == methodNumber)
                return element.Offset;
            if (element.Number > methodNumber)
                high = pos - 1;
            else
                low = pos + 1;
        }
        return -1;
    }

    public MethodContextBuffer GetSpecificMethodContext(int methodNumber)
    {
        long pos = GetOffset(methodNumber);
        if (pos < 0)
            return new MethodContextBuffer(-3);

        lock (_lock)
        {
            fileHandle.Seek(pos, SeekOrigin.Begin);
            MethodContextBuffer mcb = ReadMethodContextNoLock(justSkip: false);
            curMCIndex = methodNumber;
            return mcb;
        }
    }

    // Reads the hashes of methods to exclude from a paired .exc file, if there is one.
    private void ReadExcludedMethods(string mchFileName)
    {
        int suffixOffset = mchFileName.LastIndexOf('.');
        if (suffixOffset < 0)
        {
            Logger.LogError($"Failed to get file extension from {mchFileName}");
            return;
        }

        string excludeFileName = CheckForPairedFile(mchFileName, mchFileName.Substring(suffixOffset), ".exc");
        if (excludeFileName == null)
            return;

        string text;
        try
        {
            text = File.ReadAllText(excludeFileName, Encoding.ASCII);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open file '{excludeFileName}'. GetLastError()={ErrorHandling.LastError(e)}");
            return;
        }

        excludedMethods = new HashSet<string>(StringComparer.Ordinal);
        int counter = 0;
        foreach (string hash in text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (hash.Length == SuperPmi.Hash.MM3_HASH_BUFFER_SIZE - 1)
            {
                excludedMethods.Add(hash);
                counter++;
            }
            else
            {
                Logger.LogInfo($"The exclude file contains wrong values: {hash}.");
            }
        }
        Logger.LogInfo($"Exclude file {excludeFileName} contains {counter} methods.");
    }

    public bool IsMethodExcluded(MethodContext mc)
        => excludedMethods != null && excludedMethods.Contains(mc.dumpMethodHashToBuffer());

    public void Reset(int[] newIndexes)
    {
        fileHandle.Seek(0, SeekOrigin.Begin);
        Indexes = newIndexes;
        curIndexPos = 0;
        curMCIndex = 0;
        curTOCIndex = 0;
    }
}
