// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
    public sealed class ClassInterfaceAttribute : Attribute
    {
        public ClassInterfaceAttribute(ClassInterfaceType classInterfaceType)
        {
            Value = classInterfaceType;
        }
        public ClassInterfaceAttribute(short classInterfaceType)
        {
            Value = (ClassInterfaceType)classInterfaceType;
        }

        public ClassInterfaceType Value { get; }
    }
}
