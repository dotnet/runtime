// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class NativeMarshallingAttributeTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_unique_marshalling")]
        internal static partial IUniqueMarshalling NewUniqueMarshalling();

        [Fact]
        public void MethodReturningComInterfaceReturnsUniqueInstance()
        {
            // When a COM interface method returns the same interface type,
            // it should return a new managed instance, not the cached one
            var obj = NewUniqueMarshalling();
            obj.SetValue(42);

            var returnedObj = obj.GetThis();

            // Should be a different managed object
            Assert.NotSame(obj, returnedObj);

            // But should refer to the same underlying COM object
            Assert.Equal(42, returnedObj.GetValue());

            // Modifying through one should affect the other
            returnedObj.SetValue(100);
            Assert.Equal(100, obj.GetValue());
        }
    }
}
