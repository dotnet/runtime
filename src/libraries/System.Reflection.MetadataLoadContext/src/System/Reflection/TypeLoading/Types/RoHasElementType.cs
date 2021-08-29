// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base type for all RoTypes that return true for HasElementType.
    /// </summary>
    internal abstract partial class RoHasElementType : RoType
    {
        private readonly RoType _elementType;

        protected RoHasElementType(RoType elementType)
            : base()
        {
            Debug.Assert(elementType != null);

            _elementType = elementType;
        }

        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => true;
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => false;
        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => false;
        public sealed override bool ContainsGenericParameters => _elementType.ContainsGenericParameters;

        internal sealed override RoModule GetRoModule() => _elementType.GetRoModule();

        protected sealed override string ComputeName() => _elementType.Name + Suffix;
        protected sealed override string? ComputeNamespace() => _elementType.Namespace;
        protected sealed override string? ComputeFullName()
        {
            string? fullName = _elementType.FullName;
            return fullName == null ? null : fullName + Suffix;
        }

        protected sealed override TypeCode GetTypeCodeImpl() => TypeCode.Object;

        public sealed override string ToString() => _elementType.ToString() + Suffix;

        public sealed override MethodBase DeclaringMethod => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        protected sealed override RoType? ComputeDeclaringType() => null;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => false;
        internal sealed override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => null;

        public sealed override int MetadataToken => 0x02000000; // nil TypeDef token

        internal sealed override RoType GetRoElementType() => _elementType;

        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => _elementType.GetGenericArgumentsNoCopy();
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

        public sealed override GenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override int GenericParameterPosition => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override Type[] GetGenericParameterConstraints() => throw new InvalidOperationException(SR.Arg_NotGenericParameter);

        public sealed override Guid GUID => Guid.Empty;
        public sealed override StructLayoutAttribute? StructLayoutAttribute => null;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => throw new ArgumentException(SR.Arg_MustBeEnum);

        protected abstract string Suffix { get; }

        // Low level support for the BindingFlag-driven enumerator apis.
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => Array.Empty<EventInfo>();
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => Array.Empty<FieldInfo>();
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => Array.Empty<PropertyInfo>();
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter? filter) => Array.Empty<RoType>();
    }
}
