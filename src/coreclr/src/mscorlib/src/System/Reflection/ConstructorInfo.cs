// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    [Serializable]
    public abstract partial class ConstructorInfo : MethodBase
    {
        #region Static Members
        public readonly static String ConstructorName = ".ctor";

        public readonly static String TypeConstructorName = ".cctor";
        #endregion

        #region Constructor
        protected ConstructorInfo() { }
        #endregion

        public static bool operator ==(ConstructorInfo left, ConstructorInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeConstructorInfo || right is RuntimeConstructorInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(ConstructorInfo left, ConstructorInfo right)
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
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Constructor; } }
        #endregion

        #region Public Abstract\Virtual Members
        public abstract Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object Invoke(Object[] parameters)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            return Invoke(BindingFlags.Default, null, parameters, null);
        }
        #endregion
    }
}
