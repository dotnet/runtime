// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal abstract class JsonPropertyInfoCommon<TClass, TDeclaredProperty, TRuntimeProperty> : JsonPropertyInfo
    {
        public Func<object, TDeclaredProperty> Get { get; private set; }
        public Action<object, TDeclaredProperty> Set { get; private set; }

        public JsonValueConverter<TRuntimeProperty> ValueConverter { get; internal set; }

        public override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonSerializerOptions options)
        {
            base.Initialize(parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, elementType, options);

            if (propertyInfo != null)
            {
                if (propertyInfo.GetMethod?.IsPublic == true)
                {
                    HasGetter = true;
                    Get = MemberAccessor.CreatePropertyGetter<TClass, TDeclaredProperty>(propertyInfo);
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    HasSetter = true;
                    Set = MemberAccessor.CreatePropertySetter<TClass, TDeclaredProperty>(propertyInfo);
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

        public override void GetPolicies()
        {
            ValueConverter = DefaultConverters<TRuntimeProperty>.s_converter;
            base.GetPolicies();
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
                // Stack and Queue go into this condition, unless we add a ref to System.Collections.NonGeneric.
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
            if (!options.TryGetCreateRangeDelegate(delegateKey, out object createRangeDelegateObj))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, jsonPath);
            }

            JsonSerializerOptions.ImmutableCreateRangeDelegate<TRuntimeProperty> createRangeDelegate = (
                (JsonSerializerOptions.ImmutableCreateRangeDelegate<TRuntimeProperty>)createRangeDelegateObj);

            return (IEnumerable)createRangeDelegate.Invoke(CreateGenericTRuntimePropertyIEnumerable(sourceList));
        }

        // Creates an IEnumerable<TRuntimePropertyType> and populates it with the items in the
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TRuntimePropertyType> method to create and return the desired immutable collection type.
        public override IDictionary CreateImmutableDictionaryInstance(Type collectionType, string delegateKey, IDictionary sourceDictionary, string jsonPath, JsonSerializerOptions options)
        {
            if (!options.TryGetCreateRangeDelegate(delegateKey, out object createRangeDelegateObj))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, jsonPath);
            }

            JsonSerializerOptions.ImmutableDictCreateRangeDelegate<string, TRuntimeProperty> createRangeDelegate = (
                (JsonSerializerOptions.ImmutableDictCreateRangeDelegate<string, TRuntimeProperty>)createRangeDelegateObj);

            return (IDictionary)createRangeDelegate.Invoke(CreateGenericIEnumerableFromDictionary(sourceDictionary));
        }

        public override ValueType CreateKeyValuePairInstance(ref ReadStack state, IDictionary sourceDictionary, JsonSerializerOptions options)
        {
            Type enumerableType = state.Current.JsonPropertyInfo.RuntimePropertyType;

            // Form {"MyKey": 1}.
            if (sourceDictionary.Count == 1)
            {
                IDictionaryEnumerator enumerator = sourceDictionary.GetEnumerator();
                enumerator.MoveNext();
                return new KeyValuePair<string, TRuntimeProperty>((string)enumerator.Key, (TRuntimeProperty)enumerator.Value);
            }
            // Form {"Key": "MyKey", "Value": 1}.
            else if (sourceDictionary.Count == 2 &&
                sourceDictionary["Key"] is string key &&
                sourceDictionary["Value"] is TRuntimeProperty value
                )
            {
                return new KeyValuePair<string, TRuntimeProperty>(key, value);
            }

            throw ThrowHelper.GetJsonException_DeserializeUnableToConvertValue(enumerableType, state.JsonPath);
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
