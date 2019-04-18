// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class InterfaceTypeAttribute : Attribute
    {
        public InterfaceTypeAttribute(ComInterfaceType interfaceType)
        {
            Value = interfaceType;
        }
        public InterfaceTypeAttribute(short interfaceType)
        {
            Value = (ComInterfaceType)interfaceType;
        }

        public ComInterfaceType Value { get; }
    }
}
