// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.TypeMapManager;
using static ILCompiler.UsageBasedTypeMapManager;

namespace ILCompiler
{
    public sealed class TypeMapMetadata
    {
        internal sealed class Map
        {
            private sealed class ThrowingMethodStub : ILStubMethod
            {
                private readonly TypeDesc _typeMapGroup;

                public ThrowingMethodStub(TypeDesc owningType, TypeDesc typeMapGroup, bool externalTypeMap, TypeSystemException ex)
                {
                    OwningType = owningType;
                    _typeMapGroup = typeMapGroup;
                    Name = $"InvalidTypeMapStub_{_typeMapGroup}_{(externalTypeMap ? "External" : "Proxy")}";
                    Exception = ex;
                }

                public TypeSystemException Exception { get; }
                public override string Name { get; }
                public override MethodIL EmitIL()
                {
                    return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
                }

                protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
                {
                    return Name.CompareTo(other.Name, StringComparison.Ordinal);
                }

                public override bool IsPInvoke => false;

                public override string DiagnosticName => Name;

                protected override int ClassCode => 1744789196;

                public override TypeDesc OwningType { get; }

                public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

                public override TypeSystemContext Context => OwningType.Context;
            }

            private readonly Dictionary<TypeDesc, TypeDesc> _associatedTypeMap = [];
            private readonly Dictionary<string, (TypeDesc type, TypeDesc trimmingTarget)> _externalTypeMap = [];
            private ThrowingMethodStub _externalTypeMapExceptionStub;
            private ThrowingMethodStub _associatedTypeMapExceptionStub;

            public Map(TypeDesc typeMapGroup)
            {
                TypeMapGroup = typeMapGroup;
            }

            public TypeDesc TypeMapGroup { get; }

            public void AddAssociatedTypeMapEntry(TypeDesc type, TypeDesc associatedType)
            {
                if (!_associatedTypeMap.TryAdd(type, associatedType))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }
            public void AddExternalTypeMapEntry(string typeName, TypeDesc type, TypeDesc trimmingTarget)
            {
                if (!_externalTypeMap.TryAdd(typeName, (type, trimmingTarget)))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }

            public void SetExternalTypeMapException(ModuleDesc stubModule, TypeSystemException exception)
            {
                if (_externalTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _externalTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, externalTypeMap: true, exception);
            }

            public void SetAssociatedTypeMapException(ModuleDesc stubModule, TypeSystemException exception)
            {
                if (_associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _associatedTypeMapExceptionStub ??= new ThrowingMethodStub(stubModule.GetGlobalModuleType(), TypeMapGroup, externalTypeMap: false, exception);
            }

            public IExternalTypeMapNode GetExternalTypeMapNode()
            {
                if (_externalTypeMapExceptionStub is not null)
                {
                    return new InvalidExternalTypeMapNode(TypeMapGroup, _externalTypeMapExceptionStub);
                }
                return new ExternalTypeMapNode(TypeMapGroup, _externalTypeMap);
            }

            public IProxyTypeMapNode GetProxyTypeMapNode()
            {
                if (_associatedTypeMapExceptionStub is not null)
                {
                    return new InvalidProxyTypeMapNode(TypeMapGroup, _associatedTypeMapExceptionStub);
                }
                return new ProxyTypeMapNode(TypeMapGroup, _associatedTypeMap);
            }
        }

        public static readonly TypeMapMetadata Empty = new TypeMapMetadata(new Dictionary<TypeDesc, Map>(), "No type maps");

        private readonly IReadOnlyDictionary<TypeDesc, Map> _states;

        private TypeMapMetadata(IReadOnlyDictionary<TypeDesc, Map> states, string diagnosticName)
        {
            _states = states;
            DiagnosticName = diagnosticName;
        }

        internal Map this[TypeDesc typeMapGroup] => _states[typeMapGroup];

        public bool IsEmpty => _states.Count == 0;

        internal IEnumerable<KeyValuePair<TypeDesc, Map>> Maps => _states;

        public string DiagnosticName { get; }

