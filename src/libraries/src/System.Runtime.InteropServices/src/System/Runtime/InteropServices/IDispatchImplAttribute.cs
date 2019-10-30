// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
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
}
