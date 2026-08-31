// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    public partial interface IExternalDerived : IExternalBase
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        bool GetBool();
        public void SetBool([MarshalAs(UnmanagedType.Bool)] bool x);
        new public const string IID = "594DF2B9-66CE-490D-9D05-34646675B188";
    }
}
