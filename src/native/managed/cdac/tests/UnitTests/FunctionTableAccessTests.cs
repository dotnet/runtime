// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class FunctionTableAccessTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void QueryInterfaceFromIXCLRDataProcess_ReturnsProcess3(MockTarget.Architecture arch)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch).Build();
        SOSDacImpl impl = new(target, legacyObj: null);
        void* process = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);

        try
        {
            Guid iid = typeof(IXCLRDataProcess3).GUID;
            int hr = Marshal.QueryInterface((nint)process, in iid, out nint process3);

            Assert.Equal(HResults.S_OK, hr);
            Assert.NotEqual(nint.Zero, process3);

            try
            {
                Guid iidIUnknown = new("00000000-0000-0000-C000-000000000046");
                Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)process, in iidIUnknown, out nint identity));
                try
                {
                    foreach (Type interfaceType in new[] { typeof(IXCLRDataProcess2), typeof(IXCLRDataProcess) })
                    {
                        Guid baseIid = interfaceType.GUID;
                        Assert.Equal(HResults.S_OK, Marshal.QueryInterface(process3, in baseIid, out nint baseInterface));
                        try
                        {
                            nint baseIdentity = nint.Zero;
                            try
                            {
                                Assert.Equal(HResults.S_OK, Marshal.QueryInterface(baseInterface, in iidIUnknown, out baseIdentity));
                                Assert.Equal(identity, baseIdentity);
                            }
                            finally
                            {
                                if (baseIdentity != nint.Zero)
                                    Marshal.Release(baseIdentity);
                            }
                        }
                        finally
                        {
                            Marshal.Release(baseInterface);
                        }
                    }
                }
                finally
                {
                    Marshal.Release(identity);
                }

                IXCLRDataProcess3 process3Interface =
                    ComInterfaceMarshaller<IXCLRDataProcess3>.ConvertToManaged((void*)process3)!;
                uint bytesNeeded = uint.MaxValue;
                uint entries = uint.MaxValue;

                hr = process3Interface.GetFunctionTable(
                    new ClrDataAddress(0),
                    0,
                    null,
                    &bytesNeeded,
                    &entries);

                Assert.Equal(HResults.E_NOTIMPL, hr);
                Assert.Equal(0u, bytesNeeded);
                Assert.Equal(0u, entries);

                entries = uint.MaxValue;
                hr = process3Interface.GetFunctionTable(
                    new ClrDataAddress(0),
                    0,
                    null,
                    null,
                    &entries);

                Assert.Equal(HResults.E_POINTER, hr);
                Assert.Equal(0u, entries);

                bytesNeeded = uint.MaxValue;
                hr = process3Interface.GetFunctionTable(
                    new ClrDataAddress(0),
                    0,
                    null,
                    &bytesNeeded,
                    null);

                Assert.Equal(HResults.E_POINTER, hr);
                Assert.Equal(0u, bytesNeeded);

                Assert.Equal(
                    HResults.E_POINTER,
                    process3Interface.GetFunctionTable(new ClrDataAddress(0), 0, null, null, null));
            }
            finally
            {
                Marshal.Release(process3);
            }
        }
        finally
        {
            ComInterfaceMarshaller<IXCLRDataProcess>.Free(process);
        }
    }
}
