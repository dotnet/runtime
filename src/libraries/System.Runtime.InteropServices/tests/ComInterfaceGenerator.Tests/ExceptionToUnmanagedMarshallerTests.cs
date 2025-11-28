// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class ExceptionToUnmanagedMarshallerTests
    {
        [CustomMarshaller(typeof(Exception), MarshalMode.UnmanagedToManagedOut, typeof(CustomExceptionAsHResultMarshaller))]
        public static class CustomExceptionAsHResultMarshaller
        {
            public static int LastException { get; private set; }

            public static int ConvertToUnmanaged(Exception e)
            {
                return LastException = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
            }
        }

        [GeneratedComInterface(ExceptionToUnmanagedMarshaller = typeof(CustomExceptionAsHResultMarshaller))]
        [Guid("90F3657D-23B7-44C4-85DD-80BD1F5266E7")]
        public partial interface ICustomExceptionMarshallerComInterface
        {
            void ThrowException();
        }

        [GeneratedComClass]
        public partial class CustomExceptionMarshallerComClass : ICustomExceptionMarshallerComInterface
        {
            public void ThrowException() => throw new NotImplementedException();
        }

        [Fact]
        public void TestCustomExceptionMarshaller()
        {
            CustomExceptionMarshallerComClass comObject = new();
            StrategyBasedComWrappers wrappers = new();
            nint nativeUnknown = wrappers.GetOrCreateComInterfaceForObject(comObject, CreateComInterfaceFlags.None);

            ICustomExceptionMarshallerComInterface rcw = (ICustomExceptionMarshallerComInterface)wrappers.GetOrCreateObjectForComInstance(nativeUnknown, CreateObjectFlags.None);
            Marshal.Release(nativeUnknown);

            const int E_NOTIMPL = unchecked((int)0x80004001);
            Assert.Throws<NotImplementedException>(rcw.ThrowException);
            Assert.Equal(E_NOTIMPL, CustomExceptionAsHResultMarshaller.LastException);
        }
    }
}
