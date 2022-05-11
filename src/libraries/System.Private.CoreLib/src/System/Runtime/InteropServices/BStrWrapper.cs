// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_BSTR.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class BStrWrapper
    {
        public BStrWrapper(string? value)
        {
            WrappedObject = value;
        }

        public BStrWrapper(object? value)
        {
            WrappedObject = (string?)value;
        }

        public string? WrappedObject { get; }
    }
}
