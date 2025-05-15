// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 618 // Must test deprecated features

[ComVisible(true)]
[Guid(Server.Contract.Guids.ClassInterfaceNotSetTesting)]
public class ClassInterfaceNotSetTesting
{
}

[ComVisible(true)]
[Guid(Server.Contract.Guids.ClassInterfaceNoneTesting)]
[ClassInterface(ClassInterfaceType.None)]
public class ClassInterfaceNoneTesting
{
}

[ComVisible(true)]
[Guid(Server.Contract.Guids.ClassInterfaceAutoDispatchTesting)]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
public class ClassInterfaceAutoDispatchTesting
{
}

[ComVisible(true)]
[Guid(Server.Contract.Guids.ClassInterfaceAutoDualTesting)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class ClassInterfaceAutoDualTesting
{
}

#pragma warning restore 618 // Must test deprecated features