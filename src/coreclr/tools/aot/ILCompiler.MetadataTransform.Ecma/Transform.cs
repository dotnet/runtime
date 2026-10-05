// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Metadata.Ecma;

internal sealed class Transform<TPolicy> where TPolicy : struct, IMetadataPolicy
{
    private readonly EcmaModule _module;
    private readonly MetadataReader _reader;
    private readonly TPolicy _policy;
    private readonly MetadataBuilder _metadata = new MetadataBuilder();
    private readonly HashSet<string> _strings;
    private readonly Dictionary<(EcmaModule, EntityHandle), EntityHandle> _tokens = new();
    private readonly Dictionary<EcmaType, TypeReferenceHandle> _typeReferences = new();
    private readonly Dictionary<EcmaModule, AssemblyReferenceHandle> _assemblyReferences = new();
    private readonly Dictionary<(StringHandle, Version, StringHandle, BlobHandle, AssemblyFlags), AssemblyReferenceHandle> _assemblyReferenceRecords = new();
    private readonly Dictionary<BlobHandle, TypeSpecificationHandle> _typeSpecifications = new();
    private readonly Dictionary<(EntityHandle, StringHandle, BlobHandle), MemberReferenceHandle> _memberReferences = new();
    private readonly HashSet<TypeDefinitionHandle> _propertyOwners = new();
    private readonly HashSet<TypeDefinitionHandle> _eventOwners = new();
    private readonly List<TypeDefinitionHandle> _types = new();
    private readonly List<(EntityHandle Owner, GenericParameterHandle Parameter)> _genericParameters = new();

    public bool HasMetadata { get; private set; }

    public Transform(EcmaModule module, TPolicy policy, HashSet<string> strings)
    {
        _module = module;
        _reader = module.MetadataReader;
        _policy = policy;
        _strings = strings;

        // Definition handles must be assigned before rewriting any signatures. Lists are
        // contiguous in their owning type/method (ECMA-335 II.22.15, II.22.26 and II.22.37).
        int typeRow = 0;
        int fieldRow = 0;
        int methodRow = 0;
        foreach (TypeDefinitionHandle handle in _reader.TypeDefinitions)
        {
            EcmaType type = module.GetType(handle);
            bool selected = policy.GeneratesMetadata(type) && !policy.IsBlocked(type);
            if (!selected && !type.IsModuleType)
                continue;

            HasMetadata |= selected;
            _types.Add(handle);
            _tokens.Add((module, handle), MetadataTokens.TypeDefinitionHandle(++typeRow));
            if (!selected)
                continue;

            TypeDefinition definition = _reader.GetTypeDefinition(handle);
            foreach (FieldDefinitionHandle field in definition.GetFields())
                if (policy.GeneratesMetadata(module.GetField(field)))
                    _tokens.Add((module, field), MetadataTokens.FieldDefinitionHandle(++fieldRow));
            foreach (MethodDefinitionHandle method in definition.GetMethods())
                if (policy.GeneratesMetadata(module.GetMethod(method)))
                    _tokens.Add((module, method), MetadataTokens.MethodDefinitionHandle(++methodRow));
        }
    }

