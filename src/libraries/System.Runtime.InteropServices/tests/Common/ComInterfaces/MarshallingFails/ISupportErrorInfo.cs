// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces.MarshallingFails
{
    [GeneratedComInterface]
    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal partial interface ISupportErrorInfo
    {
        [PreserveSig]
        int InterfaceSupportsErrorInfo(in Guid riid);
    }
}
