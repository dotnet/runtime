// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string RootPath = "/proc/";
        internal const string Self = "self";
        private const string StatusFileName = "/status";

        // Normally the '/proc' filesystem uses the same pid namespace as the process.
        // With rootless containers, it may happen that these pid namespaces do not match
        // because the container doesn't have permissions to change '/proc' but it can
        // create a new pid namespace.
        //
        // When that happens, the numeric ids used by the '/proc' filesystem no longer match with
        // the process pid namespace. We can still access information for the current process
        // using '/proc/self'. For other processes, we can't map pids to the proc pids so we musn't
        // use '/proc' as that would return information for non-existing/wrong/inaccessible processes.
        //
        // The 'ProcPid' type represents a pid used by the '/proc' filesystem.
        // This type provides a type-safe way to distingish the proc pids from the process pid namespace pids,
        // which are passed as regular 'int's.
        internal enum ProcPid : int
        {
            Invalid = -1,
            Self = 0,     // Information for the current process, accessible through '/proc/self'.
        }

        internal struct ParsedStatus
        {
#if DEBUG
            internal int Pid;
#endif
            internal ulong VmHWM;
            internal ulong VmRSS;
            internal ulong VmData;
            internal ulong VmSwap;
            internal ulong VmSize;
            internal ulong VmPeak;
        }

        internal static string GetStatusFilePathForProcess(ProcPid pid) =>
            pid == ProcPid.Self ? $"{RootPath}{Self}{StatusFileName}" :
                                  string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{StatusFileName}");

        internal static bool TryReadStatusFile(ProcPid pid, out ParsedStatus result)
        {
            bool b = TryParseStatusFile(GetStatusFilePathForProcess(pid), out result);
#if DEBUG
            Debug.Assert(!b || (ProcPid)result.Pid == pid || pid == ProcPid.Self, "Expected process ID from status file to match supplied pid");
#endif
            return b;
        }

        internal static bool TryParseStatusFile(string statusFilePath, out ParsedStatus result)
        {
            if (!TryReadFile(statusFilePath, out string? fileContents))
            {
                // Between the time that we get an ID and the time that we try to read the associated stat
                // file(s), the process could be gone.
                result = default(ParsedStatus);
                return false;
            }

            ParsedStatus results = default(ParsedStatus);
            foreach (ReadOnlySpan<char> line in fileContents.AsSpan().EnumerateLines())
            {
                int startIndex = line.IndexOf(':');
                if (startIndex == -1)
                {
                    break;
                }

                ReadOnlySpan<char> value = line.Slice(startIndex + 1);
                bool valueParsed = true;

                switch (line.Slice(0, startIndex)) // name
                {
#if DEBUG
                    case "Pid":
                        valueParsed = int.TryParse(value, out results.Pid);
                        break;
#endif
                    case "VmHWM":
                        valueParsed = ulong.TryParse(value[..^3], out results.VmHWM);
                        break;

                    case "VmRSS":
                        valueParsed = ulong.TryParse(value[..^3], out results.VmRSS);
                        break;

                    case "VmData":
                        valueParsed = ulong.TryParse(value[..^3], out ulong vmData);
                        results.VmData += vmData;
                        break;

                    case "VmSwap":
                        valueParsed = ulong.TryParse(value[..^3], out results.VmSwap);
                        break;

                    case "VmSize":
                        valueParsed = ulong.TryParse(value[..^3], out results.VmSize);
                        break;

                    case "VmPeak":
                        valueParsed = ulong.TryParse(value[..^3], out results.VmPeak);
                        break;

                    case "VmStk":
                        valueParsed = ulong.TryParse(value[..^3], out ulong vmStack);
                        results.VmData += vmStack;
                        break;
                }

                Debug.Assert(valueParsed);
            }

            results.VmData *= 1024;
            results.VmPeak *= 1024;
            results.VmSize *= 1024;
            results.VmSwap *= 1024;
            results.VmRSS *= 1024;
            results.VmHWM *= 1024;
            result = results;
            return true;
        }

        private static bool TryReadFile(string path, [NotNullWhen(true)] out string? contents)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            byte[] bytes = ArrayPool<byte>.Shared.Rent(4096);
            int count = 0;

            try
            {
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: false);

                while (true)
                {
                    int read = fileStream.Read(bytes, count, bytes.Length - count);
                    if (read == 0)
                    {
                        contents = Encoding.UTF8.GetString(bytes, 0, count);
                        return true;
                    }

                    count += read;
                    if (count >= bytes.Length)
                    {
                        byte[] temp = ArrayPool<byte>.Shared.Rent(bytes.Length * 2);
                        Array.Copy(bytes, temp, bytes.Length);
                        byte[] toReturn = bytes;
                        bytes = temp;
                        ArrayPool<byte>.Shared.Return(toReturn);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex.InnerException is IOException)
            {
                contents = null;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
    }
}
