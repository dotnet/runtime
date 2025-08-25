// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessMarshalling
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessType param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessType param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessType param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessType param, int size);
        StatelessType Return();
        [PreserveSig]
        StatelessType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessMarshalling : IStatelessMarshalling
    {
        public void Method(StatelessType param, int size) { }
        public void MethodIn(in StatelessType param, int size) { }
        public void MethodOut(out StatelessType param, int size) { param = new StatelessType { I = 42 }; }
        public void MethodRef(ref StatelessType param, int size) { param = new StatelessType { I = 200 }; }
        public StatelessType Return() => throw new NotImplementedException();
        public StatelessType ReturnPreserveSig() => throw new NotImplementedException();
    }

    [NativeMarshalling(typeof(StatelessTypeMarshaller))]
    internal class StatelessType
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessType), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.ManagedToUnmanagedOut, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.UnmanagedToManagedIn, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.ElementOut, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.ElementIn, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.ElementRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatelessType), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
    internal static class StatelessTypeMarshaller
    {
        public static int AllFreeCount => Bidirectional.FreeCount + UnmanagedToManaged.FreeCount + ManagedToUnmanaged.FreeCount;

        internal static class Bidirectional
        {
            public static int FreeCount { get; private set; }
            public static nint ConvertToUnmanaged(StatelessType managed) => managed.I;

            public static StatelessType ConvertToManaged(nint unmanaged) => new StatelessType { I = (int)unmanaged };

            public static void Free(nint unmanaged) => FreeCount++;
        }

        internal static class ManagedToUnmanaged
        {
            public static int FreeCount { get; private set; }
            public static void Free(nint unmanaged) => FreeCount++;
            public static nint ConvertToUnmanaged(StatelessType managed) => managed.I;
        }

        internal static class UnmanagedToManaged
        {
            public static int FreeCount { get; private set; }
            public static void Free(nint unmanaged) => FreeCount++;
            public static StatelessType ConvertToManaged(nint unmanaged) => new StatelessType { I = (int)unmanaged };
        }
    }
}
