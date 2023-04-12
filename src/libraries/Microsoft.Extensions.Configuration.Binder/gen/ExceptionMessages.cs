// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    // Runtime exception messages; not localized so we keep them in source.
    internal static class ExceptionMessages
    {
        public const string TypeNotDetectedAsInput = "Unable to bind to type '{0}': generator did not detect the type as input.";
        public const string FailedBinding = "Failed to convert configuration value at '{0}' to type '{1}'.";
    }
}
