// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_UNKNOWN.
    public sealed class UnknownWrapper
    {
        public UnknownWrapper(object? obj)
        {
            WrappedObject = obj;
        }

        public object? WrappedObject { get; }
    }
}
