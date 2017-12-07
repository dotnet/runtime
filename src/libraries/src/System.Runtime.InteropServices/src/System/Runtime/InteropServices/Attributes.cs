// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public sealed class AutomationProxyAttribute : Attribute
    {
        public AutomationProxyAttribute(bool val) => Value = val;

        public bool Value { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class ComAliasNameAttribute : Attribute
    {
        public ComAliasNameAttribute(string alias) => Value = alias;

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class ComCompatibleVersionAttribute : Attribute
    {
        public ComCompatibleVersionAttribute(int major, int minor, int build, int revision)
        {
            MajorVersion = major;
            MinorVersion = minor;
            BuildNumber = build;
            RevisionNumber = revision;
        }

        public int MajorVersion { get; }
        public int MinorVersion { get; }
        public int BuildNumber { get; }
        public int RevisionNumber { get; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ComConversionLossAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ComRegisterFunctionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ComUnregisterFunctionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false)]
    [Obsolete("This attribute is deprecated and will be removed in a future version.", error: false)]
    public sealed class IDispatchImplAttribute : Attribute
    {
        public IDispatchImplAttribute(short implType) : this((IDispatchImplType)implType)
        {
        }

        public IDispatchImplAttribute(IDispatchImplType implType) => Value = implType;

        public IDispatchImplType Value { get; }
    }

    [Obsolete("The IDispatchImplAttribute is deprecated.", error: false)]
    public enum IDispatchImplType
    {
        CompatibleImpl = 2,
        InternalImpl = 1,
        SystemDefinedImpl = 0,
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class ImportedFromTypeLibAttribute : Attribute
    {
        public ImportedFromTypeLibAttribute(string tlbFile) => Value = tlbFile;

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ManagedToNativeComInteropStubAttribute : Attribute
    {
        public ManagedToNativeComInteropStubAttribute(Type classType, string methodName)
        {
            ClassType = classType;
            MethodName = methodName;
        }

        public Type ClassType { get; }
        public string MethodName { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class PrimaryInteropAssemblyAttribute : Attribute
    {
        public PrimaryInteropAssemblyAttribute(int major, int minor)
        {
            MajorVersion = major;
            MinorVersion = minor;
        }

        public int MajorVersion { get; }
        public int MinorVersion { get; }
    }

    [Obsolete("This attribute has been deprecated.  Application Domains no longer respect Activation Context boundaries in IDispatch calls.", error: false)]
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class SetWin32ContextInIDispatchAttribute : Attribute
    {
        public SetWin32ContextInIDispatchAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class TypeLibImportClassAttribute : Attribute
    {
        public TypeLibImportClassAttribute(Type importClass) => Value = importClass.ToString();

        public string Value { get; }
    }

    [Flags]
    public enum TypeLibTypeFlags
    {
        FAppObject      = 0x0001,
        FCanCreate      = 0x0002,
        FLicensed       = 0x0004,
        FPreDeclId      = 0x0008,
        FHidden         = 0x0010,
        FControl        = 0x0020,
        FDual           = 0x0040,
        FNonExtensible  = 0x0080,
        FOleAutomation  = 0x0100,
        FRestricted     = 0x0200,
        FAggregatable   = 0x0400,
        FReplaceable    = 0x0800,
        FDispatchable   = 0x1000,
        FReverseBind    = 0x2000,
    }

    [Flags]
    public enum TypeLibFuncFlags
    {
        FRestricted         = 0x0001,
        FSource             = 0x0002,
        FBindable           = 0x0004,
        FRequestEdit        = 0x0008,
        FDisplayBind        = 0x0010,
        FDefaultBind        = 0x0020,
        FHidden             = 0x0040,
        FUsesGetLastError   = 0x0080,
        FDefaultCollelem    = 0x0100,
        FUiDefault          = 0x0200,
        FNonBrowsable       = 0x0400,
        FReplaceable        = 0x0800,
        FImmediateBind      = 0x1000,
    }

    [Flags]
    public enum TypeLibVarFlags
    {
        FReadOnly           = 0x0001,
        FSource             = 0x0002,
        FBindable           = 0x0004,
        FRequestEdit        = 0x0008,
        FDisplayBind        = 0x0010,
        FDefaultBind        = 0x0020,
        FHidden             = 0x0040,
        FRestricted         = 0x0080,
        FDefaultCollelem    = 0x0100,
        FUiDefault          = 0x0200,
        FNonBrowsable       = 0x0400,
        FReplaceable        = 0x0800,
        FImmediateBind      = 0x1000,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct, Inherited = false)]
    public sealed class TypeLibTypeAttribute : Attribute
    {
        public TypeLibTypeAttribute(TypeLibTypeFlags flags)
        {
            Value = flags;
        }

        public TypeLibTypeAttribute(short flags)
        {
            Value = (TypeLibTypeFlags)flags;
        }

        public TypeLibTypeFlags Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class TypeLibFuncAttribute : Attribute
    {
        public TypeLibFuncAttribute(TypeLibFuncFlags flags)
        {
            Value = flags;
        }

        public TypeLibFuncAttribute(short flags)
        {
            Value = (TypeLibFuncFlags)flags;
        }

        public TypeLibFuncFlags Value { get; }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class TypeLibVarAttribute : Attribute
    {
        public TypeLibVarAttribute(TypeLibVarFlags flags)
        {
            Value = flags;
        }

        public TypeLibVarAttribute(short flags)
        {
            Value = (TypeLibVarFlags)flags;
        }

        public TypeLibVarFlags Value { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class TypeLibVersionAttribute : Attribute
    {
        public TypeLibVersionAttribute(int major, int minor)
        {
            MajorVersion = major;
            MinorVersion = minor;
        }

        public int MajorVersion { get; }
        public int MinorVersion { get; }
    }
}
