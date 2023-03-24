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
            [UnmanagedObjectUnwrapperAttribute<VTableGCHandlePair<IStaticMethodTable>>]
            internal partial interface IStaticMethodTable : IUnmanagedInterfaceType
            {
                static void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;

                [VirtualMethodIndex(0, Direction = MarshalDirection.ManagedToUnmanaged, ImplicitThisParameter = false)]
                int Add(int x, int y);
                [VirtualMethodIndex(1, Direction = MarshalDirection.ManagedToUnmanaged, ImplicitThisParameter = false)]
                int Multiply(int x, int y);
            }

            [NativeMarshalling(typeof(StaticMethodTableMarshaller))]
            public class StaticMethodTable : IStaticMethodTable.Native, IUnmanagedVirtualMethodTableProvider
            {
                private readonly void* _vtableStart;

                public StaticMethodTable(void* vtableStart)
                {
                    _vtableStart = vtableStart;
                }

                public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(Type type)
                {
                    Assert.Equal(typeof(IStaticMethodTable), type);
                    return new VirtualMethodTableInfo((void*)null, (void**)_vtableStart);
                }
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
