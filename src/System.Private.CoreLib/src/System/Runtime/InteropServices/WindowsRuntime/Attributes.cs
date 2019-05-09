// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // DefaultInterfaceAttribute marks a WinRT class (or interface group) that has its default interface specified.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class DefaultInterfaceAttribute : Attribute
    {
        private Type m_defaultInterface;

        public DefaultInterfaceAttribute(Type defaultInterface)
        {
            m_defaultInterface = defaultInterface;
        }

        public Type DefaultInterface => m_defaultInterface;
    }

    // WindowsRuntimeImport is a pseudo custom attribute which causes us to emit the tdWindowsRuntime bit
    // onto types which are decorated with the attribute.  This is needed to mark Windows Runtime types
    // which are redefined in mscorlib.dll and System.Runtime.WindowsRuntime.dll, as the C# compiler does
    // not have a built in syntax to mark tdWindowsRuntime.   These two assemblies are special as they
    // implement the CLR's support for WinRT, so this type is internal as marking tdWindowsRuntime should
    // generally be done via winmdexp for user code.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class WindowsRuntimeImportAttribute : Attribute
    {
        internal WindowsRuntimeImportAttribute() { }
    }

    // This attribute is applied to class interfaces in a generated projection assembly.  It is used by Visual Studio
    // and other tools to find out what version of a component (eg. Windows) a WinRT class began to implement
    // a particular interfaces.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class InterfaceImplementedInVersionAttribute : Attribute
    {
        public InterfaceImplementedInVersionAttribute(Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion)
        {
            m_interfaceType = interfaceType;
            m_majorVersion = majorVersion;
            m_minorVersion = minorVersion;
            m_buildVersion = buildVersion;
            m_revisionVersion = revisionVersion;
        }

        public Type InterfaceType => m_interfaceType;

        public byte MajorVersion => m_majorVersion;

        public byte MinorVersion => m_minorVersion;

        public byte BuildVersion => m_buildVersion;

        public byte RevisionVersion => m_revisionVersion;

        private Type m_interfaceType;
        private byte m_majorVersion;
        private byte m_minorVersion;
        private byte m_buildVersion;
        private byte m_revisionVersion;
    }

    // Applies to read-only array parameters
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ReadOnlyArrayAttribute : Attribute
    {
        public ReadOnlyArrayAttribute() { }
    }

    // Applies to write-only array parameters
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class WriteOnlyArrayAttribute : Attribute
    {
        public WriteOnlyArrayAttribute() { }
    }

    // This attribute is applied on the return value to specify the name of the return value. 
    // In WindowsRuntime all parameters including return value need to have unique names.
    // This is essential in JS as one of the ways to get at the results of a method in JavaScript is via a Dictionary object keyed by parameter name.
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnValueNameAttribute : Attribute
    {
        private string m_Name;

        public ReturnValueNameAttribute(string name)
        {
            m_Name = name;
        }

        public string Name => m_Name;
    }
}
