// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Cache of <see cref="ValidationAttribute" />s
    /// </summary>
    /// <remarks>
    ///     This internal class serves as a cache of validation attributes and [Display] attributes.
    ///     It exists both to help performance as well as to abstract away the differences between
    ///     Reflection and TypeDescriptor.
    /// </remarks>
    internal class ValidationAttributeStore
    {
        private readonly Dictionary<Type, TypeStoreItem> _typeStoreItems = new Dictionary<Type, TypeStoreItem>();

        /// <summary>
        ///     Gets the singleton <see cref="ValidationAttributeStore" />
        /// </summary>
        internal static ValidationAttributeStore Instance { get; } = new ValidationAttributeStore();

        /// <summary>
        ///     Retrieves the type level validation attributes for the given type.
        /// </summary>
        /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
        /// <returns>The collection of validation attributes.  It could be empty.</returns>
        internal IEnumerable<ValidationAttribute> GetTypeValidationAttributes(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var item = GetTypeStoreItem(validationContext.ObjectType);
            return item.ValidationAttributes;
        }

        /// <summary>
        ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given type.  It may be null.
        /// </summary>
        /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
        /// <returns>The display attribute instance, if present.</returns>
        internal DisplayAttribute GetTypeDisplayAttribute(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var item = GetTypeStoreItem(validationContext.ObjectType);
            return item.DisplayAttribute;
        }

        /// <summary>
        ///     Retrieves the set of validation attributes for the property
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The collection of validation attributes.  It could be empty.</returns>
        internal IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName);
            return item.ValidationAttributes;
        }

        /// <summary>
        ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given property
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The display attribute instance, if present.</returns>
        internal DisplayAttribute GetPropertyDisplayAttribute(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName);
            return item.DisplayAttribute;
        }

        /// <summary>
        ///     Retrieves the Type of the given property.
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The type of the specified property</returns>
        internal Type GetPropertyType(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName);
            return item.PropertyType;
        }

        /// <summary>
        ///     Determines whether or not a given <see cref="ValidationContext" />'s
        ///     <see cref="ValidationContext.MemberName" /> references a property on
        ///     the <see cref="ValidationContext.ObjectType" />.
        /// </summary>
        /// <param name="validationContext">The <see cref="ValidationContext" /> to check.</param>
        /// <returns><c>true</c> when the <paramref name="validationContext" /> represents a property, <c>false</c> otherwise.</returns>
        internal bool IsPropertyContext(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            PropertyStoreItem item;
            return typeItem.TryGetPropertyStoreItem(validationContext.MemberName, out item);
        }

        /// <summary>
        ///     Retrieves or creates the store item for the given type
        /// </summary>
        /// <param name="type">The type whose store item is needed.  It cannot be null</param>
        /// <returns>The type store item.  It will not be null.</returns>
        private TypeStoreItem GetTypeStoreItem(Type type)
        {
            Debug.Assert(type != null);

            lock (_typeStoreItems)
            {
                if (!_typeStoreItems.TryGetValue(type, out TypeStoreItem item))
                {
                    // use CustomAttributeExtensions.GetCustomAttributes() to get inherited attributes as well as direct ones
                    var attributes = CustomAttributeExtensions.GetCustomAttributes(type, true);
                    item = new TypeStoreItem(type, attributes);
                    _typeStoreItems[type] = item;
                }

                return item;
            }
        }

        /// <summary>
        ///     Throws an ArgumentException of the validation context is null
        /// </summary>
        /// <param name="validationContext">The context to check</param>
        private static void EnsureValidationContext(ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }
        }

        internal static bool IsPublic(PropertyInfo p) =>
            (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic);

        internal static bool IsStatic(PropertyInfo p) =>
            (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic);

        /// <summary>
        ///     Private abstract class for all store items
        /// </summary>
        private abstract class StoreItem
        {
            internal StoreItem(IEnumerable<Attribute> attributes)
            {
                ValidationAttributes = attributes.OfType<ValidationAttribute>();
                DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault();
            }

            internal IEnumerable<ValidationAttribute> ValidationAttributes { get; }

            internal DisplayAttribute DisplayAttribute { get; }
        }

        /// <summary>
        ///     Private class to store data associated with a type
        /// </summary>
        private class TypeStoreItem : StoreItem
        {
            private readonly object _syncRoot = new object();
            private readonly Type _type;
            private Dictionary<string, PropertyStoreItem> _propertyStoreItems;

            internal TypeStoreItem(Type type, IEnumerable<Attribute> attributes)
                : base(attributes)
            {
                _type = type;
            }

            internal PropertyStoreItem GetPropertyStoreItem(string propertyName)
            {
                if (!TryGetPropertyStoreItem(propertyName, out PropertyStoreItem item))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture,
                            SR.AttributeStore_Unknown_Property, _type.Name, propertyName), nameof(propertyName));
                }

                return item;
            }

            internal bool TryGetPropertyStoreItem(string propertyName, out PropertyStoreItem item)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }

                if (_propertyStoreItems == null)
                {
                    lock (_syncRoot)
                    {
                        if (_propertyStoreItems == null)
                        {
                            _propertyStoreItems = CreatePropertyStoreItems();
                        }
                    }
                }

                return _propertyStoreItems.TryGetValue(propertyName, out item);
            }

            private Dictionary<string, PropertyStoreItem> CreatePropertyStoreItems()
            {
                var propertyStoreItems = new Dictionary<string, PropertyStoreItem>();

                // exclude index properties to match old TypeDescriptor functionality
                var properties = _type.GetRuntimeProperties()
                    .Where(prop => IsPublic(prop) && !prop.GetIndexParameters().Any());
                foreach (PropertyInfo property in properties)
                {
                    // use CustomAttributeExtensions.GetCustomAttributes() to get inherited attributes as well as direct ones
                    var item = new PropertyStoreItem(property.PropertyType,
                        CustomAttributeExtensions.GetCustomAttributes(property, true));
                    propertyStoreItems[property.Name] = item;
                }

                return propertyStoreItems;
            }
        }

        /// <summary>
        ///     Private class to store data associated with a property
        /// </summary>
        private class PropertyStoreItem : StoreItem
        {
            internal PropertyStoreItem(Type propertyType, IEnumerable<Attribute> attributes)
                : base(attributes)
            {
                Debug.Assert(propertyType != null);
                PropertyType = propertyType;
            }

            internal Type PropertyType { get; }
        }
    }
}
