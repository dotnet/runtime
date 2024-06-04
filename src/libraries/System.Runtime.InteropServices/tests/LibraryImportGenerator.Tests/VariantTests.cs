// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_variant_bstr_length")]
        public static partial int GetVTBStrLength([MarshalAs(UnmanagedType.Struct)] in object obj);
    }

    public class VariantTests
    {
        [Fact]
        public void MarshalAsStruct_UsesVariantMarshaller()
        {
            Assert.Equal(3, NativeExportsNE.GetVTBStrLength("Foo"));
        }
    }
}
