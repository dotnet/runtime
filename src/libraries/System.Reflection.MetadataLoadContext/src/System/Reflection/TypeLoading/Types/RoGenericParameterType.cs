// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base type for all RoTypes that return true for IsGenericParameter. This can a generic parameter defined on a type or a method.
    /// </summary>
    internal abstract partial class RoGenericParameterType : RoType
    {
        protected RoGenericParameterType()
            : base()
        {
        }

        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => false;
        protected sealed override bool IsArrayImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        protected sealed override bool IsByRefImpl() => false;
        protected sealed override bool IsPointerImpl() => false;
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => true;
        public sealed override bool ContainsGenericParameters => true;

        protected sealed override string ComputeNamespace() => DeclaringType.Namespace;
        protected sealed override string ComputeFullName() => null;
        public sealed override string ToString() => Loader.GetDisposedString() ?? Name;

        protected sealed override TypeAttributes ComputeAttributeFlags() => TypeAttributes.Public;
        protected sealed override TypeCode GetTypeCodeImpl() => TypeCode.Object;

        internal sealed override RoType GetRoElementType() => null;
        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);

        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => Array.Empty<RoType>();
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

        public sealed override int GenericParameterPosition => (_lazyPosition == -1) ? (_lazyPosition = ComputePosition()) : _lazyPosition;
        protected abstract int ComputePosition();
        private volatile int _lazyPosition = -1;

        public sealed override Type[] GetGenericParameterConstraints() => GetGenericParameterConstraintsNoCopy().CloneArray<Type>();
        private RoType[] GetGenericParameterConstraintsNoCopy() => _lazyConstraints ?? (_lazyConstraints = ComputeGenericParameterConstraints());
        protected abstract RoType[] ComputeGenericParameterConstraints();
        private volatile RoType[] _lazyConstraints;

        public sealed override Guid GUID => Guid.Empty;
        public sealed override StructLayoutAttribute StructLayoutAttribute => null;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => throw new ArgumentException(SR.Arg_MustBeEnum);

        protected sealed override RoType ComputeBaseTypeWithoutDesktopQuirk()
        {
            RoType[] constraints = GetGenericParameterConstraintsNoCopy();
            foreach (RoType constraint in constraints)
            {
                if (!constraint.IsInterface)
                    return constraint;
            }
            return Loader.GetCoreType(CoreType.Object);
        }

        protected sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces()
        {
            RoType[] constraints = GetGenericParameterConstraintsNoCopy();
            foreach (RoType constraint in constraints)
            {
                if (constraint.IsInterface)
                    yield return constraint;
            }
        }

        // Low level support for the BindingFlag-driven enumerator apis.
        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter filter) => Array.Empty<ConstructorInfo>();
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter filter, Type reflectedType) => Array.Empty<MethodInfo>();
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter filter, Type reflectedType) => Array.Empty<EventInfo>();
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter filter, Type reflectedType) => Array.Empty<FieldInfo>();
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter filter, Type reflectedType) => Array.Empty<PropertyInfo>();
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter filter) => Array.Empty<RoType>();
    }
}
