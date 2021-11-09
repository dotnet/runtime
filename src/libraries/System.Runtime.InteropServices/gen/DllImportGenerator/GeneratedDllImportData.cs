// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// Flags used to indicate members on GeneratedDllImport attribute.
    /// </summary>
    [Flags]
    public enum DllImportMember
    {
        None = 0,
        CharSet = 1 << 0,
        EntryPoint = 1 << 1,
        ExactSpelling = 1 << 2,
        PreserveSig = 1 << 3,
        SetLastError = 1 << 4,
        All = ~None
    }

    /// <summary>
    /// GeneratedDllImportAttribute data
    /// </summary>
    /// <remarks>
    /// The names of these members map directly to those on the
    /// DllImportAttribute and should not be changed.
    /// </remarks>
    public sealed record GeneratedDllImportData(string ModuleName)
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public DllImportMember IsUserDefined { get; init; }
        public CharSet CharSet { get; init; }
        public string? EntryPoint { get; init; }
        public bool ExactSpelling { get; init; }
        public bool PreserveSig { get; init; }
        public bool SetLastError { get; init; }
    }
}