        public static TypeMapMetadata CreateFromAssembly(EcmaAssembly assembly, CompilerTypeSystemContext typeSystemContext)
        {
            Dictionary<TypeDesc, Map> typeMapStates = [];
            HashSet<EcmaAssembly> scannedAssemblies = [];

            Queue<EcmaAssembly> assembliesToScan = new Queue<EcmaAssembly>();
            assembliesToScan.Enqueue(assembly);

            while (assembliesToScan.Count > 0)
            {
                EcmaAssembly currentAssembly = assembliesToScan.Dequeue();
                if (scannedAssemblies.Contains(currentAssembly))
                    continue;

                scannedAssemblies.Add(currentAssembly);

                foreach (CustomAttributeHandle attrHandle in currentAssembly.MetadataReader.GetCustomAttributes(EntityHandle.AssemblyDefinition))
                {
                    CustomAttribute attr = currentAssembly.MetadataReader.GetCustomAttribute(attrHandle);

                    if (!MetadataExtensions.GetAttributeTypeAndConstructor(currentAssembly.MetadataReader, attrHandle, out EntityHandle attributeType, out _))
                    {
                        continue;
                    }

                    TypeDesc type = (TypeDesc)currentAssembly.GetObject(attributeType);

                    TypeMapAttributeKind attrKind = LookupTypeMapType(type);

                    if (attrKind == TypeMapAttributeKind.None)
                    {
                        // Not a type map attribute, skip it
                        continue;
                    }

                    CustomAttributeValue<TypeDesc> attrValue = attr.DecodeValue(new CustomAttributeTypeProvider(currentAssembly));

                    TypeDesc typeMapGroup = type.Instantiation[0];

                    try
                    {
                        switch (attrKind)
                        {
                            case TypeMapAttributeKind.TypeMapAssemblyTarget:
                                ProcessTypeMapAssemblyTargetAttribute(attrValue);
                                break;

                            case TypeMapAttributeKind.TypeMap:
                                ProcessTypeMapAttribute(attrValue, typeMapGroup);
                                break;

                            case TypeMapAttributeKind.TypeMapAssociation:
                                ProcessTypeMapAssociationAttribute(attrValue, typeMapGroup);
                                break;

                            default:
                                Debug.Fail($"Unexpected TypeMapAttributeKind: {attrKind}");
                                break;
                        }
                    }
                    catch (TypeSystemException ex)
                    {
                        if (!typeMapStates.TryGetValue(typeMapGroup, out Map value))
                        {
                            value = new Map(typeMapGroup);
                            typeMapStates[typeMapGroup] = value;
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMap)
                        {
                            value.SetExternalTypeMapException(typeSystemContext.GeneratedAssembly, ex);
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            value.SetAssociatedTypeMapException(typeSystemContext.GeneratedAssembly, ex);
                        }
                    }
                }

                void ProcessTypeMapAssemblyTargetAttribute(CustomAttributeValue<TypeDesc> attrValue)
                {
                    if (attrValue.FixedArguments is not [{ Value: string assemblyName }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    EcmaAssembly targetAssembly = (EcmaAssembly)assembly.Context.ResolveAssembly(AssemblyNameInfo.Parse(assemblyName), throwIfNotFound: true);

                    assembliesToScan.Enqueue(targetAssembly);
                }

                void ProcessTypeMapAttribute(CustomAttributeValue<TypeDesc> attrValue, TypeDesc typeMapGroup)
                {
                    switch (attrValue.FixedArguments)
                    {
                        case [{ Value: string typeName }, { Value: TypeDesc targetType }]:
                        {
                            if (!typeMapStates.TryGetValue(typeMapGroup, out Map typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new Map(typeMapGroup);
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, targetType);
                            break;
                        }

                        case [{ Value: string typeName }, { Value: TypeDesc targetType }, { Value: TypeDesc trimTargetType }]:
                        {
                            if (!typeMapStates.TryGetValue(typeMapGroup, out Map typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new Map(typeMapGroup);
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, trimTargetType);
                            break;
                        }

                        default:
                            ThrowHelper.ThrowBadImageFormatException();
                            return;
                    }
                }

                void ProcessTypeMapAssociationAttribute(CustomAttributeValue<TypeDesc> attrValue, TypeDesc typeMapGroup)
                {
                    // If attribute is TypeMapAssociationAttribute, we need to extract the generic argument (type map group)
                    // and process it.
                    if (attrValue.FixedArguments is not [{ Value: TypeDesc type }, { Value: TypeDesc associatedType }])
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                        return;
                    }

                    if (!typeMapStates.TryGetValue(typeMapGroup, out Map typeMapState))
                    {
                        typeMapStates[typeMapGroup] = typeMapState = new Map(typeMapGroup);
                    }

                    typeMapState.AddAssociatedTypeMapEntry(type, associatedType);
                }
            }

            return new TypeMapMetadata(typeMapStates, $"Type maps rooted at {assembly}");
        }
    }
}
