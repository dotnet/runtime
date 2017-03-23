// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text
{
    public partial class StringBuilder
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe extern void ReplaceBufferInternal(char* newBuffer, int newLength);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe extern void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength);
    }
}
