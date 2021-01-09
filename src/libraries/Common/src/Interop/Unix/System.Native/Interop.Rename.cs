// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        ///<summary>Flags to call to Rename</summary>
        [Flags] internal enum RenameFlags : int {
            ///<summary>no flags set</summary>
            None = 0,
            ///<summary>Replace newPath if it exists</summary>
            Replace = 1,
            ///<summary>Rename is operating on a directory; that is, called from Directory.Move</summary>
            Directory = 2,
        }

        /// <summary>
        /// Renames a file, moving to the correct destination if necessary. There are many edge cases to this call, check man 2 rename for more info
        /// </summary>
        /// <param name="oldPath">Path to the source item</param>
        /// <param name="newPath">Path to the desired new item</param>
        /// <param name="flags">bit flags: flags &amp; 1: whether to overwrite target or not; flags &amp; 2: whether this is Directory.Move or not</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1
        /// </returns>
        /// <remarks>clobber may be implemented in terms of stat() depending on platform; not all races can be eliminated</remarks>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Rename", SetLastError = true)]
        internal static extern int Rename(string oldPath, string newPath, RenameFlags flags);
    }
}
