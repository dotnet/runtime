// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class ReferenceHandlerTests_IgnoreCycles_Metadata : ReferenceHandlerTests_IgnoreCycles
    {
        public ReferenceHandlerTests_IgnoreCycles_Metadata()
            : base(new StringSerializerWrapper(ReferenceHandlerTests_IgnoreCyclesContext_Metadata.Default, (options) => new ReferenceHandlerTests_IgnoreCyclesContext_Metadata(options)),
                  new StreamSerializerWrapper(ReferenceHandlerTests_IgnoreCyclesContext_Metadata.Default, (options) => new ReferenceHandlerTests_IgnoreCyclesContext_Metadata(options)))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(GenericIDictionaryWrapper<string, object>))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(IList<object>))]
        [JsonSerializable(typeof(GenericIListWrapper<object>))]
        [JsonSerializable(typeof(GenericISetWrapper<object>))]
        [JsonSerializable(typeof(GenericICollectionWrapper<object>))]
        [JsonSerializable(typeof(NodeWithExtensionData))]
        [JsonSerializable(typeof(RecursiveDictionary))]
        [JsonSerializable(typeof(RecursiveList))]
        [JsonSerializable(typeof(NodeWithObjectProperty))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(ReadOnlyDictionary<string, object>))]
        [JsonSerializable(typeof(WrapperForIDictionary))]
        [JsonSerializable(typeof(IDictionary<string, object>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<int>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<Employee>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<List<int>>))]
        [JsonSerializable(typeof(object[]))]
        [JsonSerializable(typeof(ReferenceHandlerTests.IBoxedStructWithObjectProperty))]
        [JsonSerializable(typeof(ValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(ValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(NodeWithNodeProperty))]
        [JsonSerializable(typeof(NodeWithObjectProperty))]
        [JsonSerializable(typeof(EmptyClassWithExtensionProperty))]
        [JsonSerializable(typeof(Person))]
        [JsonSerializable(typeof(ICollection<object>))]
        [JsonSerializable(typeof(Stack<object>))]
        [JsonSerializable(typeof(Queue<object>))]
        [JsonSerializable(typeof(ConcurrentStack<object>))]
        [JsonSerializable(typeof(ConcurrentQueue<object>))]
        [JsonSerializable(typeof(Stack))]
        [JsonSerializable(typeof(Queue))]
        [JsonSerializable(typeof(IValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(ValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(IValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(ValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(EmptyClass))]
        [JsonSerializable(typeof(EmptyStruct))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<object>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<NodeWithNodeProperty>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<NodeWithObjectProperty>))]
        [JsonSerializable(typeof(List<Person>))]
        [JsonSerializable(typeof(RecursiveValue))]
        [JsonSerializable(typeof(List<RecursiveValue>))]
        [JsonSerializable(typeof(PersonHolder))]
        [JsonSerializable(typeof(BoxedPersonHolder))]
        [JsonSerializable(typeof(TreeNode<EmptyClass>))]
        [JsonSerializable(typeof(TreeNode<EmptyStruct>))]
        [JsonSerializable(typeof(TreeNode<object>))]
        [JsonSerializable(typeof(TreeNode<Dictionary<string, object>>))]
        [JsonSerializable(typeof(TreeNode<List<string>>))]
        [JsonSerializable(typeof(TreeNode<object>))]
        [JsonSerializable(typeof(int))]
        internal sealed partial class ReferenceHandlerTests_IgnoreCyclesContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class ReferenceHandlerTests_IgnoreCycles_Default : ReferenceHandlerTests_IgnoreCycles
    {
        public ReferenceHandlerTests_IgnoreCycles_Default()
            : base(new StringSerializerWrapper(ReferenceHandlerTests_IgnoreCyclesContext_Default.Default, (options) => new ReferenceHandlerTests_IgnoreCyclesContext_Default(options)),
                  new StreamSerializerWrapper(ReferenceHandlerTests_IgnoreCyclesContext_Default.Default, (options) => new ReferenceHandlerTests_IgnoreCyclesContext_Default(options)))
        {
        }

        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(GenericIDictionaryWrapper<string, object>))]
        [JsonSerializable(typeof(List<object>))]
        [JsonSerializable(typeof(IList<object>))]
        [JsonSerializable(typeof(GenericIListWrapper<object>))]
        [JsonSerializable(typeof(GenericISetWrapper<object>))]
        [JsonSerializable(typeof(GenericICollectionWrapper<object>))]
        [JsonSerializable(typeof(NodeWithExtensionData))]
        [JsonSerializable(typeof(RecursiveDictionary))]
        [JsonSerializable(typeof(RecursiveList))]
        [JsonSerializable(typeof(NodeWithObjectProperty))]
        [JsonSerializable(typeof(Dictionary<string, object>))]
        [JsonSerializable(typeof(ReadOnlyDictionary<string, object>))]
        [JsonSerializable(typeof(WrapperForIDictionary))]
        [JsonSerializable(typeof(IDictionary<string, object>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<int>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<Employee>))]
        [JsonSerializable(typeof(ReferenceHandlerTests.ClassWithZeroLengthProperty<List<int>>))]
        [JsonSerializable(typeof(object[]))]
        [JsonSerializable(typeof(ReferenceHandlerTests.IBoxedStructWithObjectProperty))]
        [JsonSerializable(typeof(ValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(ValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(NodeWithNodeProperty))]
        [JsonSerializable(typeof(NodeWithObjectProperty))]
        [JsonSerializable(typeof(EmptyClassWithExtensionProperty))]
        [JsonSerializable(typeof(Person))]
        [JsonSerializable(typeof(ICollection<object>))]
        [JsonSerializable(typeof(Stack<object>))]
        [JsonSerializable(typeof(Queue<object>))]
        [JsonSerializable(typeof(ConcurrentStack<object>))]
        [JsonSerializable(typeof(ConcurrentQueue<object>))]
        [JsonSerializable(typeof(Stack))]
        [JsonSerializable(typeof(Queue))]
        [JsonSerializable(typeof(IValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(ValueNodeWithIValueNodeProperty))]
        [JsonSerializable(typeof(IValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(ValueNodeWithObjectProperty))]
        [JsonSerializable(typeof(EmptyClass))]
        [JsonSerializable(typeof(EmptyStruct))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<object>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<NodeWithNodeProperty>))]
        [JsonSerializable(typeof(ClassWithGenericProperty<NodeWithObjectProperty>))]
        [JsonSerializable(typeof(List<Person>))]
        [JsonSerializable(typeof(RecursiveValue))]
        [JsonSerializable(typeof(List<RecursiveValue>))]
        [JsonSerializable(typeof(PersonHolder))]
        [JsonSerializable(typeof(BoxedPersonHolder))]
        [JsonSerializable(typeof(TreeNode<EmptyClass>))]
        [JsonSerializable(typeof(TreeNode<EmptyStruct>))]
        [JsonSerializable(typeof(TreeNode<object>))]
        [JsonSerializable(typeof(TreeNode<Dictionary<string, object>>))]
        [JsonSerializable(typeof(TreeNode<List<string>>))]
        [JsonSerializable(typeof(TreeNode<object>))]
        [JsonSerializable(typeof(int))]
        internal sealed partial class ReferenceHandlerTests_IgnoreCyclesContext_Default : JsonSerializerContext
        {
        }
    }
}
