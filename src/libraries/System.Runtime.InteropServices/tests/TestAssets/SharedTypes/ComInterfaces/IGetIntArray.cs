// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(_guid)]
    internal partial interface IGetIntArray
    {
        [return: MarshalUsing(ConstantElementCount = 10)]
        int[] GetInts();

        public const string _guid = "7D802A0A-630A-4C8E-A21F-771CC9031FB9";
    }
}
