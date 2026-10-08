// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("9C13EAA7-AC8B-4B72-9E91-B3F2C0C3F8A3")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedExternalIndexersAndProperties : IExternalIndexersAndProperties
#pragma warning restore SYSLIB1230
{
    // Marker member appended after the inherited indexer and property slots so we can verify
    // the derived vtable layout extends rather than displaces the base layout.
    int Marker { get; }
}
