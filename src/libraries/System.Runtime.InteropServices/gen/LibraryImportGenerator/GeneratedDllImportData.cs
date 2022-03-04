// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Flags used to indicate members on GeneratedDllImport attribute.
    /// </summary>
    [Flags]
    public enum DllImportMember
    {
        None = 0,
        EntryPoint = 1 << 0,
        SetLastError = 1 << 1,
        StringMarshalling = 1 << 2,
        StringMarshallingCustomType = 1 << 3,
        All = ~None
    }

    /// <summary>
    /// GeneratedDllImportAttribute data
    /// </summary>
    /// <remarks>
    /// The names of these members map directly to those on the
    /// DllImportAttribute and should not be changed.
    /// </remarks>
    internal sealed record GeneratedDllImportData(string ModuleName)
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public DllImportMember IsUserDefined { get; init; }
        public string? EntryPoint { get; init; }
        public bool SetLastError { get; init; }
        public StringMarshalling StringMarshalling { get; init; }
        public INamedTypeSymbol? StringMarshallingCustomType { get; init; }
    }
}
