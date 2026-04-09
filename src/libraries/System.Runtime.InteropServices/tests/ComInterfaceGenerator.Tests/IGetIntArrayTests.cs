// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;
using Xunit.Sdk;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class IGetIntArrayTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_get_and_set_int_array")]
        public static partial void* NewNativeObject();

        [Fact]
        public unsafe void CallRcwFromGeneratedComInterface()
        {
            var ptr = NewNativeObject(); // new_native_object
            var cw = new StrategyBasedComWrappers();
            var obj = cw.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

            var intObj = (IGetIntArray)obj;
            Assert.Equal<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, intObj.GetInts());
        }
    }
}
