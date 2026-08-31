// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IDerivedDerived : IDerived
    {
        void SetFloat(float name);

        float GetFloat();

        internal new const string IID = "7F0DB364-3C04-4487-9193-4BB05DC7B654";
    }

    [GeneratedComClass]
    internal partial class DerivedDerived : Derived, IDerivedDerived
    {
        float _data = 0;
        public float GetFloat() => _data;
        public void SetFloat(float name) => _data = name;
    }
}
