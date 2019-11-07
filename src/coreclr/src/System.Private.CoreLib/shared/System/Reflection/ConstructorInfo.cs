// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if ((object?)left == (object)right)
            {
                return true;
            }

            return (left is null) ? false : left.Equals(right);
        }

        public static bool operator !=(ConstructorInfo? left, ConstructorInfo? right) => !(left == right);

        public static readonly string ConstructorName = ".ctor";
        public static readonly string TypeConstructorName = ".cctor";
    }
}
