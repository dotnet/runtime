// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// All RoTypes that return true for IsConstructedGenericType.
    /// </summary>
    internal sealed partial class RoConstructedGenericType : RoInstantiationProviderType
    {
        private readonly RoDefinitionType _genericTypeDefinition;
        private readonly RoType[] _genericTypeArguments;

        internal RoConstructedGenericType(RoDefinitionType genericTypeDefinition, RoType[] genericTypeArguments)
            : base()
        {
            Debug.Assert(genericTypeDefinition != null);
            Debug.Assert(genericTypeArguments != null);

            _genericTypeDefinition = genericTypeDefinition;
            _genericTypeArguments = genericTypeArguments;
        }

        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => false;
        protected sealed override bool IsArrayImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        protected sealed override bool IsByRefImpl() => false;
        protected sealed override bool IsPointerImpl() => false;
        public sealed override bool IsConstructedGenericType => true;
        public sealed override bool IsGenericParameter => false;
        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => false;

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                foreach (RoType typeArgument in _genericTypeArguments)
                {
                    if (typeArgument.ContainsGenericParameters)
                        return true;
                }
                return false;
            }
        }

        internal sealed override RoModule GetRoModule() => _genericTypeDefinition.GetRoModule();

        protected sealed override string ComputeName() => _genericTypeDefinition.Name;
        protected sealed override string ComputeNamespace() => _genericTypeDefinition.Namespace;

        protected sealed override string ComputeFullName()
        {
            if (ContainsGenericParameters)
                return null;

            StringBuilder fullName = new StringBuilder();
            fullName.Append(_genericTypeDefinition.FullName);
            fullName.Append('[');

            for (int i = 0; i < _genericTypeArguments.Length; i++)
            {
                if (i != 0)
                    fullName.Append(',');

                fullName.Append('[');
                fullName.Append(_genericTypeArguments[i].AssemblyQualifiedName);
                fullName.Append(']');
            }
            fullName.Append(']');
            return fullName.ToString();
        }

        public sealed override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_genericTypeDefinition.ToString());
            sb.Append('[');
            for (int i = 0; i < _genericTypeArguments.Length; i++)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(_genericTypeArguments[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }

        public sealed override MethodBase DeclaringMethod => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        protected sealed override RoType ComputeDeclaringType() => _genericTypeDefinition.GetRoDeclaringType();

        protected sealed override RoType ComputeBaseTypeWithoutDesktopQuirk() => _genericTypeDefinition.SpecializeBaseType(Instantiation);
        protected sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => _genericTypeDefinition.SpecializeInterfaces(Instantiation);

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => _genericTypeDefinition.CustomAttributes;
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _genericTypeDefinition.IsCustomAttributeDefined(ns, name);
        internal sealed override CustomAttributeData TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _genericTypeDefinition.TryFindCustomAttribute(ns, name);

        protected sealed override TypeAttributes ComputeAttributeFlags() => _genericTypeDefinition.Attributes;
        protected sealed override TypeCode GetTypeCodeImpl() => Type.GetTypeCode(_genericTypeDefinition);

        public sealed override int MetadataToken => _genericTypeDefinition.MetadataToken;

        internal sealed override RoType GetRoElementType() => null;

        public sealed override Type GetGenericTypeDefinition() => _genericTypeDefinition;
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => _genericTypeArguments;
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => _genericTypeArguments;
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

        public sealed override Guid GUID => _genericTypeDefinition.GUID;
        public sealed override StructLayoutAttribute StructLayoutAttribute => _genericTypeDefinition.StructLayoutAttribute;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => _genericTypeDefinition.ComputeEnumUnderlyingType(); // Easy to forget that generic enums do exist!

        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);

        public sealed override GenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override int GenericParameterPosition => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override Type[] GetGenericParameterConstraints() => throw new InvalidOperationException(SR.Arg_NotGenericParameter);

        // Low level support for the BindingFlag-driven enumerator apis.
        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter filter) => _genericTypeDefinition.SpecializeConstructors(filter, this);
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter filter, Type reflectedType) => _genericTypeDefinition.SpecializeMethods(filter, reflectedType, this);
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter filter, Type reflectedType) => _genericTypeDefinition.SpecializeEvents(filter, reflectedType, this);
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter filter, Type reflectedType) => _genericTypeDefinition.SpecializeFields(filter, reflectedType, this);
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter filter, Type reflectedType) => _genericTypeDefinition.SpecializeProperties(filter, reflectedType, this);
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter filter) => _genericTypeDefinition.GetNestedTypesCore(filter);

        internal sealed override RoType[] Instantiation => _genericTypeArguments;
    }
}
