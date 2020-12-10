// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        private const string MapsFileName = "/maps";

        private static string GetMapsFilePathForProcess(int pid)
        {
            return RootPath + pid.ToString(CultureInfo.InvariantCulture) + MapsFileName;
        }

        private static (long Start, int Size) TryParseAddressRange(string s, ref int start, ref int end)
        {
            int pos = s.IndexOf('-', start, end - start);
            if (pos > 0)
            {
                if (long.TryParse(s.AsSpan(start, pos), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long startingAddress) &&
                    long.TryParse(s.AsSpan(pos + 1, end - (pos + 1)), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long endingAddress))
                {
                    return (startingAddress, (int)(endingAddress - startingAddress));
                }
            }

            return default;
        }

        private static bool HasReadAndExecFlags(string s, ref int start, ref int end)
        {
            bool sawRead = false, sawExec = false;
            for (int i = start; i < end; i++)
            {
                if (s[i] == 'r')
                    sawRead = true;
                else if (s[i] == 'x')
                    sawExec = true;
            }

            return sawRead & sawExec;
        }

        internal static ProcessModuleCollection? ParseMapsModules(int pid)
        {
            try
            {
                return ParseMapsModulesCore(File.ReadLines(GetMapsFilePathForProcess(pid)));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return null;
        }

        private static ProcessModuleCollection ParseMapsModulesCore(IEnumerable<string> lines)
        {
            Debug.Assert(lines != null);

            ProcessModule? module = null;
            ProcessModuleCollection modules = new(capacity: 0);
            bool moduleHasReadAndExecFlags = false;

            foreach (string line in lines)
            {
                // Use a StringParser to avoid string.Split costs
                var parser = new StringParser(line, separator: ' ', skipEmpty: true);

                // Parse the address start and size
                (long Start, int Size) addressRange = parser.ParseRaw(TryParseAddressRange);

                if (addressRange == default)
                {
                    Commit();
                    continue;
                }

                // Parse the permissions
                bool lineHasReadAndExecFlags = parser.ParseRaw(HasReadAndExecFlags);

                // Skip past the offset, dev, and inode fields
                parser.MoveNext();
                parser.MoveNext();
                parser.MoveNext();

                // we only care about the named modules
                if (!parser.MoveNext())
                {
                    Commit();
                    continue;
                }

                string pathname = parser.ExtractCurrentToEnd();
                bool isContinuation = module?.FileName == pathname && (long)module.BaseAddress + module.ModuleMemorySize == addressRange.Start;

                if (isContinuation)
                {
                    module!.ModuleMemorySize += addressRange.Size;
                    moduleHasReadAndExecFlags |= lineHasReadAndExecFlags;
                    continue;
                }

                Commit();

                moduleHasReadAndExecFlags = lineHasReadAndExecFlags;
                module = new ProcessModule
                {
                    FileName = pathname,
                    ModuleName = Path.GetFileName(pathname),
                    ModuleMemorySize = addressRange.Size,
                    EntryPointAddress = IntPtr.Zero // unknown
                };

                // on 32-bit platforms, it throws System.OverflowException without the void* cast.
                unsafe
                {
                    module.BaseAddress = new IntPtr(unchecked((void*)addressRange.Start));
                }
            }

            // Commit last line.
            Commit();

            return modules;

            void Commit()
            {
                // we only add module to collection, if at least one row had 'r' and 'x' set.
                if (moduleHasReadAndExecFlags && module is not null)
                {
                    modules.Add(module);
                    module = null;
                }
            }
        }
    }
}
