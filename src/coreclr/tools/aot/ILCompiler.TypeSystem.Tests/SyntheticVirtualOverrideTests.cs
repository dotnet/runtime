// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Internal.TypeSystem;

using Xunit;


namespace TypeSystemTests
{
    public partial class SyntheticVirtualOverrideTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        public SyntheticVirtualOverrideTests()
        {
            _context = new SyntheticVirtualOverrideTypeSystemContext();
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestStructEqualsAndGetHashCode()
        {
            //
            // Tests that a struct with no Equals and GetHashCode overrides
            // receive a synthetic implementation of these courtesy of the
            // EqualsAndGetHashCodeProvidingAlgorithm.
            //

            MetadataType t = _testModule.GetType("SyntheticVirtualOverride", "StructWithNoEqualsAndGetHashCode");

            Assert.DoesNotContain(t.GetMethods(), m => m.Name == "Equals");
            Assert.DoesNotContain(t.GetMethods(), m => m.Name == "GetHashCode");

            List<MethodDesc> introducedVirtualMethods = new List<MethodDesc>(t.GetAllMethods().Where(m => m.IsVirtual));
            Assert.Equal(2, introducedVirtualMethods.Count);
            Assert.Contains(introducedVirtualMethods, m => m.Name == "Equals");
            Assert.Contains(introducedVirtualMethods, m => m.Name == "GetHashCode");
            Assert.All(introducedVirtualMethods, m => { Assert.Same(t, m.OwningType); });

            List<MethodDesc> virtualSlots = new List<MethodDesc>(t.EnumAllVirtualSlots());
            Assert.All(virtualSlots, s => { Assert.True(s.OwningType.IsObject); });
            Assert.Equal(4, virtualSlots.Count);

            List<MethodDesc> vtable = virtualSlots.Select(s => t.FindVirtualFunctionTargetMethodOnObjectType(s)).ToList();

            Assert.Contains(vtable, m => m.Name == "Equals" && m.OwningType == t);
            Assert.Contains(vtable, m => m.Name == "GetHashCode" && m.OwningType == t);
            Assert.Contains(vtable, m => m.Name == "Finalize" && m.OwningType.IsObject);
            Assert.Contains(vtable, m => m.Name == "ToString" && m.OwningType.IsObject);
        }

        [Fact]
        public void TestUnoverriddenSyntheticEqualsAndGetHashCode()
        {
            //
            // Tests that the synthetic implementation on a base class is propagated to
            // derived classes.
            //

            MetadataType baseType = _testModule.GetType("SyntheticVirtualOverride", "ClassWithInjectedEqualsAndGetHashCode");
            MetadataType t = _testModule.GetType("SyntheticVirtualOverride", "ClassNotOverridingEqualsAndGetHashCode");

            List<MethodDesc> virtualSlots = new List<MethodDesc>(t.EnumAllVirtualSlots());
            Assert.All(virtualSlots, s => { Assert.True(s.OwningType.IsObject); });
            Assert.Equal(4, virtualSlots.Count);

            List<MethodDesc> vtable = virtualSlots.Select(s => t.FindVirtualFunctionTargetMethodOnObjectType(s)).ToList();

            Assert.Contains(vtable, m => m.Name == "Equals" && m.OwningType == baseType);
            Assert.Contains(vtable, m => m.Name == "GetHashCode" && m.OwningType == baseType);
            Assert.Contains(vtable, m => m.Name == "Finalize" && m.OwningType.IsObject);
            Assert.Contains(vtable, m => m.Name == "ToString" && m.OwningType.IsObject);
        }

