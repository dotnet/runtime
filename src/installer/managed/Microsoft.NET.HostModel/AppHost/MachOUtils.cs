// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Melanzana.MachO;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class MachOUtils
    {
        /// <summary>
        /// This Method is a utility to adjust the apphost MachO-header
        /// to include the bytes added by the single-file bundler at the end of the file.
        ///
        /// The tool assumes the following layout of the executable
        ///
        /// * MachoHeader (64-bit, executable, not swapped integers)
        /// * LoadCommands
        ///     LC_SEGMENT_64 (__PAGEZERO)
        ///     LC_SEGMENT_64 (__TEXT)
        ///     LC_SEGMENT_64 (__DATA)
        ///     LC_SEGMENT_64 (__LINKEDIT)
        ///     ...
        ///     LC_SYMTAB
        ///
        ///  * ... Different Segments
        ///
        ///  * The __LINKEDIT Segment (last)
        ///      * ... Different sections ...
        ///      * SYMTAB (last)
        ///
        /// The MAC codesign tool places several restrictions on the layout
        ///   * The __LINKEDIT segment must be the last one
        ///   * The __LINKEDIT segment must cover the end of the file
        ///   * All bytes in the __LINKEDIT segment are used by other linkage commands
        ///     (ex: symbol/string table, dynamic load information etc)
        ///
        /// In order to circumvent these restrictions, we:
        ///    * Extend the __LINKEDIT segment to include the bundle-data
        ///    * Extend the string table to include all the bundle-data
        ///      (that is, the bundle-data appear as strings to the loader/codesign tool).
        ///
        ///  This method has certain limitations:
        ///    * The bytes for the bundler may be unnecessarily loaded at startup
        ///    * Tools that process the string table may be confused (?)
        ///    * The string table size is limited to 4GB. Bundles larger than that size
        ///      cannot be accommodated by this utility.
        ///
        /// </summary>
        /// <param name="filePath">Path to the AppHost</param>
        /// <returns>
        ///  True if
        ///    - The input is a MachO binary, and
        ///    - The additional bytes were successfully accommodated within the MachO segments.
        ///   False otherwise
        /// </returns>
        public static bool AdjustHeadersForBundle(string filePath)
        {
            ulong fileLength = (ulong)new FileInfo(filePath).Length;
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
            if (!MachReader.IsMachOImage(fileStream))
            {
                return false;
            }

            var objectFile = MachReader.Read(fileStream).Single();

            var linkEditSegment = objectFile.Segments.Single(x => x.Name == "__LINKEDIT");
            var symbolTable = (MachSymbolTable)objectFile.LoadCommands.Single(x => x is MachSymbolTable);

            ulong newStringTableSize = fileLength - symbolTable.StringTableData.FileOffset;
            if (newStringTableSize > uint.MaxValue)
            {
                // Too big, too bad;
                return false;
            }
            symbolTable.StringTableData.Size = newStringTableSize;
            linkEditSegment.Size = fileLength;
            MachWriter.Write(fileStream, objectFile);
            return true;
        }
    }
}
