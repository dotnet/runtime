// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// LibraryImportAttribute data
    /// </summary>
    internal sealed record LibraryImportData(string ModuleName) : InteropAttributeData
    {
        public string EntryPoint { get; init; }
    }
}
