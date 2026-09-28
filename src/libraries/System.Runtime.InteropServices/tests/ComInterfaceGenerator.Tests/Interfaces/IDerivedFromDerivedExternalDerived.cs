// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("b158aaf2-85a3-40e7-805f-5797580a05f2")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedFromDerivedExternalDerived : IDerivedFromExternalDerived
#pragma warning restore SYSLIB1230
{
    float GetFloat();
}
