// Licensed to the .NET Foundation under one or more agreements.
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
        internal partial class NoImplicitThis
        {
            public readonly record struct NoCasting;

            internal partial interface IStaticMethodTable
            {
                public static readonly NoCasting TypeKey = default;

                [VirtualMethodIndex(0, ImplicitThisParameter = false)]
                int Add(int x, int y);
                [VirtualMethodIndex(1, ImplicitThisParameter = false)]
                int Multiply(int x, int y);
            }

            [NativeMarshalling(typeof(StaticMethodTableMarshaller))]
            public class StaticMethodTable : IStaticMethodTable.Native, IUnmanagedVirtualMethodTableProvider<NoCasting>
            {
                private readonly void* _vtableStart;

                public StaticMethodTable(void* vtableStart)
                {
                    _vtableStart = vtableStart;
                }

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(NoCasting typeKey) => new VirtualMethodTableInfo(IntPtr.Zero, new ReadOnlySpan<IntPtr>(_vtableStart, 2));
            }

            [CustomTypeMarshaller(typeof(StaticMethodTable), CustomTypeMarshallerKind.Value, Direction = CustomTypeMarshallerDirection.Out, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
            struct StaticMethodTableMarshaller
            {
                private void* _nativeValue;

                public void FromNativeValue(void* value) => _nativeValue = value;

                public StaticMethodTable ToManaged() => new StaticMethodTable(_nativeValue);
            }

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_static_function_table")]
            public static partial StaticMethodTable GetStaticFunctionTable();
        }
    }

    public class NoImplicitThisTests
    {
        [Fact]
        public void ValidateNoImplicitThisFunctionCallsSucceed()
        {
            int x = 7;
            int y = 56;

            NativeExportsNE.NoImplicitThis.IStaticMethodTable staticMethodTable = NativeExportsNE.NoImplicitThis.GetStaticFunctionTable();

            Assert.Equal(x + y, staticMethodTable.Add(x, y));
            Assert.Equal(x * y, staticMethodTable.Multiply(x, y));
        }
    }
}
