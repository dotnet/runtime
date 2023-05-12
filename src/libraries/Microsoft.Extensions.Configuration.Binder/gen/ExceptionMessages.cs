// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    // Runtime exception messages; not localized so we keep them in source.
    internal static class ExceptionMessages
    {
        public const string CannotSpecifyBindNonPublicProperties = "The configuration binding source generator does not support 'BinderOptions.BindNonPublicProperties'.";
        public const string FailedBinding = "Failed to convert configuration value at '{0}' to type '{1}'.";
        public const string MissingConfig = "'{0}' was set on the provided {1}, but the following properties were not found on the instance of {2}: {3}";
        public const string TypeNotDetectedAsInput = "Unable to bind to type '{0}': generator did not detect the type as input.";
        public const string TypeNotSupportedAsInput = "Unable to bind to type '{0}': generator does not support this type as input to this method.";
    }
}
