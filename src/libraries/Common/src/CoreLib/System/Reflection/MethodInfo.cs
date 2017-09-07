// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    public abstract partial class MethodInfo : MethodBase
    {
        protected MethodInfo() { }

        public override MemberTypes MemberType => MemberTypes.Method;

        public virtual ParameterInfo ReturnParameter { get { throw NotImplemented.ByDesign; } }
        public virtual Type ReturnType { get { throw NotImplemented.ByDesign; } }

        public override Type[] GetGenericArguments() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual MethodInfo GetGenericMethodDefinition() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public abstract MethodInfo GetBaseDefinition();

        public abstract ICustomAttributeProvider ReturnTypeCustomAttributes { get; }

        public virtual Delegate CreateDelegate(Type delegateType) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual Delegate CreateDelegate(Type delegateType, object target) { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(MethodInfo left, MethodInfo right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(MethodInfo left, MethodInfo right) => !(left == right);
    }
}
