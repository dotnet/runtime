// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler
{
    internal static class IncrementalFailureContract
    {
        internal const int CleanFallbackExitCode = 85;
        internal const int FailureHResult = unchecked((int)0x80131C85);
        internal const string EnableVariable = "DOTNET_ILC_INCREMENTAL";
        internal const string OutputObjectsVariable = "DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS";
        internal const string UpdatedAssembliesVariable = "DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES";

        internal static bool IsCleanFallbackRequested(Exception exception, bool isEnvironmentRequested) =>
            isEnvironmentRequested && exception.HResult == FailureHResult;
    }
}
