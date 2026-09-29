// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("EE6D1F2A-3418-4317-A87C-35488F6546AB")]
    internal partial interface IInt
    {
        public int Get();
        public void Set(int value);
        public void SwapRef(ref int value);
        public void GetOut(out int value);
        public void SetIn(in int value);
    }

    [GeneratedComClass]
    internal partial class IIntImpl : IInt
    {
        int _data;

        public int Get() => _data;

        public void Set(int value) => _data = value;

        public void SetIn(in int value)
        {
            _data = value;
        }

        public void GetOut(out int value)
        {
            value = _data;
        }

        public void SwapRef(ref int value)
        {
            var tmp = _data;
            _data = value;
            value = tmp;
        }
    }
}
