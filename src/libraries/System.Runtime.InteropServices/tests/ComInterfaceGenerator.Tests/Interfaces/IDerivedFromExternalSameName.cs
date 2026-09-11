// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

[GeneratedComInterface]
[Guid("b7c27add-cbc3-41c6-9e15-ddaf167c21a9")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedFromExternalSameNameA : IExternalSameNameA
#pragma warning restore SYSLIB1230
{
}

[GeneratedComInterface]
[Guid("5193a610-67dd-46b4-9f8c-238047c97ffb")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
internal partial interface IDerivedFromExternalSameNameB : IExternalSameNameB
#pragma warning restore SYSLIB1230
{
}
