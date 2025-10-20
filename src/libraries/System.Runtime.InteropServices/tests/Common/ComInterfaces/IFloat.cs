// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("9FA4A8A9-2D8F-48A8-B6FB-B44B5F1B9FB6")]
    internal partial interface IFloat
    {
        float Get();
        void Set(float value);
    }

    [GeneratedComClass]
    internal partial class IFloatImpl : IFloat
    {
        float _data;
        public float Get() => _data;
        public void Set(float value) => _data = value;
    }
}
