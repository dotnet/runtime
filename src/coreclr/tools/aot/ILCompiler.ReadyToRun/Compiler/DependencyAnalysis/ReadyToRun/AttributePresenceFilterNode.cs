// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.Text;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class AttributePresenceFilterNode : HeaderTableNode
    {
        private EcmaModule _module;

        public override int ClassCode => 56456113;

        public AttributePresenceFilterNode(EcmaModule module)
            : base(module.Context.Target)
        {
            _module = module;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunAttributePresenceFilter__");
            sb.Append(_module.Assembly.GetName().Name);
        }

        private struct CustomAttributeEntry
        {
            public string TypeNamespace;
            public string TypeName;
            public int Parent;
        }

        private List<CustomAttributeEntry> GetCustomAttributeEntries()
        {
            MetadataReader reader = _module.MetadataReader;
            List<CustomAttributeEntry> customAttributeEntries = new List<CustomAttributeEntry>();
            foreach (var handle in reader.CustomAttributes)
            {
                CustomAttribute customAttribute = reader.GetCustomAttribute(handle);
                EntityHandle customAttributeConstructorHandle = customAttribute.Constructor;
                string customAttributeTypeNamespace, customAttributeTypeName;
                ReadCustomAttributeTypeNameWithoutResolving(customAttributeConstructorHandle, out customAttributeTypeNamespace, out customAttributeTypeName);
                // System.Runtime.CompilerServices.NullableAttribute is NEVER added to the table (There are *many* of these, and they provide no useful value to the runtime)
                if (customAttributeTypeNamespace == "System.Runtime.CompilerServices" && customAttributeTypeName == "NullableAttribute")
                {
                    continue;
                }
                bool addToTable = false;
                if (customAttributeTypeNamespace.StartsWith("System.Runtime."))
                {
                    addToTable = true;
                }
                else if (customAttributeTypeNamespace == "System")
                {
                    // Some historical well known attributes were placed in the System namespace. Special case them
                    if (customAttributeTypeName == "ParamArrayAttribute")
                    {
                        addToTable = true;
                    }
                    else if (customAttributeTypeName == "ThreadStaticAttribute")
                    {
                        addToTable = true;
                    }
                }
                else if (customAttributeTypeNamespace == "System.Reflection")
                {
                    // Historical attribute in the System.Reflection namespace
                    if (customAttributeTypeName == "DefaultMemberAttribute")
                    {
                        addToTable = true;
                    }
                }

                if (!addToTable)
                    continue;

                customAttributeEntries.Add(new CustomAttributeEntry
                {
                    TypeNamespace = customAttributeTypeNamespace,
                    TypeName = customAttributeTypeName,
                    Parent = reader.GetToken(customAttribute.Parent)
                });
            }
            return customAttributeEntries;
        }

        /**
         * This class is used to extract the first type handle in a signature.
         *
         * In the case that a custom attribute's constructor is a MemberReference,
         * and its parent is a TypeSpec, we have to parse the signature, but we do
         * not want to actually resolve the types. So we used this dummy signature
         * type provider to extract the first type handle.
         */
        private class FirstTypeHandleExtractor : ISignatureTypeProvider<DummyType, DummyGenericContext>
        {
            private EntityHandle _firstTypeHandle;

            public EntityHandle FirstTypeHandle => _firstTypeHandle;

            public DummyType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                if (_firstTypeHandle.IsNil)
                {
                    _firstTypeHandle = handle;
                }
                return new DummyType();
            }

            public DummyType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                if (_firstTypeHandle.IsNil)
                {
                    _firstTypeHandle = handle;
                }
                return DummyType.Instance;
            }

            #region Uninteresting dummy methods

            // These methods are required by the interface, but it is otherwise uninteresting for our purpose here

            public DummyType GetArrayType(DummyType elementType, ArrayShape shape)
            {
                return DummyType.Instance;
            }

            public DummyType GetByReferenceType(DummyType elementType)
            {
                return DummyType.Instance;
            }

            public DummyType GetFunctionPointerType(MethodSignature<DummyType> signature)
            {
                return DummyType.Instance;
            }

            public DummyType GetGenericInstantiation(DummyType genericType, ImmutableArray<DummyType> typeArguments)
            {
                return DummyType.Instance;
            }

            public DummyType GetGenericMethodParameter(DummyGenericContext genericContext, int index)
            {
                return DummyType.Instance;
            }

            public DummyType GetGenericTypeParameter(DummyGenericContext genericContext, int index)
            {
                return DummyType.Instance;
            }

            public DummyType GetModifiedType(DummyType modifier, DummyType unmodifiedType, bool isRequired)
            {
                return DummyType.Instance;
            }

            public DummyType GetPinnedType(DummyType elementType)
            {
                return DummyType.Instance;
            }

            public DummyType GetPointerType(DummyType elementType)
            {
                return DummyType.Instance;
            }

            public DummyType GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return new DummyType();
            }

            public DummyType GetSZArrayType(DummyType elementType)
            {
                return DummyType.Instance;
            }

            public DummyType GetTypeFromSpecification(MetadataReader reader, DummyGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                return DummyType.Instance;
            }

            #endregion
        }

        #region Uninteresting dummy types

        private class DummyType
        {
            public static DummyType Instance = new DummyType();
        }

        private class DummyGenericContext
        {
        }

        #endregion

        private void ReadCustomAttributeTypeNameWithoutResolving(EntityHandle customAttributeConstructorHandle, out string customAttributeTypeNamespace, out string customAttributeTypeName)
        {
            /**
             * It is possible that the assembly that defines the attribute is not provided as a reference assembly.
             *
             * Most the time, as long as the custom attribute is not accessed or the reference assembly is available at runtime, the code will work just fine.
             *
             * If we used _module.GetMethod(customAttributeConstructorHandle), we should have caused an exception and failing the compilation.
             *
             * Therefore, we have this alternate path to obtain the type namespace and name.
             */
            if (customAttributeConstructorHandle.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinitionHandle customAttributeConstructorDefinitionHandle = (MethodDefinitionHandle)customAttributeConstructorHandle;
                MethodDefinition customAttributeConstructorDefinition = _module.MetadataReader.GetMethodDefinition(customAttributeConstructorDefinitionHandle);
                TypeDefinitionHandle customAttributeConstructorTypeDefinitionHandle = customAttributeConstructorDefinition.GetDeclaringType();
                GetTypeNameFromTypeDefinitionHandle(customAttributeConstructorTypeDefinitionHandle, out customAttributeTypeNamespace, out customAttributeTypeName);
            }
            else if (customAttributeConstructorHandle.Kind == HandleKind.MemberReference)
            {
                MemberReferenceHandle customAttributeConstructorReferenceHandle = (MemberReferenceHandle)customAttributeConstructorHandle;
                MemberReference customAttributeConstructorReference = _module.MetadataReader.GetMemberReference(customAttributeConstructorReferenceHandle);
                EntityHandle customAttributeConstructorReferenceParentHandle = customAttributeConstructorReference.Parent;
                if (customAttributeConstructorReferenceParentHandle.Kind == HandleKind.TypeReference)
                {
                    TypeReferenceHandle customAttributeConstructorTypeReferenceHandle = (TypeReferenceHandle)customAttributeConstructorReferenceParentHandle;
                    GetTypeNameFromTypeReferenceHandle(customAttributeConstructorTypeReferenceHandle, out customAttributeTypeNamespace, out customAttributeTypeName);
                }
                else
                {
                    Debug.Assert(customAttributeConstructorReferenceParentHandle.Kind == HandleKind.TypeSpecification);
                    TypeSpecificationHandle customAttributeConstructorTypeSpecificationHandle = (TypeSpecificationHandle)customAttributeConstructorReferenceParentHandle;
                    TypeSpecification customAttributeConstructorTypeSpecification = _module.MetadataReader.GetTypeSpecification(customAttributeConstructorTypeSpecificationHandle);
                    FirstTypeHandleExtractor fakeSignatureTypeProvider = new FirstTypeHandleExtractor();
                    customAttributeConstructorTypeSpecification.DecodeSignature(fakeSignatureTypeProvider, new DummyGenericContext());
                    EntityHandle firstTypeHandle = fakeSignatureTypeProvider.FirstTypeHandle;
                    if (firstTypeHandle.Kind == HandleKind.TypeDefinition)
                    {
                        TypeDefinitionHandle customAttributeConstructorTypeDefinitionHandle = (TypeDefinitionHandle)firstTypeHandle;
                        GetTypeNameFromTypeDefinitionHandle(customAttributeConstructorTypeDefinitionHandle, out customAttributeTypeNamespace, out customAttributeTypeName);
                    }
                    else
                    {
                        Debug.Assert(firstTypeHandle.Kind == HandleKind.TypeReference);
                        TypeReferenceHandle customAttributeConstructorTypeReferenceHandle = (TypeReferenceHandle)firstTypeHandle;
                        GetTypeNameFromTypeReferenceHandle(customAttributeConstructorTypeReferenceHandle, out customAttributeTypeNamespace, out customAttributeTypeName);
                    }
                }
            }
            else
            {
                Debug.Assert(false);
                customAttributeTypeNamespace = null;
                customAttributeTypeName = null;
            }
        }

        private void GetTypeNameFromTypeReferenceHandle(TypeReferenceHandle typeReferenceHandle, out string typeNamespace, out string typeName)
        {
            TypeReference typeReference = _module.MetadataReader.GetTypeReference(typeReferenceHandle);
            StringHandle typeNamespaceHandle = typeReference.Namespace;
            StringHandle typeNameHandle = typeReference.Name;
            typeNamespace = _module.MetadataReader.GetString(typeNamespaceHandle);
            typeName = _module.MetadataReader.GetString(typeNameHandle);
        }

        private void GetTypeNameFromTypeDefinitionHandle(TypeDefinitionHandle typeDefinitionHandle, out string typeNamespace, out string typeName)
        {
            TypeDefinition typeDefinition = _module.MetadataReader.GetTypeDefinition(typeDefinitionHandle);
            StringHandle typeNamespaceHandle = typeDefinition.Namespace;
            StringHandle typeNameHandle = typeDefinition.Name;
            typeNamespace = _module.MetadataReader.GetString(typeNamespaceHandle);
            typeName = _module.MetadataReader.GetString(typeNameHandle);
        }

        internal uint Xoshiro128StarStar(uint[] s)
        {
            uint result = BitOperations.RotateLeft(s[1] * 5, 7) * 9;
            uint t = s[1] << 9;

            s[2] ^= s[0];
            s[3] ^= s[1];
            s[1] ^= s[2];
            s[0] ^= s[3];

            s[2] ^= t;
            s[3] = BitOperations.RotateLeft(s[3], 11);

            return result;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            List<CustomAttributeEntry> customAttributeEntries = GetCustomAttributeEntries();
            int countOfEntries = customAttributeEntries.Count;
            if (countOfEntries == 0)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            // Buckets have 8 entries
            uint minTableBucketCount = (uint)(countOfEntries / 8) + 1;
            uint bucketCount = 1;

            // Bucket count must be power of two
            while (bucketCount < minTableBucketCount)
            {
                bucketCount *= 2;
            }

            // Resize the array.
            bool tryAgainWithBiggerTable = false;
            int countOfRetries = 0;
            ushort[] pTable;
            do
            {
                tryAgainWithBiggerTable = false;
                uint actualSizeOfTable = bucketCount * 8; // Buckets have 8 entries in them
                pTable = new ushort[actualSizeOfTable];
                uint[] state = new uint[] {729055690, 833774698, 218408041, 493449127};
                // Attempt to fill table
                foreach (var customAttributeEntry in customAttributeEntries)
                {
                    string name = customAttributeEntry.TypeNamespace + "." + customAttributeEntry.TypeName;
                    // This hashing algorithm MUST match exactly the logic in NativeCuckooFilter
                    int hashOfAttribute = ReadyToRunHashCode.NameHashCode(name);
                    uint hash = unchecked((uint)ReadyToRunHashCode.CombineTwoValuesIntoHash((uint)hashOfAttribute, (uint)customAttributeEntry.Parent));
                    ushort fingerprint = (ushort)(hash >> 16);
                    if (fingerprint == 0)
                    {
                        fingerprint = 1;
                    }
                    uint bucketAIndex = hash % bucketCount;
                    uint fingerprintHash = (uint)fingerprint;
                    uint bucketBIndex = (bucketAIndex ^ (fingerprintHash % bucketCount));
                    Debug.Assert(bucketAIndex == (bucketBIndex ^ (fingerprintHash % bucketCount)));
                    if ((Xoshiro128StarStar(state) & 1) != 0) // Randomly choose which bucket to attempt to fill first
                    {
                        uint temp = bucketAIndex;
                        bucketAIndex = bucketBIndex;
                        bucketBIndex = temp;
                    }
                    Func<uint, ushort, bool> hasEntryInBucket = (uint bucketIndex, ushort fprint) =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (pTable[(bucketIndex * 8) + i] == fprint)
                            {
                                return true;
                            }
                        }
                        return false;
                    };
                    Func<uint, bool> isEmptyEntryInBucket = (uint bucketIndex) =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (pTable[(bucketIndex * 8) + i] == 0)
                            {
                                return true;
                            }
                        }
                        return false;
                    };
                    Action<uint, ushort> fillEmptyEntryInBucket = (uint bucketIndex, ushort fprint) =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (pTable[(bucketIndex * 8) + i] == 0)
                            {
                                pTable[(bucketIndex * 8) + i] = fprint;
                                return;
                            }
                        }
                        Debug.Assert(false, "Not possible to reach here");
                    };
                    // Scan for pre-existing fingerprint entry in buckets
                    if (hasEntryInBucket(bucketAIndex, fingerprint) || hasEntryInBucket(bucketBIndex, fingerprint))
                    {
                        continue;
                    }

                    // Determine if there is space in a bucket to add a new entry
                    if (isEmptyEntryInBucket(bucketAIndex))
                    {
                        fillEmptyEntryInBucket(bucketAIndex, fingerprint);
                        continue;
                    }

                    if (isEmptyEntryInBucket(bucketBIndex))
                    {
                        fillEmptyEntryInBucket(bucketBIndex, fingerprint);
                        continue;
                    }

                    bool success = false;
                    int MaxNumKicks = 256;
                    // Note, that bucketAIndex itself was chosen randomly above.
                    for (int n = 0; !success && n < MaxNumKicks; n++)
                    {
                        // Randomly swap an entry in bucket bucketAIndex with fingerprint
                        uint entryIndexInBucket = Xoshiro128StarStar(state) & 0x7;
                        ushort temp = fingerprint;
                        fingerprint = pTable[(bucketAIndex * 8) + entryIndexInBucket];
                        pTable[(bucketAIndex * 8) + entryIndexInBucket] = temp;

                        // Find other bucket
                        fingerprintHash = (uint)fingerprint;
                        bucketAIndex = bucketAIndex ^ (fingerprintHash % bucketCount);
                        if (isEmptyEntryInBucket(bucketAIndex))
                        {
                            fillEmptyEntryInBucket(bucketAIndex, fingerprint);
                            success = true;
                        }
                    }

                    if (success)
                    {
                        continue;
                    }

                    tryAgainWithBiggerTable = true;
                }

                if (tryAgainWithBiggerTable)
                {
                    // bucket entry kicking path requires bucket counts to be power of two in size due to use of xor to retrieve second hash
                    bucketCount *= 2;
                }
            }
            while(tryAgainWithBiggerTable && ((countOfRetries++) < 2));

            byte[] result;
            if (tryAgainWithBiggerTable)
            {
                result = Array.Empty<byte>();
            }
            else
            {
                result = new byte[pTable.Length * 2];
                for (int i = 0; i < pTable.Length; i++)
                {
                    result[i * 2] = (byte)(pTable[i] % 256);
                    result[i * 2 + 1] = (byte)(pTable[i] >> 8);
                }
            }

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(16);
            builder.AddSymbol(this);
            builder.EmitBytes(result);

            return builder.ToObjectData();
        }
    }
}
