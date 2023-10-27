// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("A4857398-06FB-4A6E-81DB-35461BE999C5")]
    internal partial interface IInterface
    {
        public IInt Get();
        public void SetInt(IInt value);
        public void SwapRef(ref IInt value);
        public void GetOut(out IInt value);
        public void InInt(in IInt value);
    }

    [GeneratedComClass]
    internal partial class IInterfaceImpl : IInterface
    {
        IInt _data = new IIntImpl();

        IInt IInterface.Get() => _data;

        void IInterface.InInt(in IInt value)
        {
            var i = value.Get();
        }

        void IInterface.GetOut(out IInt value)
        {
            value = _data;
        }

        void IInterface.SwapRef(ref IInt value)
        {
            var tmp = _data;
            _data = value;
            value = tmp;
        }

        void IInterface.SetInt(IInt value)
        {
            int x = value.Get();
            value.Set(x);
        }
    }
}
