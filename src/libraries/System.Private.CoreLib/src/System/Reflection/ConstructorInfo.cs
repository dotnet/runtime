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
        public object Invoke(object[] parameters) => Invoke(BindingFlags.Default, binder: null, parameters: parameters, culture: null);
        public abstract object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture);

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        // Force inline as the true/false ternary takes it above ALWAYS_INLINE size even though the asm ends up smaller
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ConstructorInfo left, ConstructorInfo right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            // Quick reference equality test prior to calling the virtual Equality
            return ReferenceEquals(right, left) ? true : right.Equals(left);
        }

        public static bool operator !=(ConstructorInfo left, ConstructorInfo right) => !(left == right);

        public static readonly string ConstructorName = ".ctor";
        public static readonly string TypeConstructorName = ".cctor";
    }
}
