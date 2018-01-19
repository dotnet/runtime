// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    public abstract class PropertyInfo : MemberInfo
    {
        protected PropertyInfo() { }

        public override MemberTypes MemberType => MemberTypes.Property;

        public abstract Type PropertyType { get; }
        public abstract ParameterInfo[] GetIndexParameters();

        public abstract PropertyAttributes Attributes { get; }
        public bool IsSpecialName => (Attributes & PropertyAttributes.SpecialName) != 0;

        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }

        public MethodInfo[] GetAccessors() => GetAccessors(nonPublic: false);
        public abstract MethodInfo[] GetAccessors(bool nonPublic);

        public virtual MethodInfo GetMethod => GetGetMethod(nonPublic: true);
        public MethodInfo GetGetMethod() => GetGetMethod(nonPublic: false);
        public abstract MethodInfo GetGetMethod(bool nonPublic);

        public virtual MethodInfo SetMethod => GetSetMethod(nonPublic: true);
        public MethodInfo GetSetMethod() => GetSetMethod(nonPublic: false);
        public abstract MethodInfo GetSetMethod(bool nonPublic);

        public virtual Type[] GetOptionalCustomModifiers() => Array.Empty<Type>();
        public virtual Type[] GetRequiredCustomModifiers() => Array.Empty<Type>();

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object GetValue(object obj) => GetValue(obj, index: null);
        [DebuggerHidden]
        [DebuggerStepThrough]
        public virtual object GetValue(object obj, object[] index) => GetValue(obj, BindingFlags.Default, binder: null, index: index, culture: null);
        public abstract object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture);

        public virtual object GetConstantValue() { throw NotImplemented.ByDesign; }
        public virtual object GetRawConstantValue() { throw NotImplemented.ByDesign; }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public void SetValue(object obj, object value) => SetValue(obj, value, index: null);
        [DebuggerHidden]
        [DebuggerStepThrough]
        public virtual void SetValue(object obj, object value, object[] index) => SetValue(obj, value, BindingFlags.Default, binder: null, index: index, culture: null);
        public abstract void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture);

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(PropertyInfo left, PropertyInfo right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(PropertyInfo left, PropertyInfo right) => !(left == right);
    }
}
