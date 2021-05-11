// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Composition.Runtime.Tests
{
    public class CompositionContractTests
    {
        [Theory]
        [InlineData(typeof(int))]
        public void Ctor_ContractType(Type contractType)
        {
            var contract = new CompositionContract(contractType);
            Assert.Equal(contractType, contract.ContractType);
            Assert.Null(contract.ContractName);
            Assert.Null(contract.MetadataConstraints);
        }

        [Theory]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(object), "contractName")]
        public void Ctor_ContractType_ContractName(Type contractType, string contractName)
        {
            var contract = new CompositionContract(contractType, contractName);
            Assert.Equal(contractType, contract.ContractType);
            Assert.Equal(contractName, contract.ContractName);
            Assert.Null(contract.MetadataConstraints);
        }

        public static IEnumerable<object[]> Ctor_ContractType_ContractName_MetadataConstraints_TestData()
        {
            yield return new object[] { typeof(int), null, null };
            yield return new object[] { typeof(object), "contractName", new Dictionary<string, object> { { "key", "value" } } };
        }

        [Theory]
        [MemberData(nameof(Ctor_ContractType_ContractName_MetadataConstraints_TestData))]
        public void Ctor_ContractType_MetadataConstraints(Type contractType, string contractName, IDictionary<string, object> metadataConstraints)
        {
            var contract = new CompositionContract(contractType, contractName, metadataConstraints);
            Assert.Equal(contractType, contract.ContractType);
            Assert.Equal(contractName, contract.ContractName);
            Assert.Equal(metadataConstraints, contract.MetadataConstraints);
        }

        [Fact]
        public void Ctor_NullContractType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("contractType", () => new CompositionContract(null));
            AssertExtensions.Throws<ArgumentNullException>("contractType", () => new CompositionContract(null, "contractName"));
            AssertExtensions.Throws<ArgumentNullException>("contractType", () => new CompositionContract(null, "contractName", new Dictionary<string, object>()));
        }

        [Fact]
        public void Ctor_EmptyMetadataConstraints_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("metadataConstraints", () => new CompositionContract(typeof(string), "contractName", new Dictionary<string, object>()));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new CompositionContract(typeof(int)), new CompositionContract(typeof(int)), true };
            yield return new object[] { new CompositionContract(typeof(int)), new CompositionContract(typeof(string)), false };
            yield return new object[] { new CompositionContract(typeof(int)), new CompositionContract(typeof(int), "contractName"), false };
            yield return new object[] { new CompositionContract(typeof(int)), new CompositionContract(typeof(int), null, new Dictionary<string, object> { { "key", "value" } }), false };

            yield return new object[] { new CompositionContract(typeof(int), "contractName"), new CompositionContract(typeof(int), "contractName"), true };
            yield return new object[] { new CompositionContract(typeof(int), "contractName"), new CompositionContract(typeof(int), "ContractName"), false };
            yield return new object[] { new CompositionContract(typeof(int), "contractName"), new CompositionContract(typeof(int)), false };
            yield return new object[] { new CompositionContract(typeof(int)), new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }), false };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                true
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", 1 } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", 1 } }),
                true
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[] { "1", null } } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new object[] { "1", null } } }),
                true
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[] { "1", null } } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new object[] { "1", new object() } } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" }, { "key2", "value2" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" }, { "key2", "value2" }  }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key2", "value" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value2" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[0] } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[1] } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[0] } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new string[0] } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new object() } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", "value" } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } }),
                false
            };

            yield return new object[]
            {
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", new object[0] } }),
                new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } }),
                false
            };

            if (!PlatformDetection.IsNetFramework)
            {
                yield return new object[]
                {
                    new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } }),
                    new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } }),
                    true
                };
            }

            yield return new object[] { new CompositionContract(typeof(int)), new object(), false };
            yield return new object[] { new CompositionContract(typeof(int)), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Object_ReturnsExpected(CompositionContract contract, object other, bool expected)
        {
            Assert.Equal(expected, contract.Equals(other));
            Assert.Equal(contract.GetHashCode(), contract.GetHashCode());
        }

        [Fact]
        public void ChangeType_ValidType_Success()
        {
            var dictionary = new Dictionary<string, object> { { "key", "value" } };
            var contract = new CompositionContract(typeof(int), "contractName", dictionary);
            CompositionContract newContract = contract.ChangeType(typeof(string));

            Assert.Equal(typeof(int), contract.ContractType);
            Assert.Equal(typeof(string), newContract.ContractType);
            Assert.Equal("contractName", newContract.ContractName);
            Assert.Same(dictionary, newContract.MetadataConstraints);
        }

        [Fact]
        public void ChangeType_NullNewContractType_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("newContractType", () => contract.ChangeType(null));
        }

        [Fact]
        public void TryUnwrapMetadataConstraint_NullConstraints_ReturnsFalse()
        {
            var contract = new CompositionContract(typeof(int));
            Assert.False(contract.TryUnwrapMetadataConstraint("constraintName", out int constraintValue, out CompositionContract remainingContract));
            Assert.Equal(0, constraintValue);
            Assert.Null(remainingContract);
        }

        [Fact]
        public void TryUnwrapMetadataConstraint_NoSuchConstraintName_ReturnsFalse()
        {
            var contract = new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "constraint", 1 } });
            Assert.False(contract.TryUnwrapMetadataConstraint("constraintName", out int constraintValue, out CompositionContract remainingContract));
            Assert.Equal(0, constraintValue);
            Assert.Null(remainingContract);
        }

        [Fact]
        public void TryUnwrapMetadataConstraint_IncorrectConstraintNameType_ReturnsFalse()
        {
            var contract = new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "constraintName", "value" } });
            Assert.False(contract.TryUnwrapMetadataConstraint("constraintName", out int constraintValue, out CompositionContract remainingContract));
            Assert.Equal(0, constraintValue);
            Assert.Null(remainingContract);
        }

        [Fact]
        public void TryUnwrapMetadataConstraint_UnwrapAllConstraints_ReturnsTrue()
        {
            var originalContract = new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "constraintName1", 1 }, { "constraintName2", 2 } });
            Assert.True(originalContract.TryUnwrapMetadataConstraint("constraintName1", out int constraintValue1, out CompositionContract remainingContract1));
            Assert.Equal(1, constraintValue1);

            Assert.Equal(originalContract.ContractType, remainingContract1.ContractType);
            Assert.Equal(originalContract.ContractName, remainingContract1.ContractName);
            Assert.Equal(new Dictionary<string, object> { { "constraintName2", 2 } }, remainingContract1.MetadataConstraints);
            Assert.NotEqual(originalContract.MetadataConstraints, remainingContract1.MetadataConstraints);

            Assert.True(remainingContract1.TryUnwrapMetadataConstraint("constraintName2", out int constraintValue2, out CompositionContract remainingContract2));
            Assert.Equal(2, constraintValue2);

            Assert.Equal(originalContract.ContractType, remainingContract2.ContractType);
            Assert.Equal(originalContract.ContractName, remainingContract2.ContractName);
            Assert.Null(remainingContract2.MetadataConstraints);
            Assert.NotEqual(originalContract.MetadataConstraints, remainingContract2.MetadataConstraints);
        }

        [Fact]
        public void TryUnwrapMetadataConstraint_NullContractName_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int));
            AssertExtensions.Throws<ArgumentNullException>("constraintName", () => contract.TryUnwrapMetadataConstraint(null, out int unusedValue, out CompositionContract unusedContract));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new CompositionContract(typeof(int)), "Int32" };
            yield return new object[] { new CompositionContract(typeof(int), "contractName"), "Int32 \"contractName\"" };
            yield return new object[] { new CompositionContract(typeof(List<>), "contractName", new Dictionary<string, object> { { "key1", "value" }, { "key2", 2 } }), "List`1 \"contractName\" { key1 = \"value\", key2 = 2 }" };
            yield return new object[] { new CompositionContract(typeof(List<string>), "contractName", new Dictionary<string, object> { { "key1", "value" }, { "key2", 2 } }), "List<String> \"contractName\" { key1 = \"value\", key2 = 2 }" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50918", TestPlatforms.Android)]
        public void ToString_Get_ReturnsExpected(CompositionContract contract, string expected)
        {
            Assert.Equal(expected, contract.ToString());
        }

        [Fact]
        public void ToString_NullValueInDictionary_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(typeof(int), "contractName", new Dictionary<string, object> { { "key", null } });
            AssertExtensions.Throws<ArgumentNullException>("value", () => contract.ToString());
        }

        [Fact]
        public void ToString_NullTypeInGenericTypeArguments_ThrowsArgumentNullException()
        {
            var contract = new CompositionContract(new SubType() { GenericTypeArgumentsOverride = new Type[] { null } });
            AssertExtensions.Throws<ArgumentNullException>("type", () => contract.ToString());
        }

        private class SubType : Type
        {
            public override Assembly Assembly => throw new NotImplementedException();
            public override string AssemblyQualifiedName => throw new NotImplementedException();
            public override Type BaseType => throw new NotImplementedException();
            public override string FullName => throw new NotImplementedException();
            public override Guid GUID => throw new NotImplementedException();
            public override Module Module => throw new NotImplementedException();
            public override string Namespace => throw new NotImplementedException();
            public override Type UnderlyingSystemType => throw new NotImplementedException();

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
            public override Type GetElementType() => throw new NotImplementedException();
            public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
            public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override FieldInfo GetField(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
            public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override Type GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();
            public override Type[] GetInterfaces() => throw new NotImplementedException();
            public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override Type GetNestedType(string name, BindingFlags bindingAttr) => throw new NotImplementedException();
            public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotImplementedException();
            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw new NotImplementedException();
            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
            protected override TypeAttributes GetAttributeFlagsImpl() => throw new NotImplementedException();
            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();
            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw new NotImplementedException();

            protected override bool HasElementTypeImpl() => throw new NotImplementedException();
            protected override bool IsArrayImpl() => throw new NotImplementedException();
            protected override bool IsByRefImpl() => throw new NotImplementedException();
            protected override bool IsCOMObjectImpl() => throw new NotImplementedException();
            protected override bool IsPointerImpl() => throw new NotImplementedException();
            protected override bool IsPrimitiveImpl() => throw new NotImplementedException();

            public override string Name => "Name`1";
            public override bool IsConstructedGenericType => true;
            public Type[] GenericTypeArgumentsOverride { get; set; }
            public override Type[] GenericTypeArguments => GenericTypeArgumentsOverride;
        }
    }
}
