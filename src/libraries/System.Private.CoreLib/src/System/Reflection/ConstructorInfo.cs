// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public abstract partial class ConstructorInfo : MethodBase
    {
        protected ConstructorInfo() { }

        public override MemberTypes MemberType => MemberTypes.Constructor;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object Invoke(object?[]? parameters) => Invoke(BindingFlags.Default, binder: null, parameters: parameters, culture: null);
        public abstract object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ConstructorInfo? left, ConstructorInfo? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return left is not null && left.Equals(right);
        }

        public static bool operator !=(ConstructorInfo? left, ConstructorInfo? right) => !(left == right);

        public static readonly string ConstructorName = ".ctor";
        public static readonly string TypeConstructorName = ".cctor";
    }
}
