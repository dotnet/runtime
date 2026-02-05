// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal static class JsonTestSerializerOptions
    {
        internal static JsonSerializerOptions DisallowDuplicateProperties =>
            field ??= new() { AllowDuplicateProperties = false };

        internal static JsonSerializerOptions DisallowDuplicatePropertiesIgnoringCase =>
            field ??= new() { AllowDuplicateProperties = false, PropertyNameCaseInsensitive = true };
    }
}
