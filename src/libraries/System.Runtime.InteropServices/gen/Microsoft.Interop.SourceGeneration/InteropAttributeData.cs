// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Flags used to indicate members on source-generated interop attributes.
    /// </summary>
    [Flags]
    public enum InteropAttributeMember
    {
        None = 0,
        SetLastError = 1 << 0,
        StringMarshalling = 1 << 1,
        StringMarshallingCustomType = 1 << 2,
        All = ~None
    }

    /// <summary>
    /// Common data for all source-generated-interop trigger attributes
    /// </summary>
    public record InteropAttributeData
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public InteropAttributeMember IsUserDefined { get; init; }
        public bool SetLastError { get; init; }
        public StringMarshalling StringMarshalling { get; init; }
        public INamedTypeSymbol? StringMarshallingCustomType { get; init; }
    }
}
