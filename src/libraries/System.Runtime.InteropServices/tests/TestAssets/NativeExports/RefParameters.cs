// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharedTypes;

namespace NativeExports
{
    public static unsafe class RefParameters
    {
        [UnmanagedCallersOnly(EntryPoint = "out_params")]
        public static void EnsureOutParamIsDefault(int* ri, [DNNE.C99Type("struct int_fields*")]IntFields* rif)
        {
            int i = *ri;
            Debug.Assert(i == default);
            IntFields _if = *rif;
            Debug.Assert(_if.a == default && _if.b == default && _if.c == default);
        }
    }
}
