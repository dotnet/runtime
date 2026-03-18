// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("da8eed10-f3f4-42c3-86de-04f2dc56514e")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedExternalBase : IExternalBase
#pragma warning restore SYSLIB1230
{
    string GetName();
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("c3d3990e-5b05-4a9b-adc4-58c521700ece")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedExternalBase2 : IExternalBase
#pragma warning restore SYSLIB1230
{
    string GetName();
}
