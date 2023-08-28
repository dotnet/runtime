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
        void Method(StatelessFinallyType param);
        void MethodIn(in StatelessFinallyType param);
        void MethodOut(out StatelessFinallyType param);
        void MethodRef(ref StatelessFinallyType param);
        StatelessFinallyType Return();
        [PreserveSig]
        StatelessFinallyType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessFinallyMarshalling : IStatelessFinallyMarshalling
    {
        public void Method(StatelessFinallyType param) { _ = param.I; }
        public void MethodIn(in StatelessFinallyType param) { _ = param.I; }
        public void MethodOut(out StatelessFinallyType param) { param = new StatelessFinallyType { I = 42 }; }
        public void MethodRef(ref StatelessFinallyType param) { _ = param.I; param = new StatelessFinallyType { I = 200 }; }
        public StatelessFinallyType Return() => new StatelessFinallyType { I = 200 };
        public StatelessFinallyType ReturnPreserveSig() => new StatelessFinallyType { I = 200 };
    }

    [NativeMarshalling(typeof(StatelessFinallyTypeMarshaller))]
    internal class StatelessFinallyType
    {
        public int I;
    }

    internal struct StatelessFinallyNative
    {
        public int i;
    }

    [CustomMarshaller(typeof(StatelessFinallyType), MarshalMode.Default, typeof(StatelessFinallyTypeMarshaller))]
    internal static class StatelessFinallyTypeMarshaller
    {
        public static int FreeCount { get; private set; }
        public static StatelessFinallyNative ConvertToUnmanaged(StatelessFinallyType managed) => new StatelessFinallyNative() { i = managed.I };

        public static StatelessFinallyType ConvertToManagedFinally(StatelessFinallyNative unmanaged) => new StatelessFinallyType { I = unmanaged.i };

        public static void Free(StatelessFinallyNative unmanaged) => FreeCount++;
    }
}
