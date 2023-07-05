// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop.JavaScript
{
    /// <summary>
    /// JSImportAttribute data
    /// </summary>
    /// <remarks>
    /// The names of these members map directly to those on the
    /// JSImportAttribute and should not be changed.
    /// </remarks>
    internal sealed record JSImportData(string FunctionName, string? ModuleName)
    {
    }

    /// <summary>
    /// JSExportAttribute data
    /// </summary>
    /// <remarks>
    /// The names of these members map directly to those on the
    /// JSExportAttribute and should not be changed.
    /// </remarks>
    internal sealed record JSExportData()
    {
    }
}
