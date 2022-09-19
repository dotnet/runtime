// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Validation attribute that executes a user-supplied method at runtime, using one of these signatures:
    ///     <para>
    ///         public static <see cref="ValidationResult" /> Method(object value) { ... }
    ///     </para>
    ///     <para>
    ///         public static <see cref="ValidationResult" /> Method(object value, <see cref="ValidationContext" /> context) {
    ///         ... }
    ///     </para>
    ///     <para>
    ///         The value can be strongly typed as type conversion will be attempted.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     This validation attribute is used to invoke custom logic to perform validation at runtime.
    ///     Like any other <see cref="ValidationAttribute" />, its <see cref="IsValid(object, ValidationContext)" />
    ///     method is invoked to perform validation.  This implementation simply redirects that call to the method
    ///     identified by <see cref="Method" /> on a type identified by <see cref="ValidatorType" />
    ///     <para>
    ///         The supplied <see cref="ValidatorType" /> cannot be null, and it must be a public type.
    ///     </para>
    ///     <para>
    ///         The named <see cref="Method" /> must be public, static, return <see cref="ValidationResult" /> and take at
    ///         least one input parameter for the value to be validated.  This value parameter may be strongly typed.
    ///         Type conversion will be attempted if clients pass in a value of a different type.
    ///     </para>
    ///     <para>
    ///         The <see cref="Method" /> may also declare an additional parameter of type <see cref="ValidationContext" />.
    ///         The <see cref="ValidationContext" /> parameter provides additional context the method may use to determine
    ///         the context in which it is being used.
    ///     </para>
    ///     <para>
    ///         If the method returns <see cref="ValidationResult" />.<see cref="ValidationResult.Success" />, that indicates
    ///         the given value is acceptable and validation passed.
    ///         Returning an instance of <see cref="ValidationResult" /> indicates that the value is not acceptable
    ///         and validation failed.
    ///     </para>
    ///     <para>
    ///         If the method returns a <see cref="ValidationResult" /> with a <c>null</c>
    ///         <see cref="ValidationResult.ErrorMessage" />
    ///         then the normal <see cref="ValidationAttribute.FormatErrorMessage" /> method will be called to compose the
    ///         error message.
    ///     </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method |
        AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class CustomValidationAttribute : ValidationAttribute
    {
        #region Member Fields

        private readonly Lazy<string?> _malformedErrorMessage;
        private bool _isSingleArgumentMethod;
        private string? _lastMessage;
        private MethodInfo? _methodInfo;
        private Type? _firstParameterType;
        private Tuple<string, Type>? _typeId;

        #endregion

        #region All Constructors

        /// <summary>
        ///     Instantiates a custom validation attribute that will invoke a method in the
        ///     specified type.
        /// </summary>
        /// <remarks>
        ///     An invalid <paramref name="validatorType" /> or <paramref name="method" /> will be cause
        ///     <see cref="IsValid(object, ValidationContext)" />> to return a <see cref="ValidationResult" />
        ///     and <see cref="ValidationAttribute.FormatErrorMessage" /> to return a summary error message.
        /// </remarks>
        /// <param name="validatorType">
        ///     The type that will contain the method to invoke.  It cannot be null.  See
        ///     <see cref="Method" />.
        /// </param>
        /// <param name="method">The name of the method to invoke in <paramref name="validatorType" />.</param>
        public CustomValidationAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type validatorType, string method)
            : base(() => SR.CustomValidationAttribute_ValidationError)
        {
            ValidatorType = validatorType;
            Method = method;
            _malformedErrorMessage = new Lazy<string?>(CheckAttributeWellFormed);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the type that contains the validation method identified by <see cref="Method" />.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public Type ValidatorType { get; }

        /// <summary>
        /// Gets a unique identifier for this attribute.
        /// </summary>
        public override object TypeId => _typeId ??= new Tuple<string, Type>(Method, ValidatorType);

        /// <summary>
        ///     Gets the name of the method in <see cref="ValidatorType" /> to invoke to perform validation.
        /// </summary>
        public string Method { get; }

        public override bool RequiresValidationContext
        {
            get
            {
                // If attribute is not valid, throw an exception right away to inform the developer
                ThrowIfAttributeNotWellFormed();
                // We should return true when 2-parameter form of the validation method is used
                return !_isSingleArgumentMethod;
            }
        }
        #endregion

        /// <summary>
        ///     Override of validation method.  See <see cref="ValidationAttribute.IsValid(object, ValidationContext)" />.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">
        ///     A <see cref="ValidationContext" /> instance that provides
        ///     context about the validation operation, such as the object and member being validated.
        /// </param>
        /// <returns>Whatever the <see cref="Method" /> in <see cref="ValidatorType" /> returns.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // If attribute is not valid, throw an exception right away to inform the developer
            ThrowIfAttributeNotWellFormed();

            var methodInfo = _methodInfo;

            // If the value is not of the correct type and cannot be converted, fail
            // to indicate it is not acceptable.  The convention is that IsValid is merely a probe,
            // and clients are not expecting exceptions.
            object? convertedValue;
            if (!TryConvertValue(value, out convertedValue))
            {
                return new ValidationResult(SR.Format(SR.CustomValidationAttribute_Type_Conversion_Failed,
                                            (value != null ? value.GetType().ToString() : "null"),
                                            _firstParameterType,
                                            ValidatorType,
                                            Method));
            }

            // Invoke the method.  Catch TargetInvocationException merely to unwrap it.
            // Callers don't know Reflection is being used and will not typically see
            // the real exception
            try
            {
                // 1-parameter form is ValidationResult Method(object value)
                // 2-parameter form is ValidationResult Method(object value, ValidationContext context),
                var methodParams = _isSingleArgumentMethod
                    ? new object?[] { convertedValue }
                    : new[] { convertedValue, validationContext };

                var result = (ValidationResult?)methodInfo!.Invoke(null, methodParams);

                // We capture the message they provide us only in the event of failure,
                // otherwise we use the normal message supplied via the ctor
                _lastMessage = null;

                if (result != null)
                {
                    _lastMessage = result.ErrorMessage;
                }

                return result;
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException!;
            }
        }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.FormatErrorMessage" />
        /// </summary>
        /// <param name="name">The name to include in the formatted string</param>
        /// <returns>A localized string to describe the problem.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is malformed.</exception>
        public override string FormatErrorMessage(string name)
        {
            // If attribute is not valid, throw an exception right away to inform the developer
            ThrowIfAttributeNotWellFormed();

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                return string.Format(CultureInfo.CurrentCulture, _lastMessage, name);
            }

            // If success or they supplied no custom message, use normal base class behavior
            return base.FormatErrorMessage(name);
        }

        /// <summary>
        ///     Checks whether the current attribute instance itself is valid for use.
        /// </summary>
        /// <returns>The error message why it is not well-formed, null if it is well-formed.</returns>
        private string? CheckAttributeWellFormed() => ValidateValidatorTypeParameter() ?? ValidateMethodParameter();

        /// <summary>
        ///     Internal helper to determine whether <see cref="ValidatorType" /> is legal for use.
        /// </summary>
        /// <returns><c>null</c> or the appropriate error message.</returns>
        private string? ValidateValidatorTypeParameter()
        {
            if (ValidatorType == null)
            {
                return SR.CustomValidationAttribute_ValidatorType_Required;
            }

            if (!ValidatorType.IsVisible)
            {
                return SR.Format(SR.CustomValidationAttribute_Type_Must_Be_Public, ValidatorType.Name);
            }

            return null;
        }

        /// <summary>
        ///     Internal helper to determine whether <see cref="Method" /> is legal for use.
        /// </summary>
        /// <returns><c>null</c> or the appropriate error message.</returns>
        private string? ValidateMethodParameter()
        {
            if (string.IsNullOrEmpty(Method))
            {
                return SR.CustomValidationAttribute_Method_Required;
            }

            // Named method must be public and static
            var methodInfo = ValidatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(m => string.Equals(m.Name, Method, StringComparison.Ordinal));
            if (methodInfo == null)
            {
                return SR.Format(SR.CustomValidationAttribute_Method_Not_Found, Method, ValidatorType.Name);
            }

            // Method must return a ValidationResult or derived class
            if (!typeof(ValidationResult).IsAssignableFrom(methodInfo.ReturnType))
            {
                return SR.Format(SR.CustomValidationAttribute_Method_Must_Return_ValidationResult, Method, ValidatorType.Name);
            }

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            // Must declare at least one input parameter for the value and it cannot be ByRef
            if (parameterInfos.Length == 0 || parameterInfos[0].ParameterType.IsByRef)
            {
                return SR.Format(SR.CustomValidationAttribute_Method_Signature, Method, ValidatorType.Name);
            }

            // We accept 2 forms:
            // 1-parameter form is ValidationResult Method(object value)
            // 2-parameter form is ValidationResult Method(object value, ValidationContext context),
            _isSingleArgumentMethod = (parameterInfos.Length == 1);

            if (!_isSingleArgumentMethod)
            {
                if ((parameterInfos.Length != 2) || (parameterInfos[1].ParameterType != typeof(ValidationContext)))
                {
                    return SR.Format(SR.CustomValidationAttribute_Method_Signature, Method, ValidatorType.Name);
                }
            }

            _methodInfo = methodInfo;
            _firstParameterType = parameterInfos[0].ParameterType;
            return null;
        }

        /// <summary>
        ///     Throws InvalidOperationException if the attribute is not valid.
        /// </summary>
        private void ThrowIfAttributeNotWellFormed()
        {
            string? errorMessage = _malformedErrorMessage.Value;
            if (errorMessage != null)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        ///     Attempts to convert the given value to the type needed to invoke the method for the current
        ///     CustomValidationAttribute.
        /// </summary>
        /// <param name="value">The value to check/convert.</param>
        /// <param name="convertedValue">If successful, the converted (or copied) value.</param>
        /// <returns><c>true</c> if type value was already correct or was successfully converted.</returns>
        private bool TryConvertValue(object? value, out object? convertedValue)
        {
            convertedValue = null;
            var expectedValueType = _firstParameterType!;

            // Null is permitted for reference types or for Nullable<>'s only
            if (value == null)
            {
                if (expectedValueType.IsValueType
                    && (!expectedValueType.IsGenericType
                        || expectedValueType.GetGenericTypeDefinition() != typeof(Nullable<>)))
                {
                    return false;
                }

                return true; // convertedValue already null, which is correct for this case
            }

            // If the type is already legally assignable, we're good
            if (expectedValueType.IsInstanceOfType(value))
            {
                convertedValue = value;
                return true;
            }

            // Value is not the right type -- attempt a convert.
            // Any expected exception returns a false
            try
            {
                convertedValue = Convert.ChangeType(value, expectedValueType, CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
