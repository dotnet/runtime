// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Helper to process type names.
    /// </summary>
    public static class TypeNameHelper
    {
        /// <summary>
        /// Gets the display name of a type from an instance.
        /// </summary>
        /// <param name="item">The instance.</param>
        /// <param name="fullName"><see langword="true"/> to use the fully qualified name; otherwise <see langword="false"/>.</param>
        /// <returns>The type display name.</returns>
        [return: NotNullIfNotNull(nameof(item))]
        public static string? GetTypeDisplayName(object? item, bool fullName = true)
            => Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(item, fullName);

        /// <summary>
        /// Pretty print a type name.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="fullName"><see langword="true"/> to print a fully qualified name.</param>
        /// <param name="includeGenericParameterNames"><see langword="true"/> to include generic parameter names.</param>
        /// <param name="includeGenericParameters"><see langword="true"/> to include generic parameters.</param>
        /// <param name="nestedTypeDelimiter">Character to use as a delimiter in nested type names.</param>
        /// <returns>The pretty printed type name.</returns>
        public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = '+')
            => Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(type, fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter);
    }
}
