// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class PreserveSigTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "create_point_provider")]
        public static partial void* NewNativeObject();

        [Fact]
        public unsafe void CallRcwFromGeneratedComInterface()
        {
            var ptr = NewNativeObject(); // new_native_object
            var cw = new StrategyBasedComWrappers();
            var obj = (IPointProvider)cw.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

            var expected = new Point(42, 63);

            obj.SetPoint(expected);

            Assert.Equal(expected, obj.GetPoint());
        }
    }
}
