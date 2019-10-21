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

        public Action<TDeclaredProperty> AddItemToEnumerable { get; private set; }

        public JsonConverter<TConverter> Converter { get; internal set; }

        public override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            ClassType runtimeClassType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonConverter converter,
            bool treatAsNullable,
            JsonSerializerOptions options)
        {
            base.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType,
                propertyInfo,
                elementType,
                converter,
                treatAsNullable,
                options);

            if (propertyInfo != null)
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

        private JsonPropertyInfo _elementPropertyInfo;

        private void SetPropertyInfoForObjectElement()
        {
            if (_elementPropertyInfo == null && ElementClassInfo.PolicyProperty == null)
            {
                _elementPropertyInfo = ElementClassInfo.CreateRootProperty(Options);
            }
        }

        public override bool TryCreateEnumerableAddMethod(object target, out object addMethodDelegate)
        {
            SetPropertyInfoForObjectElement();
            Debug.Assert((_elementPropertyInfo ?? ElementClassInfo.PolicyProperty) != null);

            addMethodDelegate = (_elementPropertyInfo ?? ElementClassInfo.PolicyProperty).CreateEnumerableAddMethod(RuntimeClassInfo.AddItemToObject, target);
            return addMethodDelegate != null;
        }

        public override object CreateEnumerableAddMethod(MethodInfo addMethod, object target)
        {
            if (target is ICollection<TDeclaredProperty> collection && collection.IsReadOnly)
            {
                return null;
            }

            return Options.MemberAccessorStrategy.CreateAddDelegate<TDeclaredProperty>(addMethod, target);
        }

        public override void AddObjectToEnumerableWithReflection(object addMethodDelegate, object value)
        {
            Debug.Assert((_elementPropertyInfo ?? ElementClassInfo.PolicyProperty) != null);
            (_elementPropertyInfo ?? ElementClassInfo.PolicyProperty).AddObjectToParentEnumerable(addMethodDelegate, value);
        }

        public override void AddObjectToParentEnumerable(object addMethodDelegate, object value)
        {
            ((Action<TDeclaredProperty>)addMethodDelegate)((TDeclaredProperty)value);
        }

        public override void AddObjectToDictionary(object target, string key, object value)
        {
            Debug.Assert((_elementPropertyInfo ?? ElementClassInfo.PolicyProperty) != null);
            (_elementPropertyInfo ?? ElementClassInfo.PolicyProperty).AddObjectToParentDictionary(target, key, value);
        }

        public override void AddObjectToParentDictionary(object target, string key, object value)
        {
            if (target is IDictionary<string, TDeclaredProperty> genericDict)
            {
                Debug.Assert(!genericDict.IsReadOnly);
                genericDict[key] = (TDeclaredProperty)value;
            }
            else
            {
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(target.GetType(), parentType: null, memberInfo: null);
            }
        }

        public override bool CanPopulateDictionary(object target)
        {
            SetPropertyInfoForObjectElement();
            Debug.Assert((_elementPropertyInfo ?? ElementClassInfo.PolicyProperty) != null);
            return (_elementPropertyInfo ?? ElementClassInfo.PolicyProperty).ParentDictionaryCanBePopulated(target);
        }

        public override bool ParentDictionaryCanBePopulated(object target)
        {
            if (target is IDictionary<string, TDeclaredProperty> genericDict && !genericDict.IsReadOnly)
            {
                return true;
            }
            else if (target is IDictionary dict && !dict.IsReadOnly)
            {
                Type genericDictType = target.GetType().GetInterface("System.Collections.Generic.IDictionary`2") ??
                    target.GetType().GetInterface("System.Collections.Generic.IReadOnlyDictionary`2");

                if (genericDictType != null && genericDictType.GetGenericArguments()[0] != typeof(string))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public override IList CreateConverterList()
        {
            return new List<TDeclaredProperty>();
        }

        public override IDictionary CreateConverterDictionary()
        {
            return new Dictionary<string, TDeclaredProperty>();
        }

        // Creates an IEnumerable<TDeclaredPropertyType> and populates it with the items in the
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TDeclaredPropertyType> method to create and return the desired immutable collection type.
        public override IEnumerable CreateImmutableCollectionInstance(ref ReadStack state, Type collectionType, string delegateKey, IList sourceList, JsonSerializerOptions options)
        {
            IEnumerable collection = null;

            if (!options.TryGetCreateRangeDelegate(delegateKey, out ImmutableCollectionCreator creator) ||
                !creator.CreateImmutableEnumerable(sourceList, out collection))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, state.JsonPath());
            }

            return collection;
        }

        // Creates an IEnumerable<TDeclaredPropertyType> and populates it with the items in the
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TDeclaredPropertyType> method to create and return the desired immutable collection type.
        public override IDictionary CreateImmutableDictionaryInstance(ref ReadStack state, Type collectionType, string delegateKey, IDictionary sourceDictionary, JsonSerializerOptions options)
        {
            IDictionary collection = null;

            if (!options.TryGetCreateRangeDelegate(delegateKey, out ImmutableCollectionCreator creator) ||
                !creator.CreateImmutableDictionary(sourceDictionary, out collection))
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(collectionType, state.JsonPath());
            }

            return collection;
        }
    }
}
