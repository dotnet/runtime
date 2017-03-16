// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    [Serializable]
    public abstract class MethodInfo : MethodBase
    {
        #region Constructor
        protected MethodInfo() { }
        #endregion

        public static bool operator ==(MethodInfo left, MethodInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeMethodInfo || right is RuntimeMethodInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(MethodInfo left, MethodInfo right)
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
        public override MemberTypes MemberType { get { return System.Reflection.MemberTypes.Method; } }
        #endregion

        #region Public Abstract\Virtual Members
        public virtual Type ReturnType { get { throw new NotImplementedException(); } }

        public virtual ParameterInfo ReturnParameter { get { throw new NotImplementedException(); } }

        public abstract ICustomAttributeProvider ReturnTypeCustomAttributes { get; }

        public abstract MethodInfo GetBaseDefinition();

        public override Type[] GetGenericArguments() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }

        public virtual MethodInfo GetGenericMethodDefinition() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }

        public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments) { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }

        public virtual Delegate CreateDelegate(Type delegateType) { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }
        public virtual Delegate CreateDelegate(Type delegateType, Object target) { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }
        #endregion
    }
}
