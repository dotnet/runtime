// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessCallerAllocateBufferMarshalling
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessCallerAllocatedBufferType param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessCallerAllocatedBufferType param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessCallerAllocatedBufferType param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessCallerAllocatedBufferType param, int size);
        StatelessCallerAllocatedBufferType Return();
        [PreserveSig]
        StatelessCallerAllocatedBufferType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessCallerAllocatedBufferMarshalling : IStatelessCallerAllocateBufferMarshalling
    {
        public void Method([MarshalUsing(CountElementName = "size")] StatelessCallerAllocatedBufferType param, int size) { }
        public void MethodIn([MarshalUsing(CountElementName = "size")] in StatelessCallerAllocatedBufferType param, int size) { }
        public void MethodOut([MarshalUsing(CountElementName = "size")] out StatelessCallerAllocatedBufferType param, int size) { param = new StatelessCallerAllocatedBufferType { I = 42 }; }
        public void MethodRef([MarshalUsing(CountElementName = "size")] ref StatelessCallerAllocatedBufferType param, int size) { param = new StatelessCallerAllocatedBufferType { I = 200 }; }
        public StatelessCallerAllocatedBufferType Return() => throw new NotImplementedException();
        public StatelessCallerAllocatedBufferType ReturnPreserveSig() => throw new NotImplementedException();
    }

    [NativeMarshalling(typeof(StatelessCallerAllocatedBufferTypeMarshaller))]
    internal class StatelessCallerAllocatedBufferType
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessCallerAllocatedBufferType), MarshalMode.Default, typeof(StatelessCallerAllocatedBufferTypeMarshaller))]
    internal static class StatelessCallerAllocatedBufferTypeMarshaller
    {
        public static int FreeCount { get; private set; }
        public static int BufferSize => 64;
        public static nint ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed, Span<byte> buffer) => managed.I;

        public static StatelessCallerAllocatedBufferType ConvertToManaged(nint unmanaged) => new StatelessCallerAllocatedBufferType { I = (int)unmanaged };

        public static void Free(nint unmanaged) => FreeCount++;

        public static nint ConvertToUnmanaged(StatelessCallerAllocatedBufferType managed) => managed.I;
    }
}