    public MetadataBuilder Generate()
    {
        ModuleDefinition module = _reader.GetModuleDefinition();
        ModuleDefinitionHandle moduleHandle = _metadata.AddModule(
            0, CopyString(module.Name), _metadata.GetOrAddGuid(_reader.GetGuid(module.Mvid)), default, default);
        AssemblyDefinition assembly = _reader.GetAssemblyDefinition();
        AssemblyDefinitionHandle assemblyHandle = _metadata.AddAssembly(
            CopyString(assembly.Name), assembly.Version, CopyString(assembly.Culture),
            CopyBlob(assembly.PublicKey), assembly.Flags, assembly.HashAlgorithm);

        AddCustomAttributes(moduleHandle, _reader.GetCustomAttributes(MetadataTokens.EntityHandle(TableIndex.Module, 1)));
        AddCustomAttributes(assemblyHandle, assembly.GetCustomAttributes());

        foreach (TypeDefinitionHandle handle in _types)
            AddType(handle);

        // SRM requires GenericParam rows sorted by the coded owner, not by table.
        _genericParameters.Sort(static (a, b) =>
        {
            int result = CodedIndex.TypeOrMethodDef(a.Owner).CompareTo(CodedIndex.TypeOrMethodDef(b.Owner));
            return result != 0 ? result : MetadataTokens.GetRowNumber(a.Parameter).CompareTo(MetadataTokens.GetRowNumber(b.Parameter));
        });
        foreach ((EntityHandle owner, GenericParameterHandle handle) in _genericParameters)
        {
            GenericParameter parameter = _reader.GetGenericParameter(handle);
            GenericParameterHandle mapped = _metadata.AddGenericParameter(owner, parameter.Attributes, CopyString(parameter.Name), parameter.Index);
            foreach (GenericParameterConstraintHandle constraintHandle in parameter.GetConstraints())
            {
                GenericParameterConstraint constraint = _reader.GetGenericParameterConstraint(constraintHandle);
                _metadata.AddGenericParameterConstraint(mapped, MapToken(_module, constraint.Type));
            }
            AddCustomAttributes(mapped, parameter.GetCustomAttributes());
        }

        foreach (ExportedTypeHandle handle in _reader.ExportedTypes)
            if (_policy.GeneratesMetadata(_module, handle))
                MapToken(_module, handle);

        return _metadata;
    }

    private void AddType(TypeDefinitionHandle handle)
    {
        TypeDefinition type = _reader.GetTypeDefinition(handle);
        EcmaType entity = _module.GetType(handle);
        bool selected = _policy.GeneratesMetadata(entity) && !_policy.IsBlocked(entity);
        TypeDefinitionHandle mapped = _metadata.AddTypeDefinition(
            type.Attributes, CopyString(type.Namespace), CopyString(type.Name),
            selected ? MapToken(_module, type.BaseType) : default,
            MetadataTokens.FieldDefinitionHandle(_metadata.GetRowCount(TableIndex.Field) + 1),
            MetadataTokens.MethodDefinitionHandle(_metadata.GetRowCount(TableIndex.MethodDef) + 1));
        Debug.Assert(mapped == _tokens[(_module, handle)]);
        if (!selected)
            return;

        TypeDefinitionHandle declaringType = type.GetDeclaringType();
        if (!declaringType.IsNil)
            _metadata.AddNestedType(mapped, (TypeDefinitionHandle)_tokens[(_module, declaringType)]);

        TypeLayout layout = type.GetLayout();
        if (!layout.IsDefault)
            _metadata.AddTypeLayout(mapped, checked((ushort)layout.PackingSize), checked((uint)layout.Size));

        foreach (InterfaceImplementationHandle interfaceHandle in type.GetInterfaceImplementations())
        {
            InterfaceImplementation implementation = _reader.GetInterfaceImplementation(interfaceHandle);
            var interfaceType = (MetadataType)_module.GetType(implementation.Interface);
            if (!IsBlocked(interfaceType) && _policy.GeneratesInterfaceImpl(entity, interfaceType))
                _metadata.AddInterfaceImplementation(mapped, MapToken(_module, implementation.Interface));
        }

        AddGenericParameters(mapped, type.GetGenericParameters());
        foreach (FieldDefinitionHandle field in type.GetFields())
            if (_tokens.ContainsKey((_module, field)))
                AddField(field);
        foreach (MethodDefinitionHandle method in type.GetMethods())
            if (_tokens.ContainsKey((_module, method)))
                AddMethod(method);
        foreach (PropertyDefinitionHandle property in type.GetProperties())
            AddProperty(mapped, property);
        foreach (EventDefinitionHandle @event in type.GetEvents())
            AddEvent(mapped, @event);
        AddCustomAttributes(mapped, type.GetCustomAttributes());
    }

    private void AddField(FieldDefinitionHandle handle)
    {
        FieldDefinition field = _reader.GetFieldDefinition(handle);
        FieldDefinitionHandle mapped = _metadata.AddFieldDefinition(
            field.Attributes, CopyString(field.Name),
            RewriteSignature(_module, field.Signature, EcmaSignatureRewriter.RewriteFieldSignature));
        Debug.Assert(mapped == _tokens[(_module, handle)]);
        int offset = field.GetOffset();
        if (offset >= 0)
            _metadata.AddFieldLayout(mapped, offset);
        AddConstant(mapped, field.GetDefaultValue());
        AddCustomAttributes(mapped, field.GetCustomAttributes());
    }