        [Fact]
        public void TestOverriddenSyntheticEqualsAndGetHashCode()
        {
            //
            // Tests that the synthetic implementation on a base class can be overridden by
            // derived classes.
            //

            MetadataType baseType = _testModule.GetType("SyntheticVirtualOverride", "ClassWithInjectedEqualsAndGetHashCode");
            MetadataType t = _testModule.GetType("SyntheticVirtualOverride", "ClassOverridingEqualsAndGetHashCode");

            List<MethodDesc> virtualSlots = new List<MethodDesc>(t.EnumAllVirtualSlots());
            Assert.All(virtualSlots, s => { Assert.True(s.OwningType.IsObject); });
            Assert.Equal(4, virtualSlots.Count);

            List<MethodDesc> vtable = virtualSlots.Select(s => t.FindVirtualFunctionTargetMethodOnObjectType(s)).ToList();

            Assert.Contains(vtable, m => m.Name == "Equals" && m.OwningType == t);
            Assert.Contains(vtable, m => m.Name == "GetHashCode" && m.OwningType == t);
            Assert.Contains(vtable, m => m.Name == "Finalize" && m.OwningType.IsObject);
            Assert.Contains(vtable, m => m.Name == "ToString" && m.OwningType.IsObject);
        }

        private class SyntheticVirtualOverrideTypeSystemContext : TestTypeSystemContext
        {
            private Dictionary<TypeDesc, MethodDesc> _getHashCodeMethods = new Dictionary<TypeDesc, MethodDesc>();
            private Dictionary<TypeDesc, MethodDesc> _equalsMethods = new Dictionary<TypeDesc, MethodDesc>();

            public SyntheticVirtualOverrideTypeSystemContext()
                : base(TargetArchitecture.Unknown)
            {
            }

            private MethodDesc GetGetHashCodeMethod(TypeDesc type)
            {
                MethodDesc result;
                if (!_getHashCodeMethods.TryGetValue(type, out result))
                {
                    result = new SyntheticMethod(type, "GetHashCode",
                        new MethodSignature(0, 0, type.Context.GetWellKnownType(WellKnownType.Int32), Array.Empty<TypeDesc>()));
                    _getHashCodeMethods.Add(type, result);
                }
                return result;
            }

            private MethodDesc GetEqualsMethod(TypeDesc type)
            {
                MethodDesc result;
                if (!_equalsMethods.TryGetValue(type, out result))
                {
                    result = new SyntheticMethod(type, "Equals",
                        new MethodSignature(0, 0, type.Context.GetWellKnownType(WellKnownType.Boolean),
                        new[] { type.Context.GetWellKnownType(WellKnownType.Object) }));
                    _equalsMethods.Add(type, result);
                }
                return result;
            }

            protected override IEnumerable<MethodDesc> GetAllMethods(TypeDesc type)
            {
                MetadataType mdType = type as MetadataType;

                if (mdType.Name == "StructWithNoEqualsAndGetHashCode"
                    || mdType.Name == "ClassWithInjectedEqualsAndGetHashCode")
                {
                    yield return GetEqualsMethod(type);
                    yield return GetGetHashCodeMethod(type);
                }

                foreach (var m in mdType.GetMethods())
                    yield return m;
            }

            protected override IEnumerable<MethodDesc> GetAllVirtualMethods(TypeDesc type)
            {
                MetadataType mdType = type as MetadataType;

                if (mdType.Name == "StructWithNoEqualsAndGetHashCode"
                    || mdType.Name == "ClassWithInjectedEqualsAndGetHashCode")
                {
                    yield return GetEqualsMethod(type);
                    yield return GetGetHashCodeMethod(type);
                }

                foreach (var m in mdType.GetVirtualMethods())
                    yield return m;
            }
        }

        private partial class SyntheticMethod : MethodDesc
        {
            private TypeDesc _owningType;
            private MethodSignature _signature;
            private string _name;

            public SyntheticMethod(TypeDesc owningType, string name, MethodSignature signature)
            {
                _owningType = owningType;
                _signature = signature;
                _name = name;
            }

            protected override int ClassCode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                throw new NotImplementedException();
            }

            public override bool IsVirtual
            {
                get
                {
                    return true;
                }
            }

            public override TypeSystemContext Context
            {
                get
                {
                    return _owningType.Context;
                }
            }

            public override string Name
            {
                get
                {
                    return _name;
                }
            }

            public override TypeDesc OwningType
            {
                get
                {
                    return _owningType;
                }
            }

            public override MethodSignature Signature
            {
                get
                {
                    return _signature;
                }
            }

            public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
            {
                return false;
            }
        }
    }
}
