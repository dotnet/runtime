// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal unsafe sealed class FixedBufferAttribute : Attribute
    {
        public FixedBufferAttribute(Type elementType, int length)
        {
        }
    }
}