    private void AddMethod(MethodDefinitionHandle handle)
    {
        MethodDefinition method = _reader.GetMethodDefinition(handle);
        MethodDefinitionHandle mapped = _metadata.AddMethodDefinition(
            method.Attributes, method.ImplAttributes, CopyString(method.Name),
            RewriteSignature(_module, method.Signature, EcmaSignatureRewriter.RewriteMethodSignature),
            bodyOffset: -1, MetadataTokens.ParameterHandle(_metadata.GetRowCount(TableIndex.Param) + 1));
        Debug.Assert(mapped == _tokens[(_module, handle)]);
        foreach (ParameterHandle parameterHandle in method.GetParameters())
        {
            if (!_policy.GeneratesMetadata(_module, parameterHandle))
                continue;
            Parameter parameter = _reader.GetParameter(parameterHandle);
            ParameterHandle mappedParameter = _metadata.AddParameter(parameter.Attributes, CopyString(parameter.Name), parameter.SequenceNumber);
            AddConstant(mappedParameter, parameter.GetDefaultValue());
            AddCustomAttributes(mappedParameter, parameter.GetCustomAttributes());
        }
        AddGenericParameters(mapped, method.GetGenericParameters());
        AddCustomAttributes(mapped, method.GetCustomAttributes());
    }

    private bool HasMethod(MethodDefinitionHandle handle) => !handle.IsNil && _tokens.ContainsKey((_module, handle));

    private void AddProperty(TypeDefinitionHandle owner, PropertyDefinitionHandle handle)
    {
        PropertyDefinition property = _reader.GetPropertyDefinition(handle);
        PropertyAccessors accessors = property.GetAccessors();
        if (!HasMethod(accessors.Getter) && !HasMethod(accessors.Setter))
            return;
        PropertyDefinitionHandle mapped = _metadata.AddProperty(
            property.Attributes, CopyString(property.Name),
            RewriteSignature(_module, property.Signature, EcmaSignatureRewriter.RewritePropertySignature));
        if (_propertyOwners.Add(owner))
        {
            _metadata.AddPropertyMap(owner, mapped);
        }
        AddSemantics(mapped, MethodSemanticsAttributes.Getter, accessors.Getter);
        AddSemantics(mapped, MethodSemanticsAttributes.Setter, accessors.Setter);
        AddConstant(mapped, property.GetDefaultValue());
        AddCustomAttributes(mapped, property.GetCustomAttributes());
    }

    private void AddEvent(TypeDefinitionHandle owner, EventDefinitionHandle handle)
    {
        EventDefinition @event = _reader.GetEventDefinition(handle);
        EventAccessors accessors = @event.GetAccessors();
        if (!HasMethod(accessors.Adder) && !HasMethod(accessors.Remover) && !HasMethod(accessors.Raiser))
            return;
        EventDefinitionHandle mapped = _metadata.AddEvent(@event.Attributes, CopyString(@event.Name), MapToken(_module, @event.Type));
        if (_eventOwners.Add(owner))
        {
            _metadata.AddEventMap(owner, mapped);
        }
        AddSemantics(mapped, MethodSemanticsAttributes.Adder, accessors.Adder);
        AddSemantics(mapped, MethodSemanticsAttributes.Remover, accessors.Remover);
        AddSemantics(mapped, MethodSemanticsAttributes.Raiser, accessors.Raiser);
        AddCustomAttributes(mapped, @event.GetCustomAttributes());
    }

    private void AddSemantics(EntityHandle association, MethodSemanticsAttributes semantics, MethodDefinitionHandle method)
    {
        if (HasMethod(method))
            _metadata.AddMethodSemantics(association, semantics, (MethodDefinitionHandle)_tokens[(_module, method)]);
    }

    private void AddGenericParameters(EntityHandle owner, GenericParameterHandleCollection parameters)
    {
        foreach (GenericParameterHandle parameter in parameters)
            _genericParameters.Add((owner, parameter));
    }

