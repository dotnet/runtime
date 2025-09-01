// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("f252bddd-aac0-4004-acfd-b39f73fb9791")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedFromExternalDerived : IExternalDerived
#pragma warning restore SYSLIB1230
{
    string GetName();
}
