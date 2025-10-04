// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("F4963CBF-10AF-460B-8495-107782187705")]
    internal partial interface IArrayOfStatelessElements
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessType[] param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessType[] param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessType[] param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessType[] param, int size);
        void MethodContentsIn([MarshalUsing(CountElementName = nameof(size))][In] StatelessType[] param, int size);
        void MethodContentsOut([MarshalUsing(CountElementName = nameof(size))][Out] StatelessType[] param, int size);
        void MethodContentsInOut([MarshalUsing(CountElementName = nameof(size))][In, Out] StatelessType[] param, int size);
    }

    [GeneratedComClass]
    internal partial class ArrayOfStatelessElements : IArrayOfStatelessElements
    {
        public void Method(StatelessType[] param, int size)
        {
        }
        public void MethodContentsIn(StatelessType[] param, int size)
        {
            // We should be able to modify the contents and the caller shouldn't see it
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = param[i].I * 2 };
            }
        }
        public void MethodContentsInOut(StatelessType[] param, int size)
        {
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = param[i].I * 2 };
            }
        }
        public void MethodContentsOut(StatelessType[] param, int size)
        {
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = i };
            }
        }
        public void MethodIn(in StatelessType[] param, int size)
        {
            // We should be able to modify the contents and the caller shouldn't see it
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = param[i].I * 2 };
            }
        }
        public void MethodOut(out StatelessType[] param, int size)
        {
            param = new StatelessType[size];
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = i };
            }
        }
        public void MethodRef(ref StatelessType[] param, int size)
        {
            for (int i = 0; i < size; i++)
            {
                param[i] = new StatelessType() { I = param[i].I * 2 };
            }
        }
    }

    [GeneratedComClass]
    internal partial class ArrayOfStatelessElementsThrows : IArrayOfStatelessElements
    {
        public void Method(StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodContentsIn(StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodContentsInOut(StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodContentsOut(StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodIn(in StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodOut(out StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
        public void MethodRef(ref StatelessType[] param, int size) => throw new ManagedComMethodFailureException();
    }
}
