// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class DefaultInterfaceAttribute : Attribute
    {
        public DefaultInterfaceAttribute(Type defaultInterface)
        {
            throw new PlatformNotSupportedException();
        }

        public Type DefaultInterface => throw new PlatformNotSupportedException();
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class WindowsRuntimeImportAttribute : Attribute
    {
        internal WindowsRuntimeImportAttribute() { throw new PlatformNotSupportedException(); }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class InterfaceImplementedInVersionAttribute : Attribute
    {
        public InterfaceImplementedInVersionAttribute(Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion)
        {
            throw new PlatformNotSupportedException();
        }

        public Type InterfaceType => throw new PlatformNotSupportedException();

        public byte MajorVersion => throw new PlatformNotSupportedException();

        public byte MinorVersion => throw new PlatformNotSupportedException();

        public byte BuildVersion => throw new PlatformNotSupportedException();

        public byte RevisionVersion => throw new PlatformNotSupportedException();
    }

    // Applies to read-only array parameters
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ReadOnlyArrayAttribute : Attribute
    {
        public ReadOnlyArrayAttribute() { throw new PlatformNotSupportedException(); }
    }

    // Applies to write-only array parameters
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class WriteOnlyArrayAttribute : Attribute
    {
        public WriteOnlyArrayAttribute() { throw new PlatformNotSupportedException(); }
    }

    // This attribute is applied on the return value to specify the name of the return value.
    // In WindowsRuntime all parameters including return value need to have unique names.
    // This is essential in JS as one of the ways to get at the results of a method in JavaScript is via a Dictionary object keyed by parameter name.
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnValueNameAttribute : Attribute
    {
        public ReturnValueNameAttribute(string name)
        {
            throw new PlatformNotSupportedException();
        }

        public string Name => throw new PlatformNotSupportedException();
    }
}
