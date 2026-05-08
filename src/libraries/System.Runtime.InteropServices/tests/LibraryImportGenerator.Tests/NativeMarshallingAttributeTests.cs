// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using SharedTypes.ComInterfaces;
using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "new_unique_marshalling")]
        internal static partial IUniqueMarshalling GetUniqueMarshalling();
    }

    public class NativeMarshallingAttributeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public void GetSameComInterfaceTwiceReturnsUniqueInstances()
        {
            // When using NativeMarshalling with UniqueComInterfaceMarshaller,
            // calling GetUniqueMarshalling() twice returns different managed instances for the same COM object
            var obj1 = NativeExportsNE.GetUniqueMarshalling();
            var obj2 = NativeExportsNE.GetUniqueMarshalling();

            Assert.NotSame(obj1, obj2);

            // Both refer to the same underlying COM object (same cached pointer)
            obj1.SetValue(42);
            Assert.Equal(42, obj2.GetValue());

            // Modifying through one should affect the other
            obj2.SetValue(100);
            Assert.Equal(100, obj1.GetValue());
        }
    }
}
