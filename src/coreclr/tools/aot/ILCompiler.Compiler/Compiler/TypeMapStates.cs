// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.TypeMapManager;

namespace ILCompiler
{
    public sealed class TypeMapStates
    {
        public static readonly TypeMapStates Empty = new TypeMapStates(new Dictionary<TypeDesc, TypeMapState>());

        private readonly IReadOnlyDictionary<TypeDesc, TypeMapState> _states;

        internal TypeMapStates(IReadOnlyDictionary<TypeDesc, TypeMapState> states)
        {
            _states = states;
        }

        internal TypeMapState this[TypeDesc typeMapGroup] => _states[typeMapGroup];

        public bool IsEmpty => _states.Count == 0;

        internal IEnumerable<KeyValuePair<TypeDesc, TypeMapState>> States => _states;

        public static TypeMapStates CreateFromAssembly(EcmaAssembly assembly)
        {
            Dictionary<TypeDesc, TypeMapState> typeMapStates = [];
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
                        if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState value))
                        {
                            value = new TypeMapState();
                            typeMapStates[typeMapGroup] = value;
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMap)
                        {
                            value.SetExternalTypeMapStub(new ThrowingMethodStub(assembly.GetGlobalModuleType(), typeMapGroup, externalTypeMap: true, ex));
                        }

                        if (attrKind is TypeMapAttributeKind.TypeMapAssemblyTarget or TypeMapAttributeKind.TypeMapAssociation)
                        {
                            value.SetAssociatedTypeMapStub(new ThrowingMethodStub(assembly.GetGlobalModuleType(), typeMapGroup, externalTypeMap: false, ex));
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
                            if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                            }
                            typeMapState.AddExternalTypeMapEntry(typeName, targetType, targetType);
                            break;
                        }

                        case [{ Value: string typeName }, { Value: TypeDesc targetType }, { Value: TypeDesc trimTargetType }]:
                        {
                            if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                            {
                                typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
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

                    if (!typeMapStates.TryGetValue(typeMapGroup, out TypeMapState typeMapState))
                    {
                        typeMapStates[typeMapGroup] = typeMapState = new TypeMapState();
                    }

                    typeMapState.AddAssociatedTypeMapEntry(type, associatedType);
                }
            }

            return new TypeMapStates(typeMapStates);
        }
    }
}
