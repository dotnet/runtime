// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace SuperPmi;

// Iterates sequentially over the method contexts of a file, optionally only the given sorted indexes.
public sealed class MethodContextIterator : IDisposable
{
    private const int ProgressRate = 1000;

    private readonly int[] m_indexes; // null means all method contexts
    private readonly bool m_progressReport;
    private readonly Stopwatch m_timer = new Stopwatch();
    private FileStream m_hFile;
    private long m_fileSize;
    private int m_index;

    public MethodContextIterator(bool progressReport = false)
        : this(null, progressReport)
    {
    }

    public MethodContextIterator(int[] indexes, bool progressReport = false)
    {
        m_indexes = indexes;
        m_progressReport = progressReport;
    }

    public MethodContext Current { get; private set; }

    // The file offset of the current method context.
    public long CurrentPos { get; private set; }

    public int MethodContextNumber { get; private set; }

    public bool Initialize(string fileName)
    {
        try
        {
            m_hFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, FileOptions.SequentialScan);
            m_fileSize = m_hFile.Length;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open file '{fileName}'. GetLastError()={ErrorHandling.LastError(e)}");
            return false;
        }

        if (m_progressReport)
            m_timer.Start();
        return true;
    }

    // Gives the current method context to the caller; the iterator no longer refers to it.
    public MethodContext CurrentTakeOwnership()
    {
        MethodContext ret = Current;
        Current = null;
        return ret;
    }

    public bool MoveNext()
    {
        Current = null;

        while (true)
        {
            CurrentPos = m_hFile.Position;
            if (CurrentPos >= m_fileSize)
                return false;

            MethodContextNumber++;

            if (m_progressReport && (MethodContextNumber % ProgressRate) == 0)
            {
                Logger.LogVerbose($"Loaded {MethodContextNumber} at {(int)(ProgressRate / m_timer.Elapsed.TotalSeconds)} per second");
                m_timer.Restart();
            }

            if (!MethodContext.Initialize(MethodContextNumber, m_hFile, readCompileResults: true, out MethodContext mc))
                return false;
            Current = mc;

            if (m_indexes == null)
                break;
            if (m_index == m_indexes.Length)
                return false; // beyond the list of indexes
            if (m_indexes[m_index] == MethodContextNumber)
            {
                m_index++;
                break;
            }
        }

        return true;
    }

    // Returns false if closing the file failed.
    public bool Destroy()
    {
        m_hFile?.Dispose();
        m_hFile = null;
        Current = null;

        if (m_indexes != null && m_index < m_indexes.Length)
        {
            Logger.LogWarning($"Didn't use all of index count input: {m_index} < {m_indexes.Length} (i.e., didn't see MC #{m_indexes[m_index]})");
        }
        return true;
    }

    public void Dispose() => Destroy();
}
