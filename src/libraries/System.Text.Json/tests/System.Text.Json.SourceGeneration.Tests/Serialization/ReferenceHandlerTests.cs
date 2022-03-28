// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed class ReferenceHandlerTests_Metadata_String : ReferenceHandlerTests_Metadata
    {
        public ReferenceHandlerTests_Metadata_String()
            : base(new StringSerializerWrapper(ReferenceHandlerTestsContext_Metadata.Default, (options) => new ReferenceHandlerTestsContext_Metadata(options)))
        {
        }
    }

    public sealed class ReferenceHandlerTests_Metadata_AsyncStream : ReferenceHandlerTests_Metadata
    {
        public ReferenceHandlerTests_Metadata_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(ReferenceHandlerTestsContext_Metadata.Default, (options) => new ReferenceHandlerTestsContext_Metadata(options)))
        {
        }
    }

    public abstract partial class ReferenceHandlerTests_Metadata : ReferenceHandlerTests
    {
        public ReferenceHandlerTests_Metadata(JsonSerializerWrapper serializer)
            : base(serializer)
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(KeyValuePair<string, string>))]
        [JsonSerializable(typeof(ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(DictionaryWithGenericCycle))]
        [JsonSerializable(typeof(Dictionary<string, Employee>))]
        [JsonSerializable(typeof(DictionaryWithGenericCycleWithinList))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(ListWithGenericCycle))]
        [JsonSerializable(typeof(List<Employee>))]
        [JsonSerializable(typeof(ListWithGenericCycleWithinDictionary))]
        [JsonSerializable(typeof(ImmutableArray<PersonReference>))]
        [JsonSerializable(typeof(ClassWithObjectProperty))]
        [JsonSerializable(typeof(ClassWithListOfObjectProperty))]
        [JsonSerializable(typeof(EmployeeWithContacts))]
        [JsonSerializable(typeof(ClassWithSubsequentListProperties))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<int>))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<Employee>))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<List<int>>))]
        [JsonSerializable(typeof(Dictionary<string, string>))]
        [JsonSerializable(typeof(Dictionary<string, EmployeeWithContacts>))]
        [JsonSerializable(typeof(Dictionary<string, List<int>>))]
        [JsonSerializable(typeof(Dictionary<string, Dictionary<string, int>>))]
        [JsonSerializable(typeof(List<int>))]
        [JsonSerializable(typeof(Employee[]))]
        [JsonSerializable(typeof(List<List<int>>))]
        [JsonSerializable(typeof(ListWrapper))]
        [JsonSerializable(typeof(List<List<Employee>>))]
        [JsonSerializable(typeof(List<EmployeeStruct>))]
        [JsonSerializable(typeof(ImmutableList<EmployeeWithImmutable>))]
        [JsonSerializable(typeof(EmployeeWithImmutable[]))]
        [JsonSerializable(typeof(ImmutableDictionary<string, EmployeeWithImmutable>))]
        [JsonSerializable(typeof(EmployeeWithImmutable))]
        [JsonSerializable(typeof(ImmutableDictionary<string, Employee>))]
        [JsonSerializable(typeof(Order))]
        [JsonSerializable(typeof(List<Order>))]
        [JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
        [JsonSerializable(typeof(ClassWithTwoListProperties))]
        [JsonSerializable(typeof(EmployeeExtensionData))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(BaseAndDerivedWrapper))]
        [JsonSerializable(typeof(List<ClassIncorrectHashCode>))]
        [JsonSerializable(typeof(ClassWithComplexObjects))]
        [JsonSerializable(typeof(object[]))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(SimpleTestStruct))]
        [JsonSerializable(typeof(SimpleTestStructWithFields))]
        [JsonSerializable(typeof(SimpleTestClass))]
        [JsonSerializable(typeof(SimpleTestClassWithFields))]
        [JsonSerializable(typeof(SimpleTestClassWithNullables))]
        [JsonSerializable(typeof(SimpleTestClassWithNulls))]
        [JsonSerializable(typeof(SimpleTestClassWithSimpleObject))]
        [JsonSerializable(typeof(SimpleTestClassWithObjectArrays))]
        [JsonSerializable(typeof(BasicPerson))]
        [JsonSerializable(typeof(BasicCompany))]
        [JsonSerializable(typeof(TestClassWithNestedObjectInner))]
        [JsonSerializable(typeof(TestClassWithNestedObjectOuter))]
        [JsonSerializable(typeof(TestClassWithObjectArray))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerable))]
        [JsonSerializable(typeof(TestClassWithObjectIList))]
        [JsonSerializable(typeof(TestClassWithObjectICollection))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerableT))]
        [JsonSerializable(typeof(TestClassWithObjectIListT))]
        [JsonSerializable(typeof(TestClassWithObjectICollectionT))]
        [JsonSerializable(typeof(TestClassWithObjectIReadOnlyCollectionT))]
        [JsonSerializable(typeof(TestClassWithObjectIReadOnlyListT))]
        [JsonSerializable(typeof(TestClassWithObjectISetT))]
        [JsonSerializable(typeof(TestClassWithStringArray))]
        [JsonSerializable(typeof(TestClassWithGenericList))]
        [JsonSerializable(typeof(TestClassWithGenericIEnumerable))]
        [JsonSerializable(typeof(TestClassWithGenericIList))]
        [JsonSerializable(typeof(TestClassWithGenericICollection))]
        [JsonSerializable(typeof(TestClassWithGenericIEnumerableT))]
        [JsonSerializable(typeof(TestClassWithGenericIListT))]
        [JsonSerializable(typeof(TestClassWithGenericICollectionT))]
        [JsonSerializable(typeof(TestClassWithGenericIReadOnlyCollectionT))]
        [JsonSerializable(typeof(TestClassWithGenericIReadOnlyListT))]
        [JsonSerializable(typeof(TestClassWithGenericISetT))]
        [JsonSerializable(typeof(TestClassWithStringToPrimitiveDictionary))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerableConstructibleTypes))]
        [JsonSerializable(typeof(TestClassWithObjectImmutableTypes))]
        [JsonSerializable(typeof(JsonElementClass))]
        [JsonSerializable(typeof(JsonElementArrayClass))]
        [JsonSerializable(typeof(ClassWithComplexObjects))]
        [JsonSerializable(typeof(IBoxedStructWithObjectProperty))]
        [JsonSerializable(typeof(StructWithObjectProperty))]
        [JsonSerializable(typeof(ImmutableArray<EmployeeStruct>))]
        [JsonSerializable(typeof(PersonReference))]
        [JsonSerializable(typeof(ClassWithListAndImmutableArray))]
        [JsonSerializable(typeof(ImmutableArray<List<int>>))]
        [JsonSerializable(typeof(List<List<int>>))]
        [JsonSerializable(typeof(List<ImmutableArray<int>>))]
        [JsonSerializable(typeof(List<ImmutableArray<int>>))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(StructCollection))]
        [JsonSerializable(typeof(ImmutableArray<int>))]
        internal sealed partial class ReferenceHandlerTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed class ReferenceHandlerTests_Default_String : ReferenceHandlerTests_Default
    {
        public ReferenceHandlerTests_Default_String()
            : base(new StringSerializerWrapper(ReferenceHandlerTestsContext_Default.Default, (options) => new ReferenceHandlerTestsContext_Default(options)))
        {
        }

        [Fact]
        public override async Task ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(a));
        }
    }

    public sealed class ReferenceHandlerTests_Default_AsyncStream : ReferenceHandlerTests_Default
    {
        public ReferenceHandlerTests_Default_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(ReferenceHandlerTestsContext_Default.Default, (options) => new ReferenceHandlerTestsContext_Default(options)))
        {
        }
    }

    public abstract partial class ReferenceHandlerTests_Default : ReferenceHandlerTests
    {
        public ReferenceHandlerTests_Default(JsonSerializerWrapper serializer)
            : base(serializer)
        {
        }

        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(KeyValuePair<string, string>))]
        [JsonSerializable(typeof(ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(DictionaryWithGenericCycle))]
        [JsonSerializable(typeof(Dictionary<string, Employee>))]
        [JsonSerializable(typeof(DictionaryWithGenericCycleWithinList))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(ListWithGenericCycle))]
        [JsonSerializable(typeof(List<Employee>))]
        [JsonSerializable(typeof(ListWithGenericCycleWithinDictionary))]
        [JsonSerializable(typeof(ImmutableArray<PersonReference>))]
        [JsonSerializable(typeof(ClassWithObjectProperty))]
        [JsonSerializable(typeof(ClassWithListOfObjectProperty))]
        [JsonSerializable(typeof(EmployeeWithContacts))]
        [JsonSerializable(typeof(ClassWithSubsequentListProperties))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<int>))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<Employee>))]
        [JsonSerializable(typeof(ClassWithZeroLengthProperty<List<int>>))]
        [JsonSerializable(typeof(Dictionary<string, string>))]
        [JsonSerializable(typeof(Dictionary<string, EmployeeWithContacts>))]
        [JsonSerializable(typeof(Dictionary<string, List<int>>))]
        [JsonSerializable(typeof(Dictionary<string, Dictionary<string, int>>))]
        [JsonSerializable(typeof(List<int>))]
        [JsonSerializable(typeof(Employee[]))]
        [JsonSerializable(typeof(List<List<int>>))]
        [JsonSerializable(typeof(ListWrapper))]
        [JsonSerializable(typeof(List<List<Employee>>))]
        [JsonSerializable(typeof(List<EmployeeStruct>))]
        [JsonSerializable(typeof(ImmutableList<EmployeeWithImmutable>))]
        [JsonSerializable(typeof(EmployeeWithImmutable[]))]
        [JsonSerializable(typeof(ImmutableDictionary<string, EmployeeWithImmutable>))]
        [JsonSerializable(typeof(EmployeeWithImmutable))]
        [JsonSerializable(typeof(ImmutableDictionary<string, Employee>))]
        [JsonSerializable(typeof(Order))]
        [JsonSerializable(typeof(List<Order>))]
        [JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
        [JsonSerializable(typeof(ClassWithTwoListProperties))]
        [JsonSerializable(typeof(EmployeeExtensionData))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(BaseAndDerivedWrapper))]
        [JsonSerializable(typeof(List<ClassIncorrectHashCode>))]
        [JsonSerializable(typeof(ClassWithComplexObjects))]
        [JsonSerializable(typeof(object[]))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(SimpleTestStruct))]
        [JsonSerializable(typeof(SimpleTestStructWithFields))]
        [JsonSerializable(typeof(SimpleTestClass))]
        [JsonSerializable(typeof(SimpleTestClassWithFields))]
        [JsonSerializable(typeof(SimpleTestClassWithNullables))]
        [JsonSerializable(typeof(SimpleTestClassWithNulls))]
        [JsonSerializable(typeof(SimpleTestClassWithSimpleObject))]
        [JsonSerializable(typeof(SimpleTestClassWithObjectArrays))]
        [JsonSerializable(typeof(BasicPerson))]
        [JsonSerializable(typeof(BasicCompany))]
        [JsonSerializable(typeof(TestClassWithNestedObjectInner))]
        [JsonSerializable(typeof(TestClassWithNestedObjectOuter))]
        [JsonSerializable(typeof(TestClassWithObjectArray))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerable))]
        [JsonSerializable(typeof(TestClassWithObjectIList))]
        [JsonSerializable(typeof(TestClassWithObjectICollection))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerableT))]
        [JsonSerializable(typeof(TestClassWithObjectIListT))]
        [JsonSerializable(typeof(TestClassWithObjectICollectionT))]
        [JsonSerializable(typeof(TestClassWithObjectIReadOnlyCollectionT))]
        [JsonSerializable(typeof(TestClassWithObjectIReadOnlyListT))]
        [JsonSerializable(typeof(TestClassWithObjectISetT))]
        [JsonSerializable(typeof(TestClassWithStringArray))]
        [JsonSerializable(typeof(TestClassWithGenericList))]
        [JsonSerializable(typeof(TestClassWithGenericIEnumerable))]
        [JsonSerializable(typeof(TestClassWithGenericIList))]
        [JsonSerializable(typeof(TestClassWithGenericICollection))]
        [JsonSerializable(typeof(TestClassWithGenericIEnumerableT))]
        [JsonSerializable(typeof(TestClassWithGenericIListT))]
        [JsonSerializable(typeof(TestClassWithGenericICollectionT))]
        [JsonSerializable(typeof(TestClassWithGenericIReadOnlyCollectionT))]
        [JsonSerializable(typeof(TestClassWithGenericIReadOnlyListT))]
        [JsonSerializable(typeof(TestClassWithGenericISetT))]
        [JsonSerializable(typeof(TestClassWithStringToPrimitiveDictionary))]
        [JsonSerializable(typeof(TestClassWithObjectIEnumerableConstructibleTypes))]
        [JsonSerializable(typeof(TestClassWithObjectImmutableTypes))]
        [JsonSerializable(typeof(JsonElementClass))]
        [JsonSerializable(typeof(JsonElementArrayClass))]
        [JsonSerializable(typeof(ClassWithComplexObjects))]
        [JsonSerializable(typeof(IBoxedStructWithObjectProperty))]
        [JsonSerializable(typeof(StructWithObjectProperty))]
        [JsonSerializable(typeof(ImmutableArray<EmployeeStruct>))]
        [JsonSerializable(typeof(PersonReference))]
        [JsonSerializable(typeof(ClassWithListAndImmutableArray))]
        [JsonSerializable(typeof(ImmutableArray<List<int>>))]
        [JsonSerializable(typeof(List<List<int>>))]
        [JsonSerializable(typeof(List<ImmutableArray<int>>))]
        [JsonSerializable(typeof(List<ImmutableArray<int>>))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(StructCollection))]
        [JsonSerializable(typeof(ImmutableArray<int>))]
        internal sealed partial class ReferenceHandlerTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
