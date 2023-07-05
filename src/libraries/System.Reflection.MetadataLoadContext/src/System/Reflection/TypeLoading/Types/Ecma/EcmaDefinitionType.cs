// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// RoTypes that return true for IsTypeDefinition and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaDefinitionType : RoDefinitionType
    {
        private readonly EcmaModule _module;
        private readonly TypeDefinitionHandle _handle;

        internal EcmaDefinitionType(TypeDefinitionHandle handle, EcmaModule module)
            : base()
        {
            Debug.Assert(module != null);
            Debug.Assert(!handle.IsNil);

            _module = module;
            _handle = handle;
            _neverAccessThisExceptThroughTypeDefinitionProperty = handle.GetTypeDefinition(Reader);
        }

        internal sealed override RoModule GetRoModule() => _module;
        internal EcmaModule GetEcmaModule() => _module;

        protected sealed override RoType? ComputeDeclaringType()
        {
            if (!TypeDefinition.IsNested)
                return null;

            return TypeDefinition.GetDeclaringType().ResolveTypeDef(GetEcmaModule());
        }

        protected sealed override string ComputeName() => TypeDefinition.Name.GetString(Reader).EscapeTypeNameIdentifier();

        protected sealed override string? ComputeNamespace()
        {
            Type? declaringType = DeclaringType;
            if (declaringType != null)
                return declaringType.Namespace;
            return TypeDefinition.Namespace.GetStringOrNull(Reader)?.EscapeTypeNameIdentifier();
        }

        protected sealed override TypeAttributes ComputeAttributeFlags() => TypeDefinition.Attributes;

        internal sealed override RoType? SpecializeBaseType(RoType[] instantiation)
        {
            EntityHandle baseTypeHandle = TypeDefinition.BaseType;
            if (baseTypeHandle.IsNil)
                return null;
            return baseTypeHandle.ResolveTypeDefRefOrSpec(GetEcmaModule(), instantiation.ToTypeContext());
        }

        internal sealed override IEnumerable<RoType> SpecializeInterfaces(RoType[] instantiation)
        {
            MetadataReader reader = Reader;
            EcmaModule module = GetEcmaModule();
            TypeContext typeContext = instantiation.ToTypeContext();
            foreach (InterfaceImplementationHandle h in TypeDefinition.GetInterfaceImplementations())
            {
                InterfaceImplementation ifc = h.GetInterfaceImplementation(reader);
                yield return ifc.Interface.ResolveTypeDefRefOrSpec(module, typeContext);
            }
        }

        protected sealed override IEnumerable<CustomAttributeData> GetTrueCustomAttributes() => TypeDefinition.GetCustomAttributes().ToTrueCustomAttributes(GetEcmaModule());

        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => TypeDefinition.GetCustomAttributes().IsCustomAttributeDefined(ns, name, GetEcmaModule());
        internal sealed override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => TypeDefinition.GetCustomAttributes().TryFindCustomAttribute(ns, name, GetEcmaModule());

        public sealed override int MetadataToken => _handle.GetToken();

        public sealed override bool IsGenericTypeDefinition => GetGenericParameterCount() != 0;

        internal sealed override int GetGenericParameterCount() => GetGenericTypeParametersNoCopy().Length;
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => _lazyGenericParameters ??= ComputeGenericTypeParameters();
        private RoType[] ComputeGenericTypeParameters()
        {
            EcmaModule module = GetEcmaModule();
            GenericParameterHandleCollection gps = TypeDefinition.GetGenericParameters();
            if (gps.Count == 0)
                return Array.Empty<RoType>();

            RoType[] genericParameters = new RoType[gps.Count];
            foreach (GenericParameterHandle h in gps)
            {
                RoType gp = h.ResolveGenericParameter(module);
                genericParameters[gp.GenericParameterPosition] = gp;
            }
            return genericParameters;
        }
        private volatile RoType[]? _lazyGenericParameters;

        protected internal sealed override RoType ComputeEnumUnderlyingType()
        {
            //
            // This performs the functional equivalent of the base Type GetEnumUnderlyingType without going through all the BindingFlag lookup overhead.
            //

            if (!IsEnum)
                throw new ArgumentException(SR.Arg_MustBeEnum);

            MetadataReader reader = Reader;
            TypeContext typeContext = Instantiation.ToTypeContext();
            RoType? underlyingType = null;
            foreach (FieldDefinitionHandle handle in TypeDefinition.GetFields())
            {
                FieldDefinition fd = handle.GetFieldDefinition(reader);
                if ((fd.Attributes & FieldAttributes.Static) != 0)
                    continue;
                if (underlyingType != null)
                    throw new ArgumentException(SR.Argument_InvalidEnum);
                underlyingType = fd.DecodeSignature(GetEcmaModule(), typeContext);
            }

            if (underlyingType == null)
                throw new ArgumentException(SR.Argument_InvalidEnum);
            return underlyingType;
        }

        protected sealed override void GetPackSizeAndSize(out int packSize, out int size)
        {
            TypeLayout layout = TypeDefinition.GetLayout();
            packSize = layout.PackingSize;
            size = layout.Size;
        }

        internal sealed override bool IsTypeNameEqual(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name)
        {
            MetadataReader reader = Reader;
            TypeDefinition td = TypeDefinition;
            return td.Name.Equals(name, reader) && td.Namespace.Equals(ns, reader);
        }

        private new MetadataLoadContext Loader => _module.Loader;
        private MetadataReader Reader => GetEcmaModule().Reader;

        private ref readonly TypeDefinition TypeDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughTypeDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly TypeDefinition _neverAccessThisExceptThroughTypeDefinitionProperty;
    }
}
