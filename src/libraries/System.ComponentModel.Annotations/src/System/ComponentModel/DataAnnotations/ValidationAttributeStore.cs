// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    internal sealed class ValidationAttributeStore
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
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
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
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
        internal DisplayAttribute? GetTypeDisplayAttribute(ValidationContext validationContext)
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
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
        internal IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
            return item.ValidationAttributes;
        }

        /// <summary>
        ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given property
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The display attribute instance, if present.</returns>
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
        internal DisplayAttribute? GetPropertyDisplayAttribute(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
            return item.DisplayAttribute;
        }

        /// <summary>
        ///     Retrieves the Type of the given property.
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The type of the specified property</returns>
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
        internal Type GetPropertyType(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName!);
            return item.PropertyType;
        }

        /// <summary>
        ///     Determines whether or not a given <see cref="ValidationContext" />'s
        ///     <see cref="ValidationContext.MemberName" /> references a property on
        ///     the <see cref="ValidationContext.ObjectType" />.
        /// </summary>
        /// <param name="validationContext">The <see cref="ValidationContext" /> to check.</param>
        /// <returns><c>true</c> when the <paramref name="validationContext" /> represents a property, <c>false</c> otherwise.</returns>
        [RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
        internal bool IsPropertyContext(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            return typeItem.TryGetPropertyStoreItem(validationContext.MemberName!, out _);
        }

        /// <summary>
        ///     Retrieves or creates the store item for the given type
        /// </summary>
        /// <param name="type">The type whose store item is needed.  It cannot be null</param>
        /// <returns>The type store item.  It will not be null.</returns>
        private TypeStoreItem GetTypeStoreItem([DynamicallyAccessedMembers(TypeStoreItem.DynamicallyAccessedTypes)] Type type)
        {
            Debug.Assert(type != null);

            lock (_typeStoreItems)
            {
                if (!_typeStoreItems.TryGetValue(type, out TypeStoreItem? item))
                {
                    AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
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
            ArgumentNullException.ThrowIfNull(validationContext);
        }

        internal static bool IsPublic(PropertyInfo p) =>
            (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic);

        /// <summary>
        ///     Private abstract class for all store items
        /// </summary>
        private abstract class StoreItem
        {
            internal StoreItem(AttributeCollection attributes)
            {
                ValidationAttributes = attributes.OfType<ValidationAttribute>();
                DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault();
            }

            internal IEnumerable<ValidationAttribute> ValidationAttributes { get; }

            internal DisplayAttribute? DisplayAttribute { get; }
        }

        /// <summary>
        ///     Private class to store data associated with a type
        /// </summary>
        private sealed class TypeStoreItem : StoreItem
        {
            internal const DynamicallyAccessedMemberTypes DynamicallyAccessedTypes = DynamicallyAccessedMemberTypes.All;

            private readonly object _syncRoot = new object();
            [DynamicallyAccessedMembers(DynamicallyAccessedTypes)]
            private readonly Type _type;
            private Dictionary<string, PropertyStoreItem>? _propertyStoreItems;

            internal TypeStoreItem([DynamicallyAccessedMembers(DynamicallyAccessedTypes)] Type type, AttributeCollection attributes)
                : base(attributes)
            {
                _type = type;
            }

            [RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
            internal PropertyStoreItem GetPropertyStoreItem(string propertyName)
            {
                if (!TryGetPropertyStoreItem(propertyName, out PropertyStoreItem? item))
                {
                    throw new ArgumentException(SR.Format(SR.AttributeStore_Unknown_Property, _type.Name, propertyName),
                                                nameof(propertyName));
                }

                return item;
            }

            [RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
            internal bool TryGetPropertyStoreItem(string propertyName, [NotNullWhen(true)] out PropertyStoreItem? item)
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

            [RequiresUnreferencedCode("The Types of _type's properties cannot be statically discovered.")]
            private Dictionary<string, PropertyStoreItem> CreatePropertyStoreItems()
            {
                var propertyStoreItems = new Dictionary<string, PropertyStoreItem>();

                var properties = TypeDescriptor.GetProperties(_type);
                foreach (PropertyDescriptor property in properties)
                {
                    var item = new PropertyStoreItem(property.PropertyType, GetExplicitAttributes(property));
                    propertyStoreItems[property.Name] = item;
                }

                return propertyStoreItems;
            }

            /// <summary>
            ///     Method to extract only the explicitly specified attributes from a <see cref="PropertyDescriptor"/>
            /// </summary>
            /// <remarks>
            ///     Normal TypeDescriptor semantics are to inherit the attributes of a property's type.  This method
            ///     exists to suppress those inherited attributes.
            /// </remarks>
            /// <param name="propertyDescriptor">The property descriptor whose attributes are needed.</param>
            /// <returns>A new <see cref="AttributeCollection"/> stripped of any attributes from the property's type.</returns>
            [RequiresUnreferencedCode("The Type of propertyDescriptor.PropertyType cannot be statically discovered.")]
            private static AttributeCollection GetExplicitAttributes(PropertyDescriptor propertyDescriptor)
            {
                AttributeCollection propertyDescriptorAttributes = propertyDescriptor.Attributes;
                List<Attribute> attributes = new List<Attribute>(propertyDescriptorAttributes.Count);
                foreach (Attribute attribute in propertyDescriptorAttributes)
                {
                    attributes.Add(attribute);
                }

                AttributeCollection typeAttributes = TypeDescriptor.GetAttributes(propertyDescriptor.PropertyType);
                bool removedAttribute = false;
                foreach (Attribute attr in typeAttributes)
                {
                    for (int i = attributes.Count - 1; i >= 0; --i)
                    {
                        // We must use ReferenceEquals since attributes could Match if they are the same.
                        // Only ReferenceEquals will catch actual duplications.
                        if (object.ReferenceEquals(attr, attributes[i]))
                        {
                            attributes.RemoveAt(i);
                            removedAttribute = true;
                        }
                    }
                }
                return removedAttribute ? new AttributeCollection(attributes.ToArray()) : propertyDescriptorAttributes;
            }
        }

        /// <summary>
        ///     Private class to store data associated with a property
        /// </summary>
        private sealed class PropertyStoreItem : StoreItem
        {
            internal PropertyStoreItem(Type propertyType, AttributeCollection attributes)
                : base(attributes)
            {
                Debug.Assert(propertyType != null);
                PropertyType = propertyType;
            }

            internal Type PropertyType { get; }
        }
    }
}
