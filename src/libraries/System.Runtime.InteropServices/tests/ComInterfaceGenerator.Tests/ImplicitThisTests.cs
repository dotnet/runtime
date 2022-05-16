﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    internal unsafe partial class NativeExportsNE
    {
        internal partial class ImplicitThis
        {
            public readonly record struct NoCasting;

            internal partial interface INativeObject
            {
                public static readonly NoCasting TypeKey = default;

                [VirtualMethodIndex(0)]
                int GetData();
                [VirtualMethodIndex(1)]
                void SetData(int x);
            }

            [NativeMarshalling(typeof(NativeObjectMarshaller))]
            public class NativeObject : INativeObject.Native, IUnmanagedVirtualMethodTableProvider<NoCasting>, IDisposable
            {
                private readonly void* _pointer;

                public NativeObject(void* pointer)
                {
                    _pointer = pointer;
                }

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(NoCasting typeKey) => new VirtualMethodTableInfo((IntPtr)_pointer, new ReadOnlySpan<IntPtr>(*(void**)_pointer, 2));

                public void Dispose()
                {
                    DeleteNativeObject(_pointer);
                }
            }

            [CustomTypeMarshaller(typeof(NativeObject), CustomTypeMarshallerKind.Value, Direction = CustomTypeMarshallerDirection.Out, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
            struct NativeObjectMarshaller
            {
                private void* _nativeValue;

                public void FromNativeValue(void* value) => _nativeValue = value;

                public NativeObject ToManaged() => new NativeObject(_nativeValue);
            }

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "new_native_object")]
            public static partial NativeObject NewNativeObject();

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "delete_native_object")]
            public static partial void DeleteNativeObject(void* obj);
        }
    }

    public class ImplicitThisTests
    {
        [Fact]
        public void ValidateImplicitThisFunctionCallsSucceed()
        {
            const int value = 42;

            using NativeExportsNE.ImplicitThis.NativeObject obj = NativeExportsNE.ImplicitThis.NewNativeObject();

            NativeExportsNE.ImplicitThis.INativeObject nativeObjInterface = obj;

            nativeObjInterface.SetData(value);

            Assert.Equal(value, nativeObjInterface.GetData());
        }
    }
}
