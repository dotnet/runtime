// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base type for all RoTypes that return true for IsTypeDefinition.
    /// </summary>
    internal abstract partial class RoDefinitionType : RoInstantiationProviderType
    {
        protected RoDefinitionType()
            : base()
        {
        }

        public sealed override bool IsTypeDefinition => true;
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

        protected sealed override string ComputeFullName()
        {
            Debug.Assert(!IsConstructedGenericType);
            Debug.Assert(!IsGenericParameter);
            Debug.Assert(!HasElementType);

            string name = Name;

            Type? declaringType = DeclaringType;
            if (declaringType != null)
            {
                string? declaringTypeFullName = declaringType.FullName;
                return declaringTypeFullName + "+" + name;
            }

            string? ns = Namespace;
            if (ns == null)
                return name;
            return ns + "." + name;
        }

        public sealed override string ToString() => Loader.GetDisposedString() ?? FullName!;
        internal abstract int GetGenericParameterCount();
        internal abstract override RoType[] GetGenericTypeParametersNoCopy();

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in GetTrueCustomAttributes())
                {
                    yield return cad;
                }

                if (0 != (Attributes & TypeAttributes.Import))
                {
                    ConstructorInfo? ci = Loader.TryGetComImportCtor();
                    if (ci != null)
                        yield return new RoPseudoCustomAttributeData(ci);
                }
            }
        }

        protected abstract IEnumerable<CustomAttributeData> GetTrueCustomAttributes();

        public sealed override Type GetGenericTypeDefinition() => IsGenericTypeDefinition ? this : throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);

        protected sealed override RoType? ComputeBaseTypeWithoutDesktopQuirk() => SpecializeBaseType(Instantiation);
        internal abstract RoType? SpecializeBaseType(RoType[] instantiation);

        protected sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => SpecializeInterfaces(Instantiation);
        internal abstract IEnumerable<RoType> SpecializeInterfaces(RoType[] instantiation);

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override Type MakeGenericType(params Type[] typeArguments)
        {
            if (typeArguments == null)
                throw new ArgumentNullException(nameof(typeArguments));

            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

            int count = typeArguments.Length;
            if (count != GetGenericParameterCount())
                throw new ArgumentException(SR.Argument_GenericArgsCount, nameof(typeArguments));

            bool foundSigType = false;
            RoType[] runtimeTypeArguments = new RoType[count];
            for (int i = 0; i < count; i++)
            {
                Type typeArgument = typeArguments[i];
                if (typeArgument == null)
                    throw new ArgumentNullException();
                if (typeArgument.IsSignatureType())
                {
                    foundSigType = true;
                }
                else
                {
                    if (!(typeArgument is RoType roTypeArgument && roTypeArgument.Loader == Loader))
                        throw new ArgumentException(SR.Format(SR.MakeGenericType_NotLoadedByMetadataLoadContext, typeArgument));
                    runtimeTypeArguments[i] = roTypeArgument;
                }
            }
            if (foundSigType)
                return this.MakeSignatureGenericType(typeArguments);

            // We are intentionally not validating constraints as constraint validation is an execution-time issue that does not block our
            // library and should not block a metadata inspection tool.
            return this.GetUniqueConstructedGenericType(runtimeTypeArguments);
        }

        public sealed override Guid GUID
        {
            get
            {
                CustomAttributeData? cad = TryFindCustomAttribute(Utf8Constants.SystemRuntimeInteropServices, Utf8Constants.GuidAttribute);
                if (cad == null)
                    return default;
                IList<CustomAttributeTypedArgument> ctas = cad.ConstructorArguments;
                if (ctas.Count != 1)
                    return default;
                CustomAttributeTypedArgument cta = ctas[0];
                if (cta.ArgumentType != Loader.TryGetCoreType(CoreType.String))
                    return default;
                if (!(cta.Value is string guidString))
                    return default;
                return new Guid(guidString);
            }
        }

        public sealed override StructLayoutAttribute? StructLayoutAttribute
        {
            get
            {
                const int DefaultPackingSize = 8;

                // Note: CoreClr checks HasElementType and IsGenericParameter in addition to IsInterface but those properties cannot be true here as this
                // RoType subclass is solely for TypeDef types.)
                if (IsInterface)
                    return null;

                TypeAttributes attributes = Attributes;
                LayoutKind layoutKind = (attributes & TypeAttributes.LayoutMask) switch
                {
                    TypeAttributes.ExplicitLayout => LayoutKind.Explicit,
                    TypeAttributes.AutoLayout => LayoutKind.Auto,
                    TypeAttributes.SequentialLayout => LayoutKind.Sequential,
                    _ => LayoutKind.Auto,
                };
                CharSet charSet = (attributes & TypeAttributes.StringFormatMask) switch
                {
                    TypeAttributes.AnsiClass => CharSet.Ansi,
                    TypeAttributes.AutoClass => CharSet.Auto,
                    TypeAttributes.UnicodeClass => CharSet.Unicode,
                    _ => CharSet.None,
                };
                GetPackSizeAndSize(out int pack, out int size);

                // Metadata parameter checking should not have allowed 0 for packing size.
                // The runtime later converts a packing size of 0 to 8 so do the same here
                // because it's more useful from a user perspective.
                if (pack == 0)
                    pack = DefaultPackingSize;

                return new StructLayoutAttribute(layoutKind)
                {
                    CharSet = charSet,
                    Pack = pack,
                    Size = size,
                };
            }
        }

        protected abstract void GetPackSizeAndSize(out int packSize, out int size);

        protected sealed override TypeCode GetTypeCodeImpl()
        {
            Type t = IsEnum ? GetEnumUnderlyingType() : this;
            CoreTypes ct = Loader.GetAllFoundCoreTypes();
            if (t == ct[CoreType.Boolean])
                return TypeCode.Boolean;
            if (t == ct[CoreType.Char])
                return TypeCode.Char;
            if (t == ct[CoreType.SByte])
                return TypeCode.SByte;
            if (t == ct[CoreType.Byte])
                return TypeCode.Byte;
            if (t == ct[CoreType.Int16])
                return TypeCode.Int16;
            if (t == ct[CoreType.UInt16])
                return TypeCode.UInt16;
            if (t == ct[CoreType.Int32])
                return TypeCode.Int32;
            if (t == ct[CoreType.UInt32])
                return TypeCode.UInt32;
            if (t == ct[CoreType.Int64])
                return TypeCode.Int64;
            if (t == ct[CoreType.UInt64])
                return TypeCode.UInt64;
            if (t == ct[CoreType.Single])
                return TypeCode.Single;
            if (t == ct[CoreType.Double])
                return TypeCode.Double;
            if (t == ct[CoreType.String])
                return TypeCode.String;
            if (t == ct[CoreType.DateTime])
                return TypeCode.DateTime;
            if (t == ct[CoreType.Decimal])
                return TypeCode.Decimal;
            if (t == ct[CoreType.DBNull])
                return TypeCode.DBNull;
            return TypeCode.Object;
        }

        internal sealed override RoType? GetRoElementType() => null;
        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => GetGenericTypeParametersNoCopy();
        public sealed override GenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override int GenericParameterPosition => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override Type[] GetGenericParameterConstraints() => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
        public sealed override MethodBase DeclaringMethod => throw new InvalidOperationException(SR.Arg_NotGenericParameter);

        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => SpecializeConstructors(filter, this);
        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => SpecializeMethods(filter, reflectedType, this);
        internal sealed override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => SpecializeEvents(filter, reflectedType, this);
        internal sealed override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => SpecializeFields(filter, reflectedType, this);
        internal sealed override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => SpecializeProperties(filter, reflectedType, this);

        // Like CoreGetDeclared but allows specifying an alternate declaringType (which must be a generic instantiation of the true declaring type)
        internal abstract IEnumerable<ConstructorInfo> SpecializeConstructors(NameFilter? filter, RoInstantiationProviderType declaringType);
        internal abstract IEnumerable<MethodInfo> SpecializeMethods(NameFilter? filter, Type reflectedType, RoInstantiationProviderType declaringType);
        internal abstract IEnumerable<EventInfo> SpecializeEvents(NameFilter? filter, Type reflectedType, RoInstantiationProviderType declaringType);
        internal abstract IEnumerable<FieldInfo> SpecializeFields(NameFilter? filter, Type reflectedType, RoInstantiationProviderType declaringType);
        internal abstract IEnumerable<PropertyInfo> SpecializeProperties(NameFilter? filter, Type reflectedType, RoInstantiationProviderType declaringType);

        // Helpers for the typeref-resolution/name lookup logic.
        internal abstract bool IsTypeNameEqual(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name);
        internal abstract RoDefinitionType? GetNestedTypeCore(ReadOnlySpan<byte> utf8Name);

        internal sealed override RoType[] Instantiation => GetGenericTypeParametersNoCopy();
    }
}
