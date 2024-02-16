// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NativeExports
{
    public static unsafe class Variant
    {
        [UnmanagedCallersOnly(EntryPoint = "get_variant_bstr_length")]
        public static int GetVTBStrLength([DNNE.C99Type("void*")] ComVariant* variant)
        {
            return variant->As<string>().Length;
        }
    }
}
