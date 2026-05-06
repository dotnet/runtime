// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

/// <summary>
/// Tests that the System.ComponentModel.TypeDescriptor.InterfaceType
/// property works as expected when used in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        Type type = TypeDescriptor.InterfaceType;

        // Tests that the ctor for System.ComponentModel.TypeDescriptor+TypeDescriptorInterface is not trimmed out.
        object obj = Activator.CreateInstance(type);
        string expectedObjTypeNamePrefix = "System.ComponentModel.TypeDescriptor+TypeDescriptorInterface, System.ComponentModel.TypeConverter, Version=";

        return obj != null && obj.GetType().AssemblyQualifiedName.StartsWith(expectedObjTypeNamePrefix)
            ? 100
            : -1;
    }
}
