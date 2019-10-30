// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurationValidatorAttribute : Attribute
    {
        internal Type _declaringType;

        protected ConfigurationValidatorAttribute() { }

        public ConfigurationValidatorAttribute(Type validator)
        {
            if (validator == null) throw new ArgumentNullException(nameof(validator));

            if (!typeof(ConfigurationValidatorBase).IsAssignableFrom(validator))
            {
                throw new ArgumentException(SR.Format(SR.Validator_Attribute_param_not_validator,
                    "ConfigurationValidatorBase"));
            }

            ValidatorType = validator;
        }

        public virtual ConfigurationValidatorBase ValidatorInstance
            => (ConfigurationValidatorBase)TypeUtil.CreateInstance(ValidatorType);

        public Type ValidatorType { get; }

        // Used for limiting the visibility of types that can be accessed in the reflection
        // call made by the ValidatorInstance property getter. This will normally be the
        // type that declared the attribute, but in certain cases it could be a subclass
        // of the type that declared the attribute. This should be ok from a security
        // perspective, as one wouldn't reasonably expect a derived type to have fewer
        // security constraints than its base type.
        internal void SetDeclaringType(Type declaringType)
        {
            if (declaringType == null)
            {
                Debug.Fail("Declaring type must not be null.");
                return; // don't throw in an in-place update
            }

            if (_declaringType == null)
            {
                // First call to this method - allow any type to be set
                _declaringType = declaringType;
            }
            else
            {
                Debug.Assert(_declaringType == declaringType,
                    "Subsequent calls cannot change the declaring type of the attribute.");
            }
        }
    }
}
