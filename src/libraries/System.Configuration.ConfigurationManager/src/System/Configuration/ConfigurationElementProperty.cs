// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    // Although in Whidbey this class contains just one property, but we still do it this way,
    // instead of exposing a Validator property in ConfigurationElement, because if we need
    // another property in the future we'll expand this ElementProperty class rather than adding a
    // new overridable on ConfigurationElement
    public sealed class ConfigurationElementProperty
    {
        public ConfigurationElementProperty(ConfigurationValidatorBase validator)
        {
            if (validator is null)
            {
                throw new ArgumentNullException(nameof(validator));
            }

            Validator = validator;
        }

        public ConfigurationValidatorBase Validator { get; }
    }
}
