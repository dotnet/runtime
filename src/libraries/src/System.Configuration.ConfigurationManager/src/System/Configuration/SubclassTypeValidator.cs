// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // This class validates that the value is a subclass of a specified type
    public sealed class SubclassTypeValidator : ConfigurationValidatorBase
    {
        private readonly Type _base;

        public SubclassTypeValidator(Type baseClass)
        {
            if (baseClass == null)
                throw new ArgumentNullException(nameof(baseClass));

            _base = baseClass;
        }

        public override bool CanValidate(Type type)
        {
            return type == typeof(Type);
        }

        public override void Validate(object value)
        {
            if (value == null) return;

            // Make a check here since value.GetType() returns RuntimeType rather then Type
            if (!(value is Type)) ValidatorUtils.HelperParamValidation(value, typeof(Type));

            if (!_base.IsAssignableFrom((Type)value))
            {
                throw new ArgumentException(SR.Format(SR.Subclass_validator_error, ((Type)value).FullName,
                    _base.FullName));
            }
        }
    }
}
