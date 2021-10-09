// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Base class for all validation attributes.
    ///     <para>Override <see cref="IsValid(object, ValidationContext)" /> to implement validation logic.</para>
    /// </summary>
    /// <remarks>
    ///     The properties <see cref="ErrorMessageResourceType" /> and <see cref="ErrorMessageResourceName" /> are used to
    ///     provide
    ///     a localized error message, but they cannot be set if <see cref="ErrorMessage" /> is also used to provide a
    ///     non-localized
    ///     error message.
    /// </remarks>
    public abstract class ValidationAttribute : Attribute
    {
        #region Member Fields

        private string? _errorMessage;
        private Func<string>? _errorMessageResourceAccessor;
        private string? _errorMessageResourceName;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        private Type? _errorMessageResourceType;
        private volatile bool _hasBaseIsValid;
        private string? _defaultErrorMessage;
        #endregion

        #region All Constructors

        /// <summary>
        ///     Default constructor for any validation attribute.
        /// </summary>
        /// <remarks>
        ///     This constructor chooses a very generic validation error message.
        ///     Developers subclassing ValidationAttribute should use other constructors
        ///     or supply a better message.
        /// </remarks>
        protected ValidationAttribute()
            : this(() => SR.ValidationAttribute_ValidationError)
        {
        }

        /// <summary>
        ///     Constructor that accepts a fixed validation error message.
        /// </summary>
        /// <param name="errorMessage">A non-localized error message to use in <see cref="ErrorMessageString" />.</param>
        protected ValidationAttribute(string errorMessage)
            : this(() => errorMessage)
        {
        }

        /// <summary>
        ///     Allows for providing a resource accessor function that will be used by the <see cref="ErrorMessageString" />
        ///     property to retrieve the error message.  An example would be to have something like
        ///     CustomAttribute() : base( () =&gt; MyResources.MyErrorMessage ) {}.
        /// </summary>
        /// <param name="errorMessageAccessor">The <see cref="Func{T}" /> that will return an error message.</param>
        protected ValidationAttribute(Func<string> errorMessageAccessor)
        {
            // If null, will later be exposed as lack of error message to be able to construct accessor
            _errorMessageResourceAccessor = errorMessageAccessor;
        }

        #endregion

        #region Internal Properties
        /// <summary>
        /// Sets the default error message string.
        /// This message will be used if the user has not set <see cref="ErrorMessage"/>
        /// or the <see cref="ErrorMessageResourceType"/> and <see cref="ErrorMessageResourceName"/> pair.
        /// This property was added after the public contract for DataAnnotations was created.
        /// It is internal to avoid changing the DataAnnotations contract.
        /// </summary>
        internal string? DefaultErrorMessage
        {
            set
            {
                _defaultErrorMessage = value;
                _errorMessageResourceAccessor = null;
                CustomErrorMessageSet = true;
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        ///     Gets the localized error message string, coming either from <see cref="ErrorMessage" />, or from evaluating the
        ///     <see cref="ErrorMessageResourceType" /> and <see cref="ErrorMessageResourceName" /> pair.
        /// </summary>
        protected string ErrorMessageString
        {
            get
            {
                SetupResourceAccessor();
                return _errorMessageResourceAccessor!();
            }
        }

        /// <summary>
        ///     A flag indicating whether a developer has customized the attribute's error message by setting any one of
        ///     ErrorMessage, ErrorMessageResourceName, ErrorMessageResourceType or DefaultErrorMessage.
        /// </summary>
        internal bool CustomErrorMessageSet { get; private set; }

        /// <summary>
        ///     A flag indicating that the attribute requires a non-null
        ///     <see cref="ValidationContext" /> to perform validation.
        ///     Base class returns false. Override in child classes as appropriate.
        /// </summary>
        public virtual bool RequiresValidationContext => false;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the explicit error message string.
        /// </summary>
        /// <value>
        ///     This property is intended to be used for non-localizable error messages.  Use
        ///     <see cref="ErrorMessageResourceType" /> and <see cref="ErrorMessageResourceName" /> for localizable error messages.
        /// </value>
        public string? ErrorMessage
        {
            // If _errorMessage is not set, return the default. This is done to preserve
            // behavior prior to the fix where ErrorMessage showed the non-null message to use.
            get => _errorMessage ?? _defaultErrorMessage;
            set
            {
                _errorMessage = value;
                _errorMessageResourceAccessor = null;
                CustomErrorMessageSet = true;

                // Explicitly setting ErrorMessage also sets DefaultErrorMessage if null.
                // This prevents subsequent read of ErrorMessage from returning default.
                if (value == null)
                {
                    _defaultErrorMessage = null;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the resource name (property name) to use as the key for lookups on the resource type.
        /// </summary>
        /// <value>
        ///     Use this property to set the name of the property within <see cref="ErrorMessageResourceType" />
        ///     that will provide a localized error message.  Use <see cref="ErrorMessage" /> for non-localized error messages.
        /// </value>
        public string? ErrorMessageResourceName
        {
            get => _errorMessageResourceName;
            set
            {
                _errorMessageResourceName = value;
                _errorMessageResourceAccessor = null;
                CustomErrorMessageSet = true;
            }
        }

        /// <summary>
        ///     Gets or sets the resource type to use for error message lookups.
        /// </summary>
        /// <value>
        ///     Use this property only in conjunction with <see cref="ErrorMessageResourceName" />.  They are
        ///     used together to retrieve localized error messages at runtime.
        ///     <para>
        ///         Use <see cref="ErrorMessage" /> instead of this pair if error messages are not localized.
        ///     </para>
        /// </value>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public Type? ErrorMessageResourceType
        {
            get => _errorMessageResourceType;
            set
            {
                _errorMessageResourceType = value;
                _errorMessageResourceAccessor = null;
                CustomErrorMessageSet = true;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Validates the configuration of this attribute and sets up the appropriate error string accessor.
        ///     This method bypasses all verification once the ResourceAccessor has been set.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        private void SetupResourceAccessor()
        {
            if (_errorMessageResourceAccessor == null)
            {
                string? localErrorMessage = ErrorMessage;
                bool resourceNameSet = !string.IsNullOrEmpty(_errorMessageResourceName);
                bool errorMessageSet = !string.IsNullOrEmpty(_errorMessage);
                bool resourceTypeSet = _errorMessageResourceType != null;
                bool defaultMessageSet = !string.IsNullOrEmpty(_defaultErrorMessage);

                // The following combinations are illegal and throw InvalidOperationException:
                //   1) Both ErrorMessage and ErrorMessageResourceName are set, or
                //   2) None of ErrorMessage, ErrorMessageResourceName, and DefaultErrorMessage are set.
                if ((resourceNameSet && errorMessageSet) || !(resourceNameSet || errorMessageSet || defaultMessageSet))
                {
                    throw new InvalidOperationException(
                        SR.ValidationAttribute_Cannot_Set_ErrorMessage_And_Resource);
                }

                // Must set both or neither of ErrorMessageResourceType and ErrorMessageResourceName
                if (resourceTypeSet != resourceNameSet)
                {
                    throw new InvalidOperationException(
                        SR.ValidationAttribute_NeedBothResourceTypeAndResourceName);
                }

                // If set resource type (and we know resource name too), then go setup the accessor
                if (resourceNameSet)
                {
                    SetResourceAccessorByPropertyLookup();
                }
                else
                {
                    // Here if not using resource type/name -- the accessor is just the error message string,
                    // which we know is not empty to have gotten this far.
                    // We captured error message to local in case it changes before accessor runs
                    _errorMessageResourceAccessor = () => localErrorMessage!;
                }
            }
        }

        private void SetResourceAccessorByPropertyLookup()
        {
            Debug.Assert(_errorMessageResourceType != null);
            Debug.Assert(!string.IsNullOrEmpty(_errorMessageResourceName));
            var property = _errorMessageResourceType
                .GetProperty(_errorMessageResourceName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

            if (property != null)
            {
                var propertyGetter = property.GetMethod;

                // We only support internal and public properties
                if (propertyGetter == null || (!propertyGetter.IsAssembly && !propertyGetter.IsPublic))
                {
                    // Set the property to null so the exception is thrown as if the property wasn't found
                    property = null;
                }
            }

            if (property == null)
            {
                throw new InvalidOperationException(SR.Format(SR.ValidationAttribute_ResourceTypeDoesNotHaveProperty,
                                                    _errorMessageResourceType.FullName,
                                                    _errorMessageResourceName));
            }

            if (property.PropertyType != typeof(string))
            {
                throw new InvalidOperationException(SR.Format(SR.ValidationAttribute_ResourcePropertyNotStringType,
                                                    property.Name,
                                                    _errorMessageResourceType.FullName));
            }

            _errorMessageResourceAccessor = () => (string)property.GetValue(null, null)!;
        }

        #endregion

        #region Protected & Public Methods

        /// <summary>
        ///     Formats the error message to present to the user.
        /// </summary>
        /// <remarks>
        ///     The error message will be re-evaluated every time this function is called.
        ///     It applies the <paramref name="name" /> (for example, the name of a field) to the formated error message, resulting
        ///     in something like "The field 'name' has an incorrect value".
        ///     <para>
        ///         Derived classes can override this method to customize how errors are generated.
        ///     </para>
        ///     <para>
        ///         The base class implementation will use <see cref="ErrorMessageString" /> to obtain a localized
        ///         error message from properties within the current attribute.  If those have not been set, a generic
        ///         error message will be provided.
        ///     </para>
        /// </remarks>
        /// <param name="name">The user-visible name to include in the formatted message.</param>
        /// <returns>The localized string describing the validation error</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        public virtual string FormatErrorMessage(string name) =>
            string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);

        /// <summary>
        ///     Gets the value indicating whether or not the specified <paramref name="value" /> is valid
        ///     with respect to the current validation attribute.
        ///     <para>
        ///         Derived classes should not override this method as it is only available for backwards compatibility.
        ///         Instead, implement <see cref="IsValid(object, ValidationContext)" />.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The preferred public entry point for clients requesting validation is the <see cref="GetValidationResult" />
        ///     method.
        /// </remarks>
        /// <param name="value">The value to validate</param>
        /// <returns><c>true</c> if the <paramref name="value" /> is acceptable, <c>false</c> if it is not acceptable</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        /// <exception cref="NotImplementedException">
        ///     is thrown when neither overload of IsValid has been implemented
        ///     by a derived class.
        /// </exception>
        public virtual bool IsValid(object? value)
        {
            if (!_hasBaseIsValid)
            {
                // track that this method overload has not been overridden.
                _hasBaseIsValid = true;
            }

            // call overridden method.
            // The IsValid method without a validationContext predates the one accepting the context.
            // This is theoretically unreachable through normal use cases.
            // Instead, the overload using validationContext should be called.
            return IsValid(value, null!) == ValidationResult.Success;
        }

        /// <summary>
        ///     Protected virtual method to override and implement validation logic.
        ///     <para>
        ///         Derived classes should override this method instead of <see cref="IsValid(object)" />, which is deprecated.
        ///     </para>
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides
        ///     context about the validation operation, such as the object and member being validated.
        /// </param>
        /// <returns>
        ///     When validation is valid, <see cref="ValidationResult.Success" />.
        ///     <para>
        ///         When validation is invalid, an instance of <see cref="ValidationResult" />.
        ///     </para>
        /// </returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        /// <exception cref="NotImplementedException">
        ///     is thrown when <see cref="IsValid(object, ValidationContext)" />
        ///     has not been implemented by a derived class.
        /// </exception>
        protected virtual ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (_hasBaseIsValid)
            {
                // this means neither of the IsValid methods has been overridden, throw.
                throw NotImplemented.ByDesignWithMessage(
                    SR.ValidationAttribute_IsValid_NotImplemented);
            }

            var result = ValidationResult.Success;

            // call overridden method.
            if (!IsValid(value))
            {
                string[]? memberNames = validationContext.MemberName is { } memberName
                    ? new[] { memberName }
                    : null;
                result = new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
            }

            return result;
        }

        /// <summary>
        ///     Tests whether the given <paramref name="value" /> is valid with respect to the current
        ///     validation attribute without throwing a <see cref="ValidationException" />
        /// </summary>
        /// <remarks>
        ///     If this method returns <see cref="ValidationResult.Success" />, then validation was successful, otherwise
        ///     an instance of <see cref="ValidationResult" /> will be returned with a guaranteed non-null
        ///     <see cref="ValidationResult.ErrorMessage" />.
        /// </remarks>
        /// <param name="value">The value to validate</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides
        ///     context about the validation operation, such as the object and member being validated.
        /// </param>
        /// <returns>
        ///     When validation is valid, <see cref="ValidationResult.Success" />.
        ///     <para>
        ///         When validation is invalid, an instance of <see cref="ValidationResult" />.
        ///     </para>
        /// </returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="validationContext" /> is null.</exception>
        /// <exception cref="NotImplementedException">
        ///     is thrown when <see cref="IsValid(object, ValidationContext)" />
        ///     has not been implemented by a derived class.
        /// </exception>
        public ValidationResult? GetValidationResult(object? value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            var result = IsValid(value, validationContext);

            // If validation fails, we want to ensure we have a ValidationResult that guarantees it has an ErrorMessage
            if (result != null)
            {
                if (string.IsNullOrEmpty(result.ErrorMessage))
                {
                    var errorMessage = FormatErrorMessage(validationContext.DisplayName);
                    result = new ValidationResult(errorMessage, result.MemberNames);
                }
            }

            return result;
        }

        /// <summary>
        ///     Validates the specified <paramref name="value" /> and throws <see cref="ValidationException" /> if it is not.
        ///     <para>
        ///         The overloaded <see cref="Validate(object, ValidationContext)" /> is the recommended entry point as it
        ///         can provide additional context to the <see cref="ValidationAttribute" /> being validated.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     This base method invokes the <see cref="IsValid(object)" /> method to determine whether or not the
        ///     <paramref name="value" /> is acceptable.  If <see cref="IsValid(object)" /> returns <c>false</c>, this base
        ///     method will invoke the <see cref="FormatErrorMessage" /> to obtain a localized message describing
        ///     the problem, and it will throw a <see cref="ValidationException" />
        /// </remarks>
        /// <param name="value">The value to validate</param>
        /// <param name="name">The string to be included in the validation error message if <paramref name="value" /> is not valid</param>
        /// <exception cref="ValidationException">
        ///     is thrown if <see cref="IsValid(object)" /> returns <c>false</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        public void Validate(object? value, string name)
        {
            if (!IsValid(value))
            {
                throw new ValidationException(FormatErrorMessage(name), this, value);
            }
        }

        /// <summary>
        ///     Validates the specified <paramref name="value" /> and throws <see cref="ValidationException" /> if it is not.
        /// </summary>
        /// <remarks>
        ///     This method invokes the <see cref="IsValid(object, ValidationContext)" /> method
        ///     to determine whether or not the <paramref name="value" /> is acceptable given the
        ///     <paramref name="validationContext" />.
        ///     If that method doesn't return <see cref="ValidationResult.Success" />, this base method will throw
        ///     a <see cref="ValidationException" /> containing the <see cref="ValidationResult" /> describing the problem.
        /// </remarks>
        /// <param name="value">The value to validate</param>
        /// <param name="validationContext">Additional context that may be used for validation.  It cannot be null.</param>
        /// <exception cref="ValidationException">
        ///     is thrown if <see cref="IsValid(object, ValidationContext)" />
        ///     doesn't return <see cref="ValidationResult.Success" />.
        /// </exception>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        /// <exception cref="NotImplementedException">
        ///     is thrown when <see cref="IsValid(object, ValidationContext)" />
        ///     has not been implemented by a derived class.
        /// </exception>
        public void Validate(object? value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            ValidationResult? result = GetValidationResult(value, validationContext);

            if (result != null)
            {
                // Convenience -- if implementation did not fill in an error message,
                throw new ValidationException(result, this, value);
            }
        }
        #endregion
    }
}
