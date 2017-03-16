// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    [Serializable]
    public abstract class PropertyInfo : MemberInfo
    {
        #region Constructor
        protected PropertyInfo() { }
        #endregion

        public static bool operator ==(PropertyInfo left, PropertyInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimePropertyInfo || right is RuntimePropertyInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(PropertyInfo left, PropertyInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Property; } }
        #endregion

        #region Public Abstract\Virtual Members
        public virtual object GetConstantValue()
        {
            throw new NotImplementedException();
        }

        public virtual object GetRawConstantValue()
        {
            throw new NotImplementedException();
        }

        public abstract Type PropertyType { get; }

        public abstract void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);

        public abstract MethodInfo[] GetAccessors(bool nonPublic);

        public abstract MethodInfo GetGetMethod(bool nonPublic);

        public abstract MethodInfo GetSetMethod(bool nonPublic);

        public abstract ParameterInfo[] GetIndexParameters();

        public abstract PropertyAttributes Attributes { get; }

        public abstract bool CanRead { get; }

        public abstract bool CanWrite { get; }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object GetValue(Object obj)
        {
            return GetValue(obj, null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual Object GetValue(Object obj, Object[] index)
        {
            return GetValue(obj, BindingFlags.Default, null, index, null);
        }

        public abstract Object GetValue(Object obj, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public void SetValue(Object obj, Object value)
        {
            SetValue(obj, value, null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual void SetValue(Object obj, Object value, Object[] index)
        {
            SetValue(obj, value, BindingFlags.Default, null, index, null);
        }
        #endregion

        #region Public Members
        public virtual Type[] GetRequiredCustomModifiers() { return EmptyArray<Type>.Value; }

        public virtual Type[] GetOptionalCustomModifiers() { return EmptyArray<Type>.Value; }

        public MethodInfo[] GetAccessors() { return GetAccessors(false); }

        public virtual MethodInfo GetMethod
        {
            get
            {
                return GetGetMethod(true);
            }
        }

        public virtual MethodInfo SetMethod
        {
            get
            {
                return GetSetMethod(true);
            }
        }

        public MethodInfo GetGetMethod() { return GetGetMethod(false); }

        public MethodInfo GetSetMethod() { return GetSetMethod(false); }

        public bool IsSpecialName { get { return (Attributes & PropertyAttributes.SpecialName) != 0; } }
        #endregion
    }
}