    private void AddConstant(EntityHandle owner, ConstantHandle handle)
    {
        if (handle.IsNil)
            return;
        Constant constant = _reader.GetConstant(handle);
        BlobReader value = _reader.GetBlobReader(constant.Value);
        _metadata.AddConstant(owner, value.ReadConstant(constant.TypeCode));
    }

    private void AddCustomAttributes(EntityHandle owner, CustomAttributeHandleCollection attributes)
    {
        foreach (CustomAttributeHandle handle in attributes)
        {
            if (!_policy.GeneratesMetadata(_module, handle))
                continue;
            CustomAttribute attribute = _reader.GetCustomAttribute(handle);
            _metadata.AddCustomAttribute(owner, MapToken(_module, attribute.Constructor), CopyBlob(attribute.Value));
        }
    }

    private StringHandle CopyString(StringHandle handle) => GetOrAddString(_reader.GetString(handle));

    private StringHandle GetOrAddString(string value)
    {
        _strings.Add(value);

        return _metadata.GetOrAddString(value);
    }

    private BlobHandle CopyBlob(BlobHandle handle) => handle.IsNil ? default : _metadata.GetOrAddBlob(_reader.GetBlobContent(handle));

    private BlobHandle RewriteSignature(EcmaModule module, BlobHandle handle, Action<BlobReader, Func<EntityHandle, EntityHandle>, BlobBuilder> rewrite)
    {
        var blob = new BlobBuilder();
        rewrite(module.MetadataReader.GetBlobReader(handle), token => MapToken(module, token), blob);

        return _metadata.GetOrAddBlob(blob);
    }

    private bool IsBlocked(TypeDesc type)
    {
        if (type is ParameterizedType parameterizedType)
            return IsBlocked(parameterizedType.ParameterType);
        if (type is GenericParameterDesc or SignatureVariable)
            return false;
        if (type is FunctionPointerType functionPointer)
        {
            Internal.TypeSystem.MethodSignature signature = functionPointer.Signature;
            foreach (TypeDesc parameter in signature)
                if (IsBlocked(parameter))
                    return true;

            return IsBlocked(signature.ReturnType);
        }
        if (_policy.IsBlocked((MetadataType)type.GetTypeDefinition()))
            return true;
        foreach (TypeDesc argument in type.Instantiation)
            if (IsBlocked(argument))
                return true;

        return false;
    }

    private EntityHandle MapToken(EcmaModule module, EntityHandle handle)
    {
        if (handle.IsNil)
            return default;
        if (_tokens.TryGetValue((module, handle), out EntityHandle mapped))
            return mapped;

        MetadataReader reader = module.MetadataReader;
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
            case HandleKind.TypeReference:
                mapped = MapType((EcmaType)module.GetType(handle));
                break;
            case HandleKind.TypeSpecification:
                BlobHandle signature = RewriteSignature(module, reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature, EcmaSignatureRewriter.RewriteTypeSpecSignature);
                if (!_typeSpecifications.TryGetValue(signature, out TypeSpecificationHandle typeSpec))
                {
                    typeSpec = _metadata.AddTypeSpecification(signature);
                    _typeSpecifications.Add(signature, typeSpec);
                }
                mapped = typeSpec;
                break;
            case HandleKind.MethodDefinition:
                MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                mapped = AddMemberReference(MapToken(module, method.GetDeclaringType()), reader.GetString(method.Name),
                    RewriteSignature(module, method.Signature, EcmaSignatureRewriter.RewriteMethodSignature));
                break;
            case HandleKind.MemberReference:
                MemberReference member = reader.GetMemberReference((MemberReferenceHandle)handle);
                // Resolve attribute constructors to local MethodDefs when one is available.
                MethodDesc resolved = module.GetMethod(handle);
                if (resolved is EcmaMethod ecmaMethod && _tokens.TryGetValue((ecmaMethod.Module, ecmaMethod.Handle), out mapped))
                    break;
                mapped = AddMemberReference(MapToken(module, member.Parent), reader.GetString(member.Name),
                    RewriteSignature(module, member.Signature, EcmaSignatureRewriter.RewriteMemberReferenceSignature));
                break;
            case HandleKind.AssemblyReference:
                AssemblyReference assembly = reader.GetAssemblyReference((AssemblyReferenceHandle)handle);
                mapped = AddAssemblyReference(
                    reader.GetString(assembly.Name), assembly.Version, reader.GetString(assembly.Culture),
                    _metadata.GetOrAddBlob(reader.GetBlobContent(assembly.PublicKeyOrToken)), assembly.Flags);
                break;
            case HandleKind.ExportedType:
                ExportedType exportedType = reader.GetExportedType((ExportedTypeHandle)handle);
                mapped = _metadata.AddExportedType(exportedType.Attributes,
                    GetOrAddString(reader.GetString(exportedType.Namespace)),
                    GetOrAddString(reader.GetString(exportedType.Name)),
                    MapToken(module, exportedType.Implementation), typeDefinitionId: 0);
                break;
            default:
                throw new BadImageFormatException($"Unexpected metadata handle kind: {handle.Kind}");
        }
        _tokens.Add((module, handle), mapped);

