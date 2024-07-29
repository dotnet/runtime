// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using SharedTypes;
using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class RefParameters
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "out_params")]
            public static partial int OutParameters(out int values, out IntFields numValues);
        }
    }
    public class RefParameters
    {
        [Fact]
        public void OutParametersAreDefaultInitialized()
        {
            int myInt = 12;
            IntFields myIntFields = new IntFields() { a = 12, b = 12, c = 12 };
            NativeExportsNE.RefParameters.OutParameters(out myInt, out myIntFields);
            Debug.Assert(myInt == default);
            Debug.Assert(myIntFields.a == default && myIntFields.b == default && myIntFields.c == default);
        }
    }
}
