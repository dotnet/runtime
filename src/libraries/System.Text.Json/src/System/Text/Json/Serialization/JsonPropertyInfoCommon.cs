// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
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
                ValueConverter = DefaultConverters<TRuntimeProperty>.s_converter;
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

        // Creates an IEnumerable<TRuntimePropertyType> and populates it with the items in the,
        // sourceList argument then uses the delegateKey argument to identify the appropriate cached
        // CreateRange<TRuntimePropertyType> method to create and return the desired immutable collection type.
        public override IEnumerable CreateImmutableCollectionFromList(string delegateKey, IList sourceList)
        {
            Debug.Assert(DefaultImmutableConverter.CreateRangeDelegates.ContainsKey(delegateKey));

            DefaultImmutableConverter.ImmutableCreateRangeDelegate<TRuntimeProperty> createRangeDelegate = (
                (DefaultImmutableConverter.ImmutableCreateRangeDelegate<TRuntimeProperty>)DefaultImmutableConverter.CreateRangeDelegates[delegateKey]);

            return (IEnumerable)createRangeDelegate.Invoke(CreateGenericIEnumerableFromList(sourceList));
        }

        public override IEnumerable CreateIEnumerableConstructibleType(Type enumerableType, IList sourceList)
        {
            return (IEnumerable)Activator.CreateInstance(enumerableType, CreateGenericIEnumerableFromList(sourceList));
        }

        private IEnumerable<TRuntimeProperty> CreateGenericIEnumerableFromList(IList sourceList)
        {
            foreach (object item in sourceList)
            {
                yield return (TRuntimeProperty)item;
            }
        }
    }
}
