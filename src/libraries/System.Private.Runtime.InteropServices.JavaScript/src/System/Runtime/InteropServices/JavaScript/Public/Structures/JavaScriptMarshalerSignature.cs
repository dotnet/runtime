// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshalerSignature
    {
        // intentionaly opaque internal structure

        public unsafe int TotalBufferLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ArgumentsBufferLength + ExtraBufferLength;
            }
        }
    }
}
