// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    // A convenience base class for implementing special-purpose RoTypes such as sentinels. It exists primarily to reduce the number
    // of files that have to be edited whenever RoType adds or removes an abstract method rather than to imply any meaningful commonality.
    internal abstract class RoStubType : RoType
    {
        protected RoStubType() : base() { }

        public sealed override bool IsTypeDefinition => throw null;
        public sealed override bool IsGenericTypeDefinition => throw null;
        protected sealed override bool HasElementTypeImpl() => throw null;
        protected sealed override bool IsArrayImpl() => throw null;
        public sealed override bool IsSZArray => throw null;
        public sealed override bool IsVariableBoundArray => throw null;
        protected sealed override bool IsByRefImpl() => throw null;
        protected sealed override bool IsPointerImpl() => throw null;
        public sealed override bool IsConstructedGenericType => throw null;
        public sealed override bool IsGenericParameter => throw null;
        public sealed override bool IsGenericTypeParameter => throw null;
        public sealed override bool IsGenericMethodParameter => throw null;
        public sealed override bool ContainsGenericParameters => throw null;

        internal sealed override RoModule GetRoModule() => throw null;

        public sealed override int GetArrayRank() => throw null;

        protected sealed override string ComputeName() => throw null;
        protected sealed override string ComputeNamespace() => throw null;
        protected sealed override string ComputeFullName() => throw null;

        protected sealed override TypeAttributes ComputeAttributeFlags() => throw null;
        protected sealed override TypeCode GetTypeCodeImpl() => throw null;

        public sealed override string ToString() => GetType().ToString();

        public sealed override MethodBase DeclaringMethod => throw null;
        protected sealed override RoType ComputeDeclaringType() => throw null;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => throw null;
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => throw null;
        internal sealed override CustomAttributeData TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => throw null;

        public sealed override int MetadataToken => throw null;

        internal sealed override RoType GetRoElementType() => throw null;

        public sealed override Type GetGenericTypeDefinition() => throw null;
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => throw null;
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => throw null;
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => throw null;
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw null;

        public sealed override GenericParameterAttributes GenericParameterAttributes => throw null;
        public sealed override int GenericParameterPosition => throw null;
        public sealed override Type[] GetGenericParameterConstraints() => throw null;

        public sealed override Guid GUID => throw null;
        public sealed override StructLayoutAttribute StructLayoutAttribute => throw null;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => throw null;

        protected sealed override RoType ComputeBaseTypeWithoutDesktopQuirk() => throw null;
        protected sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => throw null;

        // Low level support for the BindingFlag-driven enumerator apis.
        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter filter) => throw null;
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter filter, Type reflectedType) => throw null;
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter filter, Type reflectedType) => throw null;
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter filter, Type reflectedType) => throw null;
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter filter, Type reflectedType) => throw null;
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter filter) => throw null;
    }
}
