// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Describes the context in which a validation is being performed.
    /// </summary>
    /// <remarks>
    ///     This class contains information describing the instance on which
    ///     validation is being performed.
    ///     <para>
    ///         It supports <see cref="IServiceProvider" /> so that custom validation
    ///         code can acquire additional services to help it perform its validation.
    ///     </para>
    ///     <para>
    ///         An <see cref="Items" /> property bag is available for additional contextual
    ///         information about the validation.  Values stored in <see cref="Items" />
    ///         will be available to validation methods that use this <see cref="ValidationContext" />
    ///     </para>
    /// </remarks>
    public sealed class ValidationContext
        // WinStore impl no longer inherits from IServiceProvider but still has GetService()???
        // Also we use this ability in Validator.CreateValidationContext()??
        : IServiceProvider
    {
        internal const string InstanceTypeNotStaticallyDiscovered = "The Type of instance cannot be statically discovered and the Type's properties can be trimmed.";

        #region Member Fields

        private readonly Dictionary<object, object?> _items;
        private string? _displayName;
        private Func<Type, object?>? _serviceProvider;

        #endregion

        #region Constructors

        /// <summary>
        ///     Construct a <see cref="ValidationContext" /> for a given object instance being validated.
        /// </summary>
        /// <param name="instance">The object instance being validated.  It cannot be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="instance" /> is <c>null</c></exception>
        [RequiresUnreferencedCode(InstanceTypeNotStaticallyDiscovered)]
        public ValidationContext(object instance)
            : this(instance, null, null)
        {
        }

        /// <summary>
        ///     Construct a <see cref="ValidationContext" /> for a given object instance and an optional
        ///     property bag of <paramref name="items" />.
        /// </summary>
        /// <param name="instance">The object instance being validated.  It cannot be null.</param>
        /// <param name="items">
        ///     Optional set of key/value pairs to make available to consumers via <see cref="Items" />.
        ///     If null, an empty dictionary will be created.  If not null, the set of key/value pairs will be copied into a
        ///     new dictionary, preventing consumers from modifying the original dictionary.
        /// </param>
        /// <exception cref="ArgumentNullException">When <paramref name="instance" /> is <c>null</c></exception>
        [RequiresUnreferencedCode(InstanceTypeNotStaticallyDiscovered)]
        public ValidationContext(object instance, IDictionary<object, object?>? items)
            : this(instance, null, items)
        {
        }

        /// <summary>
        ///     Construct a <see cref="ValidationContext" /> for a given object instance, an optional
        ///     <paramref name="serviceProvider" />, and an optional
        ///     property bag of <paramref name="items" />.
        /// </summary>
        /// <param name="instance">The object instance being validated.  It cannot be null.</param>
        /// <param name="serviceProvider">
        ///     Optional <see cref="IServiceProvider" /> to use when <see cref="GetService" /> is called.
        ///     If it is null, <see cref="GetService" /> will always return null.
        /// </param>
        /// <param name="items">
        ///     Optional set of key/value pairs to make available to consumers via <see cref="Items" />.
        ///     If null, an empty dictionary will be created.  If not null, the set of key/value pairs will be copied into a
        ///     new dictionary, preventing consumers from modifying the original dictionary.
        /// </param>
        /// <exception cref="ArgumentNullException">When <paramref name="instance" /> is <c>null</c></exception>
        [RequiresUnreferencedCode(InstanceTypeNotStaticallyDiscovered)]
        public ValidationContext(object instance, IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
        {
            ArgumentNullException.ThrowIfNull(instance);

            if (serviceProvider != null)
            {
                IServiceProvider localServiceProvider = serviceProvider;
                InitializeServiceProvider(localServiceProvider.GetService);
            }

            _items = items != null ? new Dictionary<object, object?>(items) : new Dictionary<object, object?>();
            ObjectInstance = instance;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the object instance being validated.  While it will not be null, the state of the instance is indeterminate
        ///     as it might only be partially initialized during validation.
        ///     <para>Consume this instance with caution!</para>
        /// </summary>
        /// <remarks>
        ///     During validation, especially property-level validation, the object instance might be in an indeterminate state.
        ///     For example, the property being validated, as well as other properties on the instance might not have been
        ///     updated to their new values.
        /// </remarks>
        public object ObjectInstance { get; }

        /// <summary>
        ///     Gets the type of the object being validated.  It will not be null.
        /// </summary>
        public Type ObjectType => ObjectInstance.GetType();

        /// <summary>
        ///     Gets or sets the user-visible name of the type or property being validated.
        /// </summary>
        /// <value>
        ///     If this name was not explicitly set, this property will consult an associated <see cref="DisplayAttribute" />
        ///     to see if can use that instead.  Lacking that, it returns <see cref="MemberName" />.  The
        ///     <see cref="ObjectInstance" />
        ///     type name will be used if MemberName has not been set.
        /// </value>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    _displayName = GetDisplayName();
                    if (string.IsNullOrEmpty(_displayName))
                    {
                        _displayName = ObjectType.Name;
                    }
                }
                return _displayName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _displayName = value;
            }
        }

        /// <summary>
        ///     Gets or sets the name of the type or property being validated.
        /// </summary>
        /// <value>
        ///     This name reflects the API name of the member being validated, not a localized name.  It should be set
        ///     only for property or parameter contexts.
        /// </value>
        public string? MemberName { get; set; }

        /// <summary>
        ///     Gets the dictionary of key/value pairs associated with this context.
        /// </summary>
        /// <value>
        ///     This property will never be null, but the dictionary may be empty.  Changes made
        ///     to items in this dictionary will never affect the original dictionary specified in the constructor.
        /// </value>
        public IDictionary<object, object?> Items => _items;

        internal Type? MemberType
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The ctors are marked with RequiresUnreferencedCode.")]
            get
            {
                Type? propertyType = _propertyType;

                if (propertyType is null && MemberName != null)
                {
                    _propertyType = propertyType = ValidationAttributeStore.Instance.GetPropertyType(this);
                }

                return propertyType;
            }

            set => _propertyType = value;
        }

        private Type? _propertyType;

        #endregion

        #region Methods

        /// <summary>
        ///     Looks up the display name using the DisplayAttribute attached to the respective type or property.
        /// </summary>
        /// <returns>A display-friendly name of the member represented by the <see cref="MemberName" />.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctors are marked with RequiresUnreferencedCode.")]
        private string? GetDisplayName()
        {
            string? displayName = null;
            ValidationAttributeStore store = ValidationAttributeStore.Instance;
            DisplayAttribute? displayAttribute = null;

            if (string.IsNullOrEmpty(MemberName))
            {
                displayAttribute = store.GetTypeDisplayAttribute(this);
            }
            else if (store.IsPropertyContext(this))
            {
                displayAttribute = store.GetPropertyDisplayAttribute(this);
            }

            if (displayAttribute != null)
            {
                displayName = displayAttribute.GetName();
            }

            return displayName ?? MemberName;
        }

        /// <summary>
        ///     Initializes the <see cref="ValidationContext" /> with a service provider that can return
        ///     service instances by <see cref="Type" /> when <see cref="GetService" /> is called.
        /// </summary>
        /// <param name="serviceProvider">
        ///     A <see cref="Func{T, TResult}" /> that can return service instances given the
        ///     desired <see cref="Type" /> when <see cref="GetService" /> is called.
        ///     If it is <c>null</c>, <see cref="GetService" /> will always return <c>null</c>.
        /// </param>
        public void InitializeServiceProvider(Func<Type, object?> serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region IServiceProvider Members

        /// <summary>
        ///     See <see cref="IServiceProvider.GetService(Type)" />.
        /// </summary>
        /// <param name="serviceType">The type of the service needed.</param>
        /// <returns>An instance of that service or null if it is not available.</returns>
        public object? GetService(Type serviceType) => _serviceProvider?.Invoke(serviceType);

        #endregion
    }
}
