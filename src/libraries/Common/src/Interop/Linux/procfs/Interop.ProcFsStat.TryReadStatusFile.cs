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
    internal static partial class procfs
    {
        internal const string RootPath = "/proc/";
        private const string StatusFileName = "/status";

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

        internal static string GetStatusFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + StatusFileName;
        }

        internal static bool TryReadStatusFile(int pid, out ParsedStatus result)
        {
            bool b = TryParseStatusFile(GetStatusFilePathForProcess(pid), out result);
#if DEBUG
            Debug.Assert(!b || result.Pid == pid, "Expected process ID from status file to match supplied pid");
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

                ReadOnlySpan<char> name = line.Slice(0, startIndex);
                ReadOnlySpan<char> value = line.Slice(startIndex + 1);
                bool valueParsed = true;

#if DEBUG
                if (name.SequenceEqual("Pid"))
                {
                    valueParsed = int.TryParse(value, out results.Pid);
                }
                else
#endif
                if (name.SequenceEqual("VmHWM"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out results.VmHWM);
                }
                else if (name.SequenceEqual("VmRSS"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out results.VmRSS);
                }
                else if (name.SequenceEqual("VmData"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out ulong vmData);
                    results.VmData += vmData;
                }
                else if (name.SequenceEqual("VmSwap"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out results.VmSwap);
                }
                else if (name.SequenceEqual("VmSize"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out results.VmSize);
                }
                else if (name.SequenceEqual("VmPeak"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out results.VmPeak);
                }
                else if (name.SequenceEqual("VmStk"))
                {
                    valueParsed = ulong.TryParse(value[..^3], out ulong vmStack);
                    results.VmData += vmStack;
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
