// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    [Serializable]
    public abstract partial class MethodBase : MemberInfo
    {
        #region Constructor
        protected MethodBase() { }
        #endregion

        public static bool operator ==(MethodBase left, MethodBase right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            MethodInfo method1, method2;
            ConstructorInfo constructor1, constructor2;

            if ((method1 = left as MethodInfo) != null && (method2 = right as MethodInfo) != null)
                return method1 == method2;
            else if ((constructor1 = left as ConstructorInfo) != null && (constructor2 = right as ConstructorInfo) != null)
                return constructor1 == constructor2;

            return false;
        }

        public static bool operator !=(MethodBase left, MethodBase right)
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

        #region Public Abstract\Virtual Members
        [System.Diagnostics.Contracts.Pure]
        public abstract ParameterInfo[] GetParameters();

        public virtual MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return GetMethodImplementationFlags();
            }
        }

        public abstract MethodImplAttributes GetMethodImplementationFlags();

        public abstract RuntimeMethodHandle MethodHandle { get; }

        public abstract MethodAttributes Attributes { get; }

        public abstract Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);

        public virtual CallingConventions CallingConvention { get { return CallingConventions.Standard; } }

        public virtual Type[] GetGenericArguments() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }

        public virtual bool IsGenericMethodDefinition { get { return false; } }

        public virtual bool ContainsGenericParameters { get { return false; } }

        public virtual bool IsGenericMethod { get { return false; } }

        public virtual bool IsSecurityCritical { get { throw new NotImplementedException(); } }

        public virtual bool IsSecuritySafeCritical { get { throw new NotImplementedException(); } }

        public virtual bool IsSecurityTransparent { get { throw new NotImplementedException(); } }

        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object Invoke(Object obj, Object[] parameters)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            return Invoke(obj, BindingFlags.Default, null, parameters, null);
        }

        public bool IsPublic { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; } }

        public bool IsPrivate { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; } }

        public bool IsFamily { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; } }

        public bool IsAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; } }

        public bool IsFamilyAndAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; } }

        public bool IsFamilyOrAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; } }

        public bool IsStatic { get { return (Attributes & MethodAttributes.Static) != 0; } }

        public bool IsFinal
        {
            get { return (Attributes & MethodAttributes.Final) != 0; }
        }
        public bool IsVirtual
        {
            get { return (Attributes & MethodAttributes.Virtual) != 0; }
        }
        public bool IsHideBySig { get { return (Attributes & MethodAttributes.HideBySig) != 0; } }

        public bool IsAbstract { get { return (Attributes & MethodAttributes.Abstract) != 0; } }

        public bool IsSpecialName { get { return (Attributes & MethodAttributes.SpecialName) != 0; } }

        public bool IsConstructor
        {
            get
            {
                // To be backward compatible we only return true for instance RTSpecialName ctors.
                return (this is ConstructorInfo &&
                        !IsStatic &&
                        ((Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName));
            }
        }

        public virtual MethodBody GetMethodBody()
        {
            throw new InvalidOperationException();
        }
        #endregion
    }
}
