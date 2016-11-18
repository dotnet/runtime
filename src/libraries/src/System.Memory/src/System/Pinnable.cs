// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    //
    // This class exists solely so that arbitrary objects can be Unsafe-casted to it to get a ref to the start of the user data.
    //
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class Pinnable<T>
    {
        public T Data;
    }
}
