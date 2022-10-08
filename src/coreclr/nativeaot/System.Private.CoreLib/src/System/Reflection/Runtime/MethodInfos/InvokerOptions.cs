// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Runtime.MethodInfos
{
    [Flags]
    internal enum InvokerOptions
    {
        None = 0x00000000,
        AllowNullThis = 0x00000001,     // Don't raise an exception if the "thisObject" parameter to Invoker is null.
    }
}
