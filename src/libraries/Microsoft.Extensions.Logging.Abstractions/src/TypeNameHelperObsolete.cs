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
        /// <inheritdoc cref="Abstractions.TypeNameHelper.GetTypeDisplayName(object?, bool)"/>
        [return: NotNullIfNotNull(nameof(item))]
        public static string? GetTypeDisplayName(object? item, bool fullName = true)
            => Abstractions.TypeNameHelper.GetTypeDisplayName(item, fullName);

        /// <inheritdoc cref="Abstractions.TypeNameHelper.GetTypeDisplayName(Type, bool, bool, bool, char)"/>
        public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = '+')
            => Abstractions.TypeNameHelper.GetTypeDisplayName(type, fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter);
    }
}
