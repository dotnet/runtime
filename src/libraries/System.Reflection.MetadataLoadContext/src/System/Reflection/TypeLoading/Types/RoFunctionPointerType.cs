// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.TypeLoading.Ecma;
using System.Text;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// All RoTypes that return true for IsFunctionPointer.
    /// </summary>
    internal sealed class RoFunctionPointerType : RoType
    {
        private readonly EcmaModule _module;
        private readonly SignatureCallingConvention _callKind;
        private readonly bool _isUnmanaged;
        internal readonly Type _returnType;
        internal readonly Type[] _parameterTypes;

        private volatile string? _toString;

        private string GetToString() => _toString ??= ComputeToString();

        internal RoFunctionPointerType(EcmaModule module, MethodSignature<RoType> signature)
        {
            Debug.Assert(module != null);

            _module = module;
            _returnType = signature.ReturnType;

            ImmutableArray<RoType> sigParameterTypes = signature.ParameterTypes;
            _parameterTypes = new RoType[sigParameterTypes.Length];
            for (int i = 0; i < sigParameterTypes.Length; i++)
            {
                _parameterTypes[i] = sigParameterTypes[i];
            }

            _callKind = GetCallingConvention(signature, out _isUnmanaged);
        }

        public sealed override bool IsFunctionPointer => true;
        public sealed override bool IsUnmanagedFunctionPointer => _isUnmanaged;
        public sealed override Type GetFunctionPointerReturnType() => _returnType.UnderlyingSystemType;
        public sealed override Type[] GetFunctionPointerParameterTypes() => _parameterTypes.CloneArrayToUnmodifiedTypes();
        protected sealed override string? ComputeFullName() => null;
        protected sealed override string ComputeName() => string.Empty;

        internal static SignatureCallingConvention GetCallingConvention(MethodSignature<RoType> signature, out bool isUnmanaged)
        {
            SignatureCallingConvention callKind = signature.Header.CallingConvention;

            isUnmanaged = false;

            switch (callKind)
            {
                case SignatureCallingConvention.CDecl:
                case SignatureCallingConvention.StdCall:
                case SignatureCallingConvention.ThisCall:
                case SignatureCallingConvention.FastCall:
                case SignatureCallingConvention.Unmanaged:
                    isUnmanaged = true;
                    break;
            }

            return callKind;
        }

        internal SignatureCallingConvention CallKind => _callKind;

        private string ComputeToString()
        {
            Type t = _returnType;
            StringBuilder sb = new(t.ToString());
            sb.Append('(');
            AppendParameters(sb);
            sb.Append(')');
            return sb.ToString();

            void AppendParameters(StringBuilder sb)
            {
                for (int i = 0; i < _parameterTypes.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(_parameterTypes[i].ToString());
                }
            }
        }

        protected sealed override string? ComputeNamespace() => null;
        public sealed override string ToString() => GetToString();
        public sealed override string? AssemblyQualifiedName => null;

        internal sealed override RoModule GetRoModule() => _module;
        public sealed override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public sealed override bool Equals([NotNullWhen(true)] Type? type) => Equals((object?)type);
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            // Treat as an unmodified type; do not not include the modified type values including
            // calling conventions and custom modifiers.

            if (obj is not RoFunctionPointerType other)
                return false;

            if (GetFunctionPointerReturnType() != other.GetFunctionPointerReturnType())
                return false;

            if (IsUnmanagedFunctionPointer != other.IsUnmanagedFunctionPointer)
                return false;

            Type[] args = GetFunctionPointerParameterTypes();
            Type[] otherArgs = other.GetFunctionPointerParameterTypes();
            if (!args.Length.Equals(otherArgs.Length))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != otherArgs[i])
                    return false;
            }

            return true;
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
        public sealed override bool IsGenericParameter => false;
        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => false;
        public sealed override bool ContainsGenericParameters => IsGenericTypeDefinition;

        protected sealed override TypeCode GetTypeCodeImpl() => TypeCode.Object;

        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);

        public sealed override MethodBase DeclaringMethod => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        protected sealed override RoType? ComputeDeclaringType() => null;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => false;
        internal sealed override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => null;

        public sealed override int MetadataToken => 0x02000000; // nil TypeDef token

        internal sealed override RoType? GetRoElementType() => null;

        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => Array.Empty<RoType>();
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

        public sealed override GenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override int GenericParameterPosition => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override Type[] GetGenericParameterConstraints() => throw new InvalidOperationException(SR.Arg_NotGenericParameter);

        public sealed override Guid GUID => Guid.Empty;
        public sealed override StructLayoutAttribute? StructLayoutAttribute => null;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => throw new ArgumentException(SR.Arg_MustBeEnum);

        // Low level support for the BindingFlag-driven enumerator apis.
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => Array.Empty<EventInfo>();
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => Array.Empty<FieldInfo>();
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => Array.Empty<PropertyInfo>();
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter? filter) => Array.Empty<RoType>();


        protected sealed override TypeAttributes ComputeAttributeFlags() => TypeAttributes.Public;

        internal sealed override RoType? ComputeBaseTypeWithoutDesktopQuirk() => null;
        internal sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => Array.Empty<RoType>();

        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => Array.Empty<ConstructorInfo>();
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => Array.Empty<MethodInfo>();
    }
}
