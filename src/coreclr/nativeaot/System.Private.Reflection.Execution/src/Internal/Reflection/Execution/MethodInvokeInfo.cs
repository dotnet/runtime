// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;

namespace Internal.Reflection.Execution
{
    internal sealed class MethodInvokeInfo : DynamicInvokeInfo
    {
        public MethodInvokeInfo(MethodBase method, IntPtr invokeThunk)
            : base(method, invokeThunk)
        {
        }

        public IntPtr LdFtnResult { get; set; }
        public IntPtr VirtualResolveData { get; set; }
    }
}
