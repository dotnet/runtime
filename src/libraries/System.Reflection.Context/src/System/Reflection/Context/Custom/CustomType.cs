// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Projection;
using System.Reflection.Context.Virtual;

namespace System.Reflection.Context.Custom
{
    internal sealed class CustomType : ProjectingType
    {
        private IEnumerable<PropertyInfo>? _newProperties;

        public CustomType(Type template, CustomReflectionContext context)
            : base(template, context.Projector)
        {
            ReflectionContext = context;
        }

        public CustomReflectionContext ReflectionContext { get; }

        // Currently only the results of GetCustomAttributes can be customizaed.
        // We don't need to override GetCustomAttributesData.
        public override object[] GetCustomAttributes(bool inherit)
        {
            return GetCustomAttributes(typeof(object), inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return AttributeUtils.GetCustomAttributes(ReflectionContext, this, attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return AttributeUtils.IsDefined(this, attributeType, inherit);
        }

        public override bool IsInstanceOfType([NotNullWhen(true)] object? o)
        {
            Type objectType = ReflectionContext.GetTypeForObject(o!);
            return IsAssignableFrom(objectType);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            // list of properties on this type according to the underlying ReflectionContext
            PropertyInfo[] properties = base.GetProperties(bindingAttr);

            // Optimization: we currently don't support adding nonpublic or static properties,
            // so if Public or Instance is not set we don't need to check for new properties.
            bool getDeclaredOnly = (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly;
            bool getInstance = (bindingAttr & BindingFlags.Instance) == BindingFlags.Instance;
            bool getPublic = (bindingAttr & BindingFlags.Public) == BindingFlags.Public;
            if (!getPublic || !getInstance)
                return properties;

            List<PropertyInfo> results = new List<PropertyInfo>(properties);

            //Unlike in runtime reflection, the newly added properties don't hide properties with the same name and signature.
            results.AddRange(NewProperties);

            // adding new properties declared on base types
            if (!getDeclaredOnly)
            {
                CustomType? baseType = BaseType as CustomType;
                while (baseType != null)
                {
                    IEnumerable<PropertyInfo> newProperties = baseType.NewProperties;

                    // We shouldn't add a base type property directly on a subtype.
                    // A new property with a different ReflectedType should be used.
                    foreach (PropertyInfo prop in newProperties)
                        results.Add(new InheritedPropertyInfo(prop, this));

                    baseType = baseType.BaseType as CustomType;
                }
            }

            return results.ToArray();
        }

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            PropertyInfo? property = base.GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);

            bool getIgnoreCase = (bindingAttr & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase;
            bool getDeclaredOnly = (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly;
            bool getInstance = (bindingAttr & BindingFlags.Instance) == BindingFlags.Instance;
            bool getPublic = (bindingAttr & BindingFlags.Public) == BindingFlags.Public;

            // If the ReflectionContext adds a property with identical name and type to an existing property,
            // the behavior is unspecified.
            // In this implementation, we return the existing property.
            if (!getPublic || !getInstance)
                return property;

            // Adding indexer properties is currently not supported.
            if (types != null && types.Length > 0)
                return property;

            List<PropertyInfo> matchingProperties = new List<PropertyInfo>();
            if (property != null)
                matchingProperties.Add(property);

            // If the ReflectionContext adds two or more properties with the same name and type,
            // the behavior is unspecified.
            // In this implementation, we throw AmbiguousMatchException even if the two properties are
            // defined on different types (base and sub classes).

            StringComparison comparison = getIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            CustomType? type = this;
            foreach (PropertyInfo newDeclaredProperty in type.NewProperties)
            {
                if (string.Equals(newDeclaredProperty.Name, name, comparison))
                    matchingProperties.Add(newDeclaredProperty);
            }

            if (!getDeclaredOnly)
            {
                while ((type = type.BaseType as CustomType) != null)
                {
                    foreach (PropertyInfo newBaseProperty in type.NewProperties)
                    {
                        if (string.Equals(newBaseProperty.Name, name, comparison))
                            matchingProperties.Add(new InheritedPropertyInfo(newBaseProperty, this));
                    }
                }
            }

            if (matchingProperties.Count == 0)
                return null;

            if (binder == null)
                binder = Type.DefaultBinder;

            return binder.SelectProperty(bindingAttr, matchingProperties.ToArray(), returnType, types, modifiers);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            // list of methods on this type according to the underlying ReflectionContext
            MethodInfo[] methods = base.GetMethods(bindingAttr);

            // Optimization: we currently don't support adding nonpublic or static property getters or setters,
            // so if Public or Instance is not set we don't need to check for new properties.
            bool getDeclaredOnly = (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly;
            bool getInstance = (bindingAttr & BindingFlags.Instance) == BindingFlags.Instance;
            bool getPublic = (bindingAttr & BindingFlags.Public) == BindingFlags.Public;
            if (!getPublic || !getInstance)
                return methods;

            List<MethodInfo> results = new List<MethodInfo>(methods);

            // in runtime reflection hidden methods are always returned in GetMethods
            foreach (PropertyInfo prop in NewProperties)
            {
                results.AddRange(prop.GetAccessors());
            }

            // adding new methods declared on base types
            if (!getDeclaredOnly)
            {
                CustomType? baseType = BaseType as CustomType;
                while (baseType != null)
                {
                    // We shouldn't add a base type method directly on a subtype.
                    // A new method with a different ReflectedType should be used.
                    foreach (PropertyInfo prop in baseType.NewProperties)
                    {
                        PropertyInfo inheritedProperty = new InheritedPropertyInfo(prop, this);
                        results.AddRange(inheritedProperty.GetAccessors());
                    }

                    baseType = baseType.BaseType as CustomType;
                }
            }

            return results.ToArray();
        }

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            MethodInfo? method = base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);

            bool getIgnoreCase = (bindingAttr & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase;
            bool getDeclaredOnly = (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly;
            bool getInstance = (bindingAttr & BindingFlags.Instance) == BindingFlags.Instance;
            bool getPublic = (bindingAttr & BindingFlags.Public) == BindingFlags.Public;

            // If the ReflectionContext adds a property with identical name and type to an existing property,
            // the behavior is unspecified.
            // In this implementation, we return the existing method.
            if (!getPublic || !getInstance)
                return method;

            bool getPropertyGetter = false;
            bool getPropertySetter = false;

            StringComparison comparison = getIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (name.Length > 4)
            {
                // Right now we don't support adding fabricated indexers on types
                getPropertyGetter = (types == null || types.Length == 0) && name.StartsWith("get_", comparison);

                if (!getPropertyGetter)
                    getPropertySetter = (types == null || types.Length == 1) && name.StartsWith("set_", comparison);
            }

            // not a property getter or setter
            if (!getPropertyGetter && !getPropertySetter)
                return method;

            // get the target property name by removing "get_" or "set_"
            string targetPropertyName = name.Substring(4);

            List<MethodInfo> matchingMethods = new List<MethodInfo>();
            if (method != null)
                matchingMethods.Add(method);

            // in runtime reflection hidden methods are always returned in GetMethods
            foreach (PropertyInfo newDeclaredProperty in NewProperties)
            {
                if (string.Equals(newDeclaredProperty.Name, targetPropertyName, comparison))
                {
                    MethodInfo? accessor = getPropertyGetter ? newDeclaredProperty.GetGetMethod() : newDeclaredProperty.GetSetMethod();
                    if (accessor != null)
                        matchingMethods.Add(accessor);
                }
            }

            // adding new methods declared on base types
            if (!getDeclaredOnly)
            {
                CustomType? baseType = BaseType as CustomType;

                while (baseType != null)
                {
                    // We shouldn't add a base type method directly on a subtype.
                    // A new method with a different ReflectedType should be used.
                    foreach (PropertyInfo newBaseProperty in baseType.NewProperties)
                    {
                        if (string.Equals(newBaseProperty.Name, targetPropertyName, comparison))
                        {
                            PropertyInfo inheritedProperty = new InheritedPropertyInfo(newBaseProperty, this);

                            MethodInfo? accessor = getPropertyGetter ? inheritedProperty.GetGetMethod() : inheritedProperty.GetSetMethod();
                            if (accessor != null)
                                matchingMethods.Add(accessor);
                        }
                    }

                    baseType = baseType.BaseType as CustomType;
                }
            }


            if (matchingMethods.Count == 0)
                return null;

            if (types == null || getPropertyGetter)
            {
                Debug.Assert(types == null || types.Length == 0);

                // matches any signature
                if (matchingMethods.Count == 1)
                    return matchingMethods[0];
                else
                    throw new AmbiguousMatchException();
            }
            else
            {
                Debug.Assert(getPropertySetter && types != null && types.Length == 1);

                if (binder == null)
                    binder = Type.DefaultBinder;

                return (MethodInfo?)binder.SelectMethod(bindingAttr, matchingMethods.ToArray(), types, modifiers);
            }
        }

        private IEnumerable<PropertyInfo> NewProperties
        {
            get
            {
                if (_newProperties == null)
                {
                    _newProperties = ReflectionContext.GetNewPropertiesForType(this);
                }

                return _newProperties;
            }
        }
    }
}
