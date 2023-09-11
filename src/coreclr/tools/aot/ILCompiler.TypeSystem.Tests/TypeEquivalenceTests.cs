// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;
using Xunit.Abstractions;

namespace TypeSystemTests
{
    public class Entrypoint
    {
        class Logger : ITestOutputHelper
        {
            void ITestOutputHelper.WriteLine(string message) => Console.WriteLine(message);
            void ITestOutputHelper.WriteLine(string format, params object[] args) => Console.WriteLine(format, args);
        }

        public static void NotQuiteMain()
        {
            TypeEquivalenceTests tests = new TypeEquivalenceTests(new Logger());
            tests.TestTypesWhichShouldMatch();
        }
    }
    public class TypeEquivalenceTests
    {
        private TestTypeSystemContext _context;
        private EcmaModule _testModule1;
        private EcmaModule _testModule2;

        private MetadataType _referenceType;
        private MetadataType _otherReferenceType;
        private MetadataType _structType;
        private MetadataType _otherStructType;
        private MetadataType _genericReferenceType;
        private MetadataType _genericStructType;
        private MetadataType _genericReferenceTypeWithThreeParams;
        private MetadataType _genericStructTypeWithThreeParams;
        private MetadataType _interfaceGenericType;

        private ITestOutputHelper _logger;

        public TypeEquivalenceTests(ITestOutputHelper outputHelper)
        {
            _logger = outputHelper;
            _context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule1 = (EcmaModule)_context.CreateModuleForSimpleName("TypeEquivalenceAssembly1");
            _testModule2 = (EcmaModule)_context.CreateModuleForSimpleName("TypeEquivalenceAssembly2");

            _referenceType = systemModule.GetType("Canonicalization", "ReferenceType");
            _otherReferenceType = systemModule.GetType("Canonicalization", "OtherReferenceType");
            _structType = systemModule.GetType("Canonicalization", "StructType");
            _otherStructType = systemModule.GetType("Canonicalization", "OtherStructType");
            _genericReferenceType = systemModule.GetType("Canonicalization", "GenericReferenceType`1");
            _genericStructType = systemModule.GetType("Canonicalization", "GenericStructType`1");
            _genericReferenceTypeWithThreeParams = systemModule.GetType("Canonicalization", "GenericReferenceTypeWithThreeParams`3");
            _genericStructTypeWithThreeParams = systemModule.GetType("Canonicalization", "GenericStructTypeWithThreeParams`3");
            _interfaceGenericType = systemModule.GetType("Canonicalization", "InterfaceGenericType`1");
        }

        private IEnumerable<TypeDefinitionHandle> GetAllNestedTypes(MetadataReader metadataReader, TypeDefinition typeDef)
        {
            foreach (var nestedHandle in typeDef.GetNestedTypes())
            {
                yield return nestedHandle;
                var nestedType = metadataReader.GetTypeDefinition(nestedHandle);
                foreach (var moreNestedHandle in GetAllNestedTypes(metadataReader, nestedType))
                {
                    yield return moreNestedHandle;
                }
            }
        }

        private IEnumerable<TypeDefinitionHandle> GetAllTypesInNamespace(EcmaModule module, string @namespace)
        {
            var metadataReader = module.MetadataReader;
            foreach (var typeDefHandle in metadataReader.TypeDefinitions)
            {
                var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                if (typeDef.IsNested)
                {
                    continue; // Ignore nested types for now
                }

                if (metadataReader.StringComparer.Equals(typeDef.Namespace, @namespace))
                {
                    yield return typeDefHandle;
                    foreach (var nestedHandle in GetAllNestedTypes(metadataReader, typeDef))
                    {
                        yield return nestedHandle;
                    }
                }
            }
        }

        private static bool IsEqualCustomAttributeName(CustomAttributeHandle attributeHandle, MetadataReader metadataReader,
            string attributeNamespace, string attributeName)
        {
            StringHandle namespaceHandle, nameHandle;
            if (!metadataReader.GetAttributeNamespaceAndName(attributeHandle, out namespaceHandle, out nameHandle))
                return false;

            return metadataReader.StringComparer.Equals(namespaceHandle, attributeNamespace)
                && metadataReader.StringComparer.Equals(nameHandle, attributeName);
        }

