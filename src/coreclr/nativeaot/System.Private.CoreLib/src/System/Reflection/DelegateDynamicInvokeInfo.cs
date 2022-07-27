// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    [ReflectionBlocked]
    public sealed class DelegateDynamicInvokeInfo
    {
        public DelegateDynamicInvokeInfo()
        {
        }

        internal MethodInfo InvokeMethod { get; init; }
        internal IntPtr InvokeThunk { get; init; }
        internal IntPtr GenericDictionary { get; init; }
    }
}
