// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;

namespace Internal.Reflection.Execution
{
    internal sealed class MethodInvokeInfo
    {
        public IntPtr LdFtnResult { get; set; }
        public IntPtr DynamicInvokeMethod { get; set; }
        public IntPtr DynamicInvokeGenericDictionary { get; set; }
        public MethodBase MethodInfo { get; set; }
        public IntPtr VirtualResolveData { get; set; }
    }
}