        private string GetTypeIdentiferFromTypeDef(EcmaModule module, TypeDefinitionHandle typeDefHandle)
        {
            CustomAttributeTypeProvider customAttributeTypeProvider = new CustomAttributeTypeProvider(module);
            var typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle);
            foreach (var attributeHandle in typeDef.GetCustomAttributes())
            {
                if (IsEqualCustomAttributeName(attributeHandle, module.MetadataReader, "System.Runtime.InteropServices", "TypeIdentifierAttribute"))
                {
                    var typeIdentifierAttribute = module.MetadataReader.GetCustomAttribute(attributeHandle).DecodeValue(customAttributeTypeProvider);

                    if (typeIdentifierAttribute.FixedArguments.Length != 2)
                        throw new Exception("Unexpected in this test suite");

                    return $"{typeIdentifierAttribute.FixedArguments[0].Value}_{typeIdentifierAttribute.FixedArguments[1].Value}";
                }
            }

            return null;
        }

        private Dictionary<string, TypeDefinitionHandle> GetTypeIdentifierAssociatedTypesInNamespace(EcmaModule module, string @namespace)
        {
            Dictionary<string, TypeDefinitionHandle> result = new Dictionary<string, TypeDefinitionHandle>();
            foreach (var typeDef in GetAllTypesInNamespace(module, @namespace))
            {
                string typeId = GetTypeIdentiferFromTypeDef(module, typeDef);
                if (typeId != null)
                {
                    result.Add(typeId, typeDef);
                }
            }
            return result;
        }

        private IEnumerable<ValueTuple<TypeDesc, TypeDesc>> GetTypesWhichClaimMatchingTypeIdentifiersInNamespace(string @namespace)
        {
            var module1Types = GetTypeIdentifierAssociatedTypesInNamespace(_testModule1, @namespace);
            var module2Types = GetTypeIdentifierAssociatedTypesInNamespace(_testModule2, @namespace);

            foreach (var data in module1Types)
            {
                if (module2Types.TryGetValue(data.Key, out var typeDef2))
                {
                    yield return ((TypeDesc)_testModule1.GetObject(data.Value), (TypeDesc)_testModule2.GetObject(typeDef2));
                }
            }
        }

        [Fact]
        public void TestTypesWhichShouldMatch()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichMatch"))
            {
                _logger.WriteLine($"Comparing {typePair.Item1} to {typePair.Item2}");
                Assert.NotEqual(typePair.Item1, typePair.Item2);
                Assert.True(typePair.Item1.IsEquivalentTo(typePair.Item2));
                Assert.True(typePair.Item1.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType);
                Assert.True(typePair.Item2.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType);
            }
        }

        [Fact]
        public void TestGenericInterfacesWithTypeEquivalence()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichMatch"))
            {
                var gen1 = _interfaceGenericType.MakeInstantiatedType(typePair.Item1);
                var gen2 = _interfaceGenericType.MakeInstantiatedType(typePair.Item2);

                _logger.WriteLine($"Comparing {gen1} to {gen2}");
                Assert.NotEqual(gen1, gen2);
                Assert.True(gen1.IsEquivalentTo(gen2));
            }
        }

        [Fact]
        public void TestGenericClassesWithTypeEquivalence()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichMatch"))
            {
                var gen1 = _genericReferenceType.MakeInstantiatedType(typePair.Item1);
                var gen2 = _genericReferenceType.MakeInstantiatedType(typePair.Item2);

                _logger.WriteLine($"Comparing{gen1} to {gen2}");
                Assert.NotEqual(gen1, gen2);
                Assert.False(gen1.IsEquivalentTo(gen2));
            }
        }

        [Fact]
        public void TestGenericStructsWithTypeEquivalence()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichMatch"))
            {
                var gen1 = _genericStructType.MakeInstantiatedType(typePair.Item1);
                var gen2 = _genericStructType.MakeInstantiatedType(typePair.Item2);

                _logger.WriteLine($"Comparing {gen1} to {gen2}");
                Assert.NotEqual(gen1, gen2);
                Assert.False(gen1.IsEquivalentTo(gen2));
            }
        }

        [Fact]
        public void TestTypesWhichShouldNotMatch()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichDoNotMatch"))
            {
                _logger.WriteLine($"Comparing {typePair.Item1} to {typePair.Item2}");
                Assert.False(typePair.Item1.IsEquivalentTo(typePair.Item2));
                Assert.True(typePair.Item1.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType);
                Assert.True(typePair.Item2.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType);
            }
        }

        [Fact]
        public void TestTypesWhichShouldNotBeLoadable()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichDoNotLoad"))
            {
                if (((MetadataType)typePair.Item1).Name.EndsWith("IGNORE"))
                    continue;

                _logger.WriteLine($"Checking load behavior of {typePair.Item1}");
                Assert.False(typePair.Item1.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType);
            }
        }
    }
}
