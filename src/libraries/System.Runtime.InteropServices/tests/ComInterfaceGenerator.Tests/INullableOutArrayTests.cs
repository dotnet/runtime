// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public partial class INullableOutArrayTests
    {
        [Fact]
        // Regression test for https://github.com/dotnet/runtime/issues/118135
        public unsafe void NullableOutArray_Marshalling_Works()
        {
            // Arrange
            INullableOutArray originalObject = new INullableOutArrayImpl();
            ComWrappers cw = new StrategyBasedComWrappers();
            nint ptr = cw.GetOrCreateComInterfaceForObject(originalObject, CreateComInterfaceFlags.None);
            object obj = cw.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.None);

            INullableOutArray throughInterface = (INullableOutArray)obj;

            var (__this, __vtable) = ((IUnmanagedVirtualMethodTableProvider)throughInterface).GetVirtualMethodTableInfoForKey(typeof(INullableOutArray));
            var __target = (delegate* unmanaged[MemberFunction]<void*, int, int*, int*, int>)__vtable[4];

            int[] outputArray = new int[5];

            fixed (int* __outputArray_native = &ArrayMarshaller<int, int>.ManagedToUnmanagedIn.GetPinnableReference(outputArray))
            {
                int hr = __target(__this, 5, __outputArray_native, null);
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
