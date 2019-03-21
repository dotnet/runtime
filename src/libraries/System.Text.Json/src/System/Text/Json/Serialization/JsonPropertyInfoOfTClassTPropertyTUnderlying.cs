// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal abstract class JsonPropertyInfo<TClass, TProperty, TUnderlying> : JsonPropertyInfo
    {
        internal bool _isPropertyPolicy;
        internal Func<TClass, TProperty> Get { get; private set; }
        internal Action<TClass, TProperty> Set { get; private set; }

        public JsonValueConverter<TUnderlying> ValueConverter { get; internal set; }

        // Constructor used for internal identifiers
        internal JsonPropertyInfo() { }

        internal JsonPropertyInfo(Type classType, Type propertyType, PropertyInfo propertyInfo, Type elementType, JsonSerializerOptions options) :
            base(classType, propertyType, propertyInfo, elementType, options)
        {
            if (propertyInfo != null)
            {
                if (propertyInfo.GetMethod?.IsPublic == true)
                {
                    HasGetter = true;
                    Get = (Func<TClass, TProperty>)Delegate.CreateDelegate(typeof(Func<TClass, TProperty>), propertyInfo.GetGetMethod());
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    HasSetter = true;
                    Set = (Action<TClass, TProperty>)Delegate.CreateDelegate(typeof(Action<TClass, TProperty>), propertyInfo.GetSetMethod());
                }
            }
            else
            {
                _isPropertyPolicy = true;
                HasGetter = true;
                HasSetter = true;
            }

            GetPolicies(options);
        }

        internal override void GetPolicies(JsonSerializerOptions options)
        {
            ValueConverter = DefaultConverters<TUnderlying>.s_converter;

            base.GetPolicies(options);
        }

        internal override object GetValueAsObject(object obj, JsonSerializerOptions options)
        {
            if (_isPropertyPolicy)
            {
                return obj;
            }

            Debug.Assert(Get != null);
            return Get((TClass)obj);
        }

        internal override void SetValueAsObject(object obj, object value, JsonSerializerOptions options)
        {
            Debug.Assert(Set != null);
            TProperty typedValue = (TProperty)value;

            if (typedValue != null || !IgnoreNullPropertyValueOnWrite(options))
            {
                Set((TClass)obj, (TProperty)value);
            }
        }
    }
}
