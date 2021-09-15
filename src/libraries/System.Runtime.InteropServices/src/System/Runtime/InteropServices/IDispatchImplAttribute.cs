// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false)]
    [Obsolete("IDispatchImplAttribute has been deprecated and is not supported.")]
    public sealed class IDispatchImplAttribute : Attribute
    {
        public IDispatchImplAttribute(short implType) : this((IDispatchImplType)implType)
        {
        }

        public IDispatchImplAttribute(IDispatchImplType implType) => Value = implType;

        public IDispatchImplType Value { get; }
    }
}
