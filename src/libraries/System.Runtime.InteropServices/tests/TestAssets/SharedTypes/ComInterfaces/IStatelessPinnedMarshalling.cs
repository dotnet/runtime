// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessPinnedMarshalling
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessPinnedType param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessPinnedType param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessPinnedType param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessPinnedType param, int size);
        StatelessPinnedType Return();
        [PreserveSig]
        StatelessPinnedType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatelessPinnedMarshalling : IStatelessPinnedMarshalling
    {
        public void Method([MarshalUsing(CountElementName = "size")] StatelessPinnedType param, int size) { }
        public void MethodIn([MarshalUsing(CountElementName = "size")] in StatelessPinnedType param, int size) { }
        public void MethodOut([MarshalUsing(CountElementName = "size")] out StatelessPinnedType param, int size) { param = new StatelessPinnedType { I = 42 }; }
        public void MethodRef([MarshalUsing(CountElementName = "size")] ref StatelessPinnedType param, int size) { param = new StatelessPinnedType { I = 200 }; }
        public StatelessPinnedType Return() => throw new NotImplementedException();
        public StatelessPinnedType ReturnPreserveSig() => throw new NotImplementedException();
    }

    [NativeMarshalling(typeof(StatelessPinnedTypeMarshaller))]
    internal class StatelessPinnedType
    {
        public int I;
    }

    internal struct StatelessPinnedStruct
    {
        public int I;
    }

    [CustomMarshaller(typeof(StatelessPinnedType), MarshalMode.Default, typeof(StatelessPinnedTypeMarshaller))]
    internal static class StatelessPinnedTypeMarshaller
    {
        public static int FreeCount { get; private set; }
        public static nint ConvertToUnmanaged(StatelessPinnedType managed) => managed.I;

        public static StatelessPinnedType ConvertToManaged(nint unmanaged) => new StatelessPinnedType { I = (int)unmanaged };

        static StatelessPinnedStruct _field;
        public static ref StatelessPinnedStruct GetPinnableReference(StatelessPinnedType unmanaged)
        {
            _field = new StatelessPinnedStruct() { I = unmanaged.I };
            return ref _field;
        }

        public static void Free(nint unmanaged) => FreeCount++;
    }
}
