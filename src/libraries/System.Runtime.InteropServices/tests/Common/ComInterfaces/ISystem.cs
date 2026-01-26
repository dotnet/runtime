// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
    [Guid(IID)]
    internal partial interface ISystem
    {
        // Make sure method names System and Microsoft don't interfere with framework type / method references
        void Microsoft(int p);
        void System(int p);
        public const string IID = "3BFFE3FD-D11E-4195-8250-0C73321977A0";
    }
}
