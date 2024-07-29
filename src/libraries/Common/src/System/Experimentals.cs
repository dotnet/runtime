// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class Experimentals
    {
        internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

        // Please see docs\project\list-of-diagnostics.md for instructions on the steps required
        // to introduce an experimental API, claim a diagnostic id, and ensure the
        // "aka.ms/dotnet-warnings/{0}" URL points to documentation for the API.
        // The diagnostic IDs reserved for experimental APIs are SYSLIB5### (SYSLIB5001 - SYSLIB5999).

        // When an API is no longer marked as experimental, the diagnostic ID should be removed from this file
        // but retained in the table in docs\project\list-of-diagnostics.md to prevent reuse. Be sure to remove
        // suppressions from the codebase as well.

        // Tensor<T> and related APIs are marked as [Experimental] in .NET 9
        internal const string TensorTDiagId = "SYSLIB5001";

        // When adding a new diagnostic ID, add it to the table in docs\project\list-of-diagnostics.md as well.
        // Keep new const identifiers above this comment.
    }
}
