// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    /// <summary>
    /// Helper to process type names.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility.", error: true)]
    public class TypeNameHelper
    {
        /// <summary>Pretty prints a type name.</summary>
        public static string GetTypeDisplayName(Type type)
            => Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(type);
    }
}
