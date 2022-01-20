// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("browser")]
    public static partial class JavaScriptMarshal
    {
        public static InvokeJSResult InvokeJSFunctionByName (string internedFunctionName) => throw new PlatformNotSupportedException();
        public static InvokeJSResult InvokeJSFunctionByName<T1> (string internedFunctionName, T1 arg1) => throw new PlatformNotSupportedException();
        public static InvokeJSResult InvokeJSFunctionByName<T1, T2> (string internedFunctionName, T1 arg1, T2 arg2)  => throw new PlatformNotSupportedException();
        public static InvokeJSResult InvokeJSFunctionByName<T1, T2, T3> (string internedFunctionName, T1 arg1, T2 arg2, T3 arg3)  => throw new PlatformNotSupportedException();
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("browser")]
    public enum InvokeJSResult : int
    {
        Success = 0,
        InvalidFunctionName,
        FunctionNotFound,
        InvalidArgumentCount,
        InvalidArgumentType,
        MissingArgumentType,
        NullArgumentPointer,
        FunctionHadReturnValue,
        FunctionThrewException,
        InternalError,
    }
}
