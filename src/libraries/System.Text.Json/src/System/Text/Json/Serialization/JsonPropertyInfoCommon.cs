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
        public bool _isPropertyPolicy;
        public Func<TClass, TDeclaredProperty> Get { get; private set; }
        public Action<TClass, TDeclaredProperty> Set { get; private set; }

        public JsonValueConverter<TRuntimeProperty> ValueConverter { get; internal set; }

        // Constructor used for internal identifiers
        public JsonPropertyInfoCommon() { }

        public JsonPropertyInfoCommon(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonSerializerOptions options) :
            base(parentClassType, declaredPropertyType, runtimePropertyType, propertyInfo, elementType, options)
        {
            if (propertyInfo != null)
            {
                if (propertyInfo.GetMethod?.IsPublic == true)
                {
                    HasGetter = true;
                    Get = (Func<TClass, TDeclaredProperty>)Delegate.CreateDelegate(typeof(Func<TClass, TDeclaredProperty>), propertyInfo.GetGetMethod());
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    HasSetter = true;
                    Set = (Action<TClass, TDeclaredProperty>)Delegate.CreateDelegate(typeof(Action<TClass, TDeclaredProperty>), propertyInfo.GetSetMethod());
                }
            }
            else
            {
                _isPropertyPolicy = true;
                HasGetter = true;
                HasSetter = true;
                ValueConverter = DefaultConverters<TRuntimeProperty>.s_converter;
            }

            GetPolicies(options);
        }

        public override void GetPolicies(JsonSerializerOptions options)
        {
            ValueConverter = DefaultConverters<TRuntimeProperty>.s_converter;
            base.GetPolicies(options);
        }

        public override object GetValueAsObject(object obj)
        {
            if (_isPropertyPolicy)
            {
                return obj;
            }

            Debug.Assert(Get != null);
            return Get((TClass)obj);
        }

        public override void SetValueAsObject(object obj, object value)
        {
            Debug.Assert(Set != null);
            TDeclaredProperty typedValue = (TDeclaredProperty)value;

            if (typedValue != null || !IgnoreNullValues)
            {
                Set((TClass)obj, (TDeclaredProperty)value);
            }
        }

        public override IList CreateConverterList()
        {
            return new List<TDeclaredProperty>();
        }

        // Map interfaces to a well-known implementation.
        public override Type GetConcreteType(Type interfaceType)
        {
            if (interfaceType.IsAssignableFrom(typeof(IDictionary<string, TRuntimeProperty>)) ||
                interfaceType.IsAssignableFrom(typeof(IReadOnlyDictionary<string, TRuntimeProperty>)))
            {
                return typeof(Dictionary<string, TRuntimeProperty>);
            }

            return interfaceType;
        }
    }
}
