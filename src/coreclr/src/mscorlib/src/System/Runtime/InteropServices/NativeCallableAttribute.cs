// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
** Any method marked with NativeCallableAttribute can be directly called from 
** native code.The function token can be loaded to a local variable using LDFTN and
** passed as a callback to native method.
=============================================================================*/

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeCallableAttribute : Attribute
    {
        public NativeCallableAttribute()
        {
        }
        // Optional. If omitted , compiler will choose one for you.
        public CallingConvention CallingConvention;
        // Optional. If omitted, then the method is native callable, but no EAT is emitted.
        public string EntryPoint;
    }
}