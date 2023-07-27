// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A26-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessFinallyMarshalling
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessFinallyType param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessFinallyType param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessFinallyType param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessFinallyType param, int size);
        StatelessFinallyType Return();
        [PreserveSig]
        StatelessFinallyType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessFinallyMarshalling : IStatelessFinallyMarshalling
    {
        public void Method([MarshalUsing(CountElementName = "size")] StatelessFinallyType param, int size) { }
        public void MethodIn([MarshalUsing(CountElementName = "size")] in StatelessFinallyType param, int size) { }
        public void MethodOut([MarshalUsing(CountElementName = "size")] out StatelessFinallyType param, int size) { param = new StatelessFinallyType { I = 42 }; }
        public void MethodRef([MarshalUsing(CountElementName = "size")] ref StatelessFinallyType param, int size) { param = new StatelessFinallyType { I = 200 }; }
        public StatelessFinallyType Return() => throw new NotImplementedException();
        public StatelessFinallyType ReturnPreserveSig() => throw new NotImplementedException();
    }

    [NativeMarshalling(typeof(StatelessFinallyTypeMarshaller))]
    internal class StatelessFinallyType
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessFinallyType), MarshalMode.Default, typeof(StatelessFinallyTypeMarshaller))]
    internal static class StatelessFinallyTypeMarshaller
    {
        public static int FreeCount { get; private set; }
        public static nint ConvertToUnmanaged(StatelessFinallyType managed) => managed.I;

        public static StatelessFinallyType ConvertToManagedFinally(nint unmanaged) => new StatelessFinallyType { I = (int)unmanaged };

        public static void Free(nint unmanaged) => FreeCount++;
    }
}
