// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace ManagedDll1
{
    public class Class1
    {
        [DllImport(@"MAWSPINative", EntryPoint="GetInt", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetIntNative();

        public static int GetInt()
        {
            return GetIntNative();
        }
    }
}