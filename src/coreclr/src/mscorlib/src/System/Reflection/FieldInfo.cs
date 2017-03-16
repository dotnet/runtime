// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    [Serializable]
    public abstract partial class FieldInfo : MemberInfo
    {
        #region Constructor
        protected FieldInfo() { }
        #endregion

        public static bool operator ==(FieldInfo left, FieldInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeFieldInfo || right is RuntimeFieldInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(FieldInfo left, FieldInfo right)
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
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Field; } }
        #endregion

        #region Public Abstract\Virtual Members

        public virtual Type[] GetRequiredCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public virtual Type[] GetOptionalCustomModifiers()
        {
            throw new NotImplementedException();
        }

        [CLSCompliant(false)]
        public virtual void SetValueDirect(TypedReference obj, Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        [CLSCompliant(false)]
        public virtual Object GetValueDirect(TypedReference obj)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        public abstract RuntimeFieldHandle FieldHandle { get; }

        public abstract Type FieldType { get; }

        public abstract Object GetValue(Object obj);

        public virtual Object GetRawConstantValue() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS")); }

        public abstract void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);

        public abstract FieldAttributes Attributes { get; }
        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public void SetValue(Object obj, Object value)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
        }

        public bool IsPublic { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public; } }

        public bool IsPrivate { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private; } }

        public bool IsFamily { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family; } }

        public bool IsAssembly { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly; } }

        public bool IsFamilyAndAssembly { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem; } }

        public bool IsFamilyOrAssembly { get { return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem; } }

        public bool IsStatic { get { return (Attributes & FieldAttributes.Static) != 0; } }

        public bool IsInitOnly { get { return (Attributes & FieldAttributes.InitOnly) != 0; } }

        public bool IsLiteral { get { return (Attributes & FieldAttributes.Literal) != 0; } }

        public bool IsNotSerialized { get { return (Attributes & FieldAttributes.NotSerialized) != 0; } }

        public bool IsSpecialName { get { return (Attributes & FieldAttributes.SpecialName) != 0; } }

        public bool IsPinvokeImpl { get { return (Attributes & FieldAttributes.PinvokeImpl) != 0; } }

        public virtual bool IsSecurityCritical
        {
            get { return FieldHandle.IsSecurityCritical(); }
        }

        public virtual bool IsSecuritySafeCritical
        {
            get { return FieldHandle.IsSecuritySafeCritical(); }
        }

        public virtual bool IsSecurityTransparent
        {
            get { return FieldHandle.IsSecurityTransparent(); }
        }

        #endregion
    }
}
