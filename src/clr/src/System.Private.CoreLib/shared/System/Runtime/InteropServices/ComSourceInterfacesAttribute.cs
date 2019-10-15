// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ComSourceInterfacesAttribute : Attribute
    {
        public ComSourceInterfacesAttribute(string sourceInterfaces)
        {
            Value = sourceInterfaces;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface)
        {
            Value = sourceInterface.FullName!;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2)
        {
            Value = sourceInterface1.FullName + "\0" + sourceInterface2.FullName;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3)
        {
            Value = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3, Type sourceInterface4)
        {
            Value = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName + "\0" + sourceInterface4.FullName;
        }

        public string Value { get; }
    }
}
