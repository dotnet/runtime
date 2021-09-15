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

        private static string GetMapsFilePathForProcess(int pid) => string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{MapsFileName}");

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
                if (!TryParseMapsEntry(line, out (long StartAddress, int Size, bool HasReadAndExecFlags, string Path) parsedLine))
                {
                    // Invalid entry for the purposes of ProcessModule parsing,
                    // discard flushing the current module if it exists.
                    CommitCurrentModule();
                    continue;
                }

                // Check if entry is a continuation of the current module.
                if (module is not null &&
                    module.FileName == parsedLine.Path &&
                    (long)module.BaseAddress + module.ModuleMemorySize == parsedLine.StartAddress)
                {
                    // Is continuation, update the current module.
                    module.ModuleMemorySize += parsedLine.Size;
                    moduleHasReadAndExecFlags |= parsedLine.HasReadAndExecFlags;
                    continue;
                }

                // Not a continuation, commit any current modules and create a new one.
                CommitCurrentModule();

                module = new ProcessModule
                {
                    FileName = parsedLine.Path,
                    ModuleName = Path.GetFileName(parsedLine.Path),
                    ModuleMemorySize = parsedLine.Size,
                    EntryPointAddress = IntPtr.Zero // unknown
                };

                // on 32-bit platforms, it throws System.OverflowException with IntPtr.ctor(Int64),
                // so we use IntPtr.ctor(void*) to skip the overflow checking.
                unsafe
                {
                    module.BaseAddress = new IntPtr((void*)parsedLine.StartAddress);
                }

                moduleHasReadAndExecFlags = parsedLine.HasReadAndExecFlags;
            }

            // Commit any pending modules.
            CommitCurrentModule();

            return modules;

            void CommitCurrentModule()
            {
                // we only add module to collection, if at least one row had 'r' and 'x' set.
                if (moduleHasReadAndExecFlags && module is not null)
                {
                    modules.Add(module);
                    module = null;
                }
            }
        }

        private static bool TryParseMapsEntry(string line, out (long StartAddress, int Size, bool HasReadAndExecFlags, string Path) parsedLine)
        {
            // Use a StringParser to avoid string.Split costs
            var parser = new StringParser(line, separator: ' ', skipEmpty: true);

            // Parse the address start and size
            (long start, int size) = parser.ParseRaw(TryParseAddressRange);

            if (size < 0)
            {
                parsedLine = default;
                return false;
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
                parsedLine = default;
                return false;
            }

            // Parse the pathname
            string pathname = parser.ExtractCurrentToEnd();
            parsedLine = (start, size, lineHasReadAndExecFlags, pathname);
            return true;

            static (long Start, int Size) TryParseAddressRange(string s, ref int start, ref int end)
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

                return (0, -1);
            }

            static bool HasReadAndExecFlags(string s, ref int start, ref int end)
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
        }
    }
}