        return mapped;
    }

    private EntityHandle MapType(EcmaType type)
    {
        if (_tokens.TryGetValue((type.Module, type.Handle), out EntityHandle definition))
            return definition;

        return MapTypeReference(type);
    }

    private TypeReferenceHandle MapTypeReference(EcmaType type)
    {
        if (_typeReferences.TryGetValue(type, out TypeReferenceHandle reference))
            return reference;
        EntityHandle scope = type.ContainingType is EcmaType containingType
            ? MapTypeReference(containingType)
            : type.Module == _module ? MetadataTokens.EntityHandle(TableIndex.Module, 1) : MapAssembly(type.Module);
        TypeDefinition definition = type.MetadataReader.GetTypeDefinition(type.Handle);
        reference = _metadata.AddTypeReference(scope,
            GetOrAddString(type.MetadataReader.GetString(definition.Namespace)),
            GetOrAddString(type.MetadataReader.GetString(definition.Name)));
        _typeReferences.Add(type, reference);

        return reference;
    }

    private AssemblyReferenceHandle MapAssembly(EcmaModule module)
    {
        if (_assemblyReferences.TryGetValue(module, out AssemblyReferenceHandle reference))
            return reference;
        AssemblyNameInfo name = module.Assembly.GetName();
        byte[] key = ImmutableCollectionsMarshal.AsArray(name.PublicKeyOrToken);
        if ((name.Flags & AssemblyNameFlags.PublicKey) != 0)
        {
            var assemblyName = new AssemblyName();
            assemblyName.SetPublicKey(key);
            key = assemblyName.GetPublicKeyToken();
        }
        reference = AddAssemblyReference(
            name.Name, name.Version, name.CultureName,
            _metadata.GetOrAddBlob(key), (AssemblyFlags)(name.Flags & ~AssemblyNameFlags.PublicKey));
        _assemblyReferences.Add(module, reference);

        return reference;
    }

    private AssemblyReferenceHandle AddAssemblyReference(string name, Version version, string culture, BlobHandle keyOrToken, AssemblyFlags flags)
    {
        StringHandle nameHandle = GetOrAddString(name);
        StringHandle cultureHandle = GetOrAddString(culture);
        var key = (nameHandle, version, cultureHandle, keyOrToken, flags);
        if (!_assemblyReferenceRecords.TryGetValue(key, out AssemblyReferenceHandle reference))
        {
            reference = _metadata.AddAssemblyReference(nameHandle, version, cultureHandle, keyOrToken, flags, default);
            _assemblyReferenceRecords.Add(key, reference);
        }

        return reference;
    }

    private MemberReferenceHandle AddMemberReference(EntityHandle parent, string name, BlobHandle signature)
    {
        StringHandle nameHandle = GetOrAddString(name);
        var key = (parent, nameHandle, signature);
        if (!_memberReferences.TryGetValue(key, out MemberReferenceHandle reference))
        {
            reference = _metadata.AddMemberReference(parent, nameHandle, signature);
            _memberReferences.Add(key, reference);
        }

        return reference;
    }
}
