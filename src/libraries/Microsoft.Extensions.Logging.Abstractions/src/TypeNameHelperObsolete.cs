// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    /// <summary>
    /// Helper to process type names.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. Do not use.", error: true)]
    public static class TypeNameHelper
    {
        /// <summary>Gets the display name of a type from an instance.</summary>
        [return: NotNullIfNotNull(nameof(item))]
        public static string? GetTypeDisplayName(object? item, bool fullName = true)
            => Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(item, fullName);

        /// <summary>Pretty prints a type name.</summary>
        public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = '+')
            => Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(type, fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter);
    }
}
