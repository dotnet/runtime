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
    public unsafe partial class IGetAndSetIntTests
    {

        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_get_and_set_int")]
        public static partial void* NewNativeObject();

        [Fact]
        public unsafe void CallRcwFromGeneratedComInterface()
        {
            var ptr = NewNativeObject(); // new_native_object
            var cw = new StrategyBasedComWrappers();
            var obj = cw.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

            var intObj = (IGetAndSetInt)obj;
            Assert.Equal(0, intObj.GetInt());
            intObj.SetInt(2);
            Assert.Equal(2, intObj.GetInt());
        }
    }
}
