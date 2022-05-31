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
        private const string CallingConventionTypePrefix = "System.Runtime.CompilerServices.CallConv";

        private readonly EcmaModule _module;
        private readonly MethodSignature<RoType> _signature;
        private readonly RoFunctionPointerParameterInfo _returnInfo;
        private readonly RoFunctionPointerParameterInfo[] _parameterInfos;

        private volatile string? _toString;
        private volatile Type[]? _lazyCallingConventions;
        private bool? _lazyIsUnmanaged;

        private Type[] CallingConventions => _lazyCallingConventions ??= ComputeCallingConventions();
        private string GetToString() => _toString ??= ComputeToString();

        internal RoFunctionPointerType(EcmaModule module, MethodSignature<RoType> signature)
        {
            Debug.Assert(module != null);

            _module = module;
            _signature = signature;
            _returnInfo = new RoFunctionPointerParameterInfo(signature.ReturnType);

            ImmutableArray<RoType> parameters = signature.ParameterTypes;
            RoFunctionPointerParameterInfo[] parameterInfos = new RoFunctionPointerParameterInfo[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterInfos[i] = new RoFunctionPointerParameterInfo(parameters[i]);
            }
            _parameterInfos = parameterInfos;
        }

        public sealed override bool IsFunctionPointer => true;
        public sealed override bool IsUnmanagedFunctionPointer => _lazyIsUnmanaged ??= ComputeIsUnmanaged();
        public sealed override Type[] GetFunctionPointerCallingConventions() => CallingConventions;
        public sealed override FunctionPointerParameterInfo GetFunctionPointerReturnParameter() => _returnInfo;
        public sealed override FunctionPointerParameterInfo[] GetFunctionPointerParameterInfos() => _parameterInfos;

        private bool ComputeIsUnmanaged()
        {
            var _ = CallingConventions;
            Debug.Assert(_lazyIsUnmanaged.HasValue);
            return _lazyIsUnmanaged.Value!;
        }

        private Type[] ComputeCallingConventions()
        {
            SignatureCallingConvention sigCallingConvention = _signature.Header.CallingConvention;
            Type[] customModifiers = _returnInfo.GetOptionalCustomModifiers();

            bool unmanaged = false;
            List<Type>? list = null;
            bool foundCallingConvention = false;

            for (int i = 0; i < customModifiers.Length; i++)
            {
                Type mod = customModifiers[i];
                string name = mod.FullName!;

                if (name.StartsWith(CallingConventionTypePrefix))
                {
                    list ??= new List<Type>();
                    list.Add(mod);

                    // Only consider the base calling convention, not other CallConv* Types.
                    if (name == CallingConventionTypePrefix + sigCallingConvention.ToString())
                    {
                        foundCallingConvention = true;
                        unmanaged = true;
                    }
                }
            }

            // Normalize the calling conventions.
            if (!foundCallingConvention)
            {
                RoType? callConv = null;

                switch (sigCallingConvention)
                {
                    case SignatureCallingConvention.CDecl:
                        callConv = Loader.GetCoreType(CoreType.CallConvCdecl);
                        unmanaged = true;
                        break;
                    case SignatureCallingConvention.FastCall:
                        callConv = Loader.GetCoreType(CoreType.CallConvFastcall);
                        unmanaged = true;
                        break;
                    case SignatureCallingConvention.StdCall:
                        callConv = Loader.GetCoreType(CoreType.CallConvStdcall);
                        unmanaged = true;
                        break;
                    case SignatureCallingConvention.ThisCall:
                        callConv = Loader.GetCoreType(CoreType.CallConvThiscall);
                        unmanaged = true;
                        break;
                    case SignatureCallingConvention.Unmanaged:
                        // There is no CallConvUnmanaged type.
                        unmanaged = true;
                        break;
                }

                if (callConv != null)
                {
                    list ??= new List<Type>();
                    list.Add(callConv);
                    _returnInfo.GetOptionalCustomModifiersList().Add(callConv);
                }
            }

            _lazyIsUnmanaged = unmanaged;

            return list == null ?  Type.EmptyTypes : list.ToArray();
        }

        protected sealed override string? ComputeFullName() => null;
        protected sealed override string ComputeName() => "*()";

        private string ComputeToString()
        {
            Type t = _returnInfo.ParameterType;
            StringBuilder sb = new(t.ToString());
            sb.Append('(');
            AppendParameters(sb);
            sb.Append(')');
            return sb.ToString();
        }

        private void AppendParameters(StringBuilder sb)
        {
            for (int i = 0; i < _parameterInfos.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(_parameterInfos[i].ParameterType.ToString());
            }
        }

        protected sealed override string? ComputeNamespace() => typeof(Type).Namespace;
        public sealed override string ToString() => GetToString();
        public sealed override string? AssemblyQualifiedName => null;
        internal MethodSignature<RoType> Signature => _signature;

        internal sealed override RoModule GetRoModule() => _module;
        internal EcmaModule GetEcmaModule() => _module;

        public sealed override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public sealed override bool Equals([NotNullWhen(true)] Type? type) => Equals((object?)type);

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is RoFunctionPointerType other))
                return false;

            if (GetFunctionPointerReturnParameter().ParameterType != other.GetFunctionPointerReturnParameter().ParameterType)
                return false;

            FunctionPointerParameterInfo[] args = GetFunctionPointerParameterInfos();
            FunctionPointerParameterInfo[] otherArgs = other.GetFunctionPointerParameterInfos();
            if (!args.Length.Equals(otherArgs.Length))
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ParameterType != otherArgs[i].ParameterType)
                    return false;
            }

            if (!CallingConventions.Length.Equals(other.CallingConventions.Length))
                return false;

            for (int i = 0; i < CallingConventions.Length; i++)
            {
                if (!CallingConventions[i].Equals(other.CallingConventions[i]))
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
