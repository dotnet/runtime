// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    internal unsafe partial class NativeExportsNE
    {
        internal partial class NoImplicitThis
        {
            public readonly record struct NoCasting;

            internal partial interface IStaticMethodTable : IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>
            {
                static int IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>.VirtualMethodTableLength => 2;
                static void* IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>.VirtualMethodTableManagedImplementation => null;

                static void* IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>.GetUnmanagedWrapperForObject(IStaticMethodTable obj) => null;

                static IStaticMethodTable IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>.GetObjectForUnmanagedWrapper(void* ptr) => null;

                static NoCasting IUnmanagedInterfaceType<IStaticMethodTable, NoCasting>.TypeKey => default;

                [VirtualMethodIndex(0, Direction = MarshalDirection.ManagedToUnmanaged, ImplicitThisParameter = false)]
                int Add(int x, int y);
                [VirtualMethodIndex(1, Direction = MarshalDirection.ManagedToUnmanaged, ImplicitThisParameter = false)]
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

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(NoCasting typeKey) =>
                    new VirtualMethodTableInfo(IntPtr.Zero, new ReadOnlySpan<IntPtr>(_vtableStart, IUnmanagedVirtualMethodTableProvider<NoCasting>.GetVirtualMethodTableLength<IStaticMethodTable>()));
            }

            [CustomMarshaller(typeof(StaticMethodTable), MarshalMode.ManagedToUnmanagedOut, typeof(StaticMethodTableMarshaller))]
            static class StaticMethodTableMarshaller
            {
                public static StaticMethodTable ConvertToManaged(void* value) => new StaticMethodTable(value);
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
