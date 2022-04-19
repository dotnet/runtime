// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// VirtualMethodIndexAttribute data
    /// </summary>
    internal sealed record VirtualMethodIndexData(int Index) : InteropAttributeData
    {
        public bool ImplicitThisParameter { get; init; }

        public CustomTypeMarshallerDirection Direction { get; init; }
    }
}
