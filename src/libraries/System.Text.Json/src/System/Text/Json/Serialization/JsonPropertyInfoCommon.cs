// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal abstract class JsonPropertyInfoCommon<TClass, TDeclaredProperty, TRuntimeProperty, TConverter> : JsonPropertyInfo
    {
        public Func<object, TDeclaredProperty> Get { get; private set; }
        public Action<object, TDeclaredProperty> Set { get; private set; }

        public JsonConverter<TConverter> Converter { get; internal set; }

        public override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            Type implementedPropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            base.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, implementedPropertyType, propertyInfo, elementType, converter, options);

            if (propertyInfo != null &&
                // We only want to get the getter and setter if we are going to use them.
                // If the declared type is not the property info type, then we are just
                // getting metadata on how best to (de)serialize derived types.
                declaredPropertyType == propertyInfo.PropertyType)
            {
                if (propertyInfo.GetMethod?.IsPublic == true)
                {
                    HasGetter = true;
                    Get = options.MemberAccessorStrategy.CreatePropertyGetter<TClass, TDeclaredProperty>(propertyInfo);
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    HasSetter = true;
                    Set = options.MemberAccessorStrategy.CreatePropertySetter<TClass, TDeclaredProperty>(propertyInfo);
                }
            }
            else
            {
                IsPropertyPolicy = true;
                HasGetter = true;
                HasSetter = true;
            }

            GetPolicies();
        }

        public override JsonConverter ConverterBase
        {
            get
            {
                return Converter;
            }
            set
            {
                Debug.Assert(Converter == null);
                Debug.Assert(value is JsonConverter<TConverter>);

                Converter = (JsonConverter<TConverter>)value;
            }
        }

        public override object GetValueAsObject(object obj)
        {
            if (IsPropertyPolicy)
            {
                return obj;
            }

            Debug.Assert(HasGetter);
            return Get(obj);
        }

        public override void SetValueAsObject(object obj, object value)
        {
            Debug.Assert(HasSetter);
            TDeclaredProperty typedValue = (TDeclaredProperty)value;

            if (typedValue != null || !IgnoreNullValues)
            {
                Set(obj, typedValue);
            }
        }

        public override IList CreateConverterList()
        {
            return new List<TDeclaredProperty>();
        }

        public override Type GetDictionaryConcreteType()
        {
            return typeof(Dictionary<string, TRuntimeProperty>);
        }

        public override Type GetConcreteType(Type parentType)
        {
            if (JsonClassInfo.IsDeserializedByAssigningFromList(parentType))
            {
                return typeof(List<TDeclaredProperty>);
            }
            else if (JsonClassInfo.IsSetInterface(parentType))
            {
                return typeof(HashSet<TDeclaredProperty>);
            }

            return parentType;
        }

        public override IEnumerable CreateDerivedEnumerableInstance(JsonPropertyInfo collectionPropertyInfo, IList sourceList, string jsonPath, JsonSerializerOptions options)
        {
            object instance = collectionPropertyInfo.DeclaredTypeClassInfo.CreateObject();

            if (instance is IList instanceOfIList && !instanceOfIList.IsReadOnly)
            {
                foreach (object item in sourceList)
                {
                    instanceOfIList.Add(item);
                }
                return instanceOfIList;
            }
            else if (instance is ICollection<TRuntimeProperty> instanceOfICollection && !instanceOfICollection.IsReadOnly)
            {
                foreach (TRuntimeProperty item in sourceList)
                {
                    instanceOfICollection.Add(item);
                }
                return instanceOfICollection;
            }
            else if (instance is Stack<TRuntimeProperty> instanceOfStack)
            {
                foreach (TRuntimeProperty item in sourceList)
                {
                    instanceOfStack.Push(item);
                }
                return instanceOfStack;
            }
            else if (instance is Queue<TRuntimeProperty> instanceOfQueue)
            {
                foreach (TRuntimeProperty item in sourceList)
                {
                    instanceOfQueue.Enqueue(item);
                }
                return instanceOfQueue;
            }

            // TODO: Use reflection to support types implementing Stack or Queue.

            throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                collectionPropertyInfo.DeclaredPropertyType,
                collectionPropertyInfo.ParentClassType,
                collectionPropertyInfo.PropertyInfo);
        }

        public override object CreateDerivedDictionaryInstance(JsonPropertyInfo collectionPropertyInfo, IDictionary sourceDictionary, string jsonPath, JsonSerializerOptions options)
        {
            object instance = collectionPropertyInfo.DeclaredTypeClassInfo.CreateObject();

            if (instance is IDictionary instanceOfIDictionary && !instanceOfIDictionary.IsReadOnly)
            {
                foreach (DictionaryEntry entry in sourceDictionary)
                {
                    instanceOfIDictionary.Add((string)entry.Key, entry.Value);
                }
                return instanceOfIDictionary;
            }
            else if (instance is IDictionary<string, TRuntimeProperty> instanceOfGenericIDictionary && !instanceOfGenericIDictionary.IsReadOnly)
            {
                foreach (DictionaryEntry entry in sourceDictionary)
                {
                    instanceOfGenericIDictionary.Add((string)entry.Key, (TRuntimeProperty)entry.Value);
                }
                return instanceOfGenericIDictionary;
            }

            // TODO: Use reflection to support types implementing SortedList and maybe immutable dictionaries.

            // Types implementing SortedList and immutable dictionaries will fail here.
            throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(
                collectionPropertyInfo.DeclaredPropertyType,
                collectionPropertyInfo.ParentClassType,
                collectionPropertyInfo.PropertyInfo);
        }

        public override IEnumerable CreateIEnumerableInstance(Type parentType, IList sourceList, string jsonPath, JsonSerializerOptions options)
        {
            if (parentType.IsGenericType)
            {
                Type genericTypeDefinition = parentType.GetGenericTypeDefinition();
                IEnumerable<TDeclaredProperty> items = CreateGenericTDeclaredPropertyIEnumerable(sourceList);

                if (genericTypeDefinition == typeof(Stack<>))
                {
                    return new Stack<TDeclaredProperty>(items);
                }
                else if (genericTypeDefinition == typeof(Queue<>))
                {
                    return new Queue<TDeclaredProperty>(items);
                }
                else if (genericTypeDefinition == typeof(HashSet<>))
                {
                    return new HashSet<TDeclaredProperty>(items);
                }
                else if (genericTypeDefinition == typeof(LinkedList<>))
                {
                    return new LinkedList<TDeclaredProperty>(items);
                }
                else if (genericTypeDefinition == typeof(SortedSet<>))
                {
                    return new SortedSet<TDeclaredProperty>(items);
                }

                return (IEnumerable)Activator.CreateInstance(parentType, items);
            }
            else
            {
                if (parentType == typeof(ArrayList))
                {
                    return new ArrayList(sourceList);
                }
                // Stack and Queue go into this condition, until we support with reflection.
                else
                {
                    return (IEnumerable)Activator.CreateInstance(parentType, sourceList);
                }
            }
        }

        public override IDictionary CreateIDictionaryInstance(Type parentType, IDictionary sourceDictionary, string jsonPath, JsonSerializerOptions options)
        {
            if (parentType.FullName == JsonClassInfo.HashtableTypeName)
            {
                return new Hashtable(sourceDictionary);
            }
            // SortedList goes into this condition, unless we add a ref to System.Collections.NonGeneric.
            else
            {
                return (IDictionary)Activator.CreateInstance(parentType, sourceDictionary);
            }
        }

        // Creates an IEnumerable<TRuntimePropertyType> and populates it with the items in the
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TRuntimePropertyType> method to create and return the desired immutable collection type.
        public override IEnumerable CreateImmutableCollectionInstance(Type collectionType, string delegateKey, IList sourceList, string jsonPath, JsonSerializerOptions options)
        {
            IEnumerable collection = null;

            if (!options.TryGetCreateRangeDelegate(delegateKey, out ImmutableCollectionCreator creator) ||
                !creator.CreateImmutableEnumerable(sourceList, out collection))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, jsonPath);
            }

            return collection;
        }

        // Creates an IEnumerable<TRuntimePropertyType> and populates it with the items in the
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TRuntimePropertyType> method to create and return the desired immutable collection type.
        public override IDictionary CreateImmutableDictionaryInstance(Type collectionType, string delegateKey, IDictionary sourceDictionary, string jsonPath, JsonSerializerOptions options)
        {
            IDictionary collection = null;

            if (!options.TryGetCreateRangeDelegate(delegateKey, out ImmutableCollectionCreator creator) ||
                !creator.CreateImmutableDictionary(sourceDictionary, out collection))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, jsonPath);
            }

            return collection;
        }

        private IEnumerable<TRuntimeProperty> CreateGenericTRuntimePropertyIEnumerable(IList sourceList)
        {
            foreach (object item in sourceList)
            {
                yield return (TRuntimeProperty)item;
            }
        }

        private IEnumerable<TDeclaredProperty> CreateGenericTDeclaredPropertyIEnumerable(IList sourceList)
        {
            foreach (object item in sourceList)
            {
                yield return (TDeclaredProperty)item;
            }
        }

        private IEnumerable<KeyValuePair<string, TRuntimeProperty>> CreateGenericIEnumerableFromDictionary(IDictionary sourceDictionary)
        {
            foreach (DictionaryEntry item in sourceDictionary)
            {
                yield return new KeyValuePair<string, TRuntimeProperty>((string)item.Key, (TRuntimeProperty)item.Value);
            }
        }
    }
}
