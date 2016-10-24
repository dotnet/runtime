// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public sealed class AutomationProxyAttribute : Attribute
    {
        public AutomationProxyAttribute(bool val)
        {
            Value = val;
        }
        public bool Value { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class ComAliasNameAttribute : Attribute
    {
        public ComAliasNameAttribute(string alias)
        {
            Value = alias;
        }
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
    [Obsolete("This attribute is deprecated and will be removed in a future version.", false)]
    public sealed class IDispatchImplAttribute : Attribute
    {
        public IDispatchImplAttribute(short implType)
        {
            Value = (IDispatchImplType)implType;
        }
        public IDispatchImplAttribute(IDispatchImplType implType)
        {
            Value = implType;
        }
        public IDispatchImplType Value { get; }
    }

    [Obsolete("The IDispatchImplAttribute is deprecated.", false)]
    [Serializable]
    public enum IDispatchImplType
    {
        CompatibleImpl = 2,
        InternalImpl = 1,
        SystemDefinedImpl = 0,
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class LCIDConversionAttribute : Attribute
    {
        public LCIDConversionAttribute(int lcid)
        {
            Value = lcid;
        }
        public int Value { get; }
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

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ProgIdAttribute : Attribute
    {
        public ProgIdAttribute(string progId)
        {
            Value = progId;
        }
        public string Value { get; }
    }

    [Obsolete("This attribute has been deprecated.  Application Domains no longer respect Activation Context boundaries in IDispatch calls.", false)]
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class SetWin32ContextInIDispatchAttribute : Attribute
    {
        public SetWin32ContextInIDispatchAttribute()
        {
        }
    }
}
