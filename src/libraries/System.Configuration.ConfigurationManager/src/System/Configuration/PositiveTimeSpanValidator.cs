// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    public class PositiveTimeSpanValidator : ConfigurationValidatorBase
    {
        public override bool CanValidate(Type type)
        {
            return type == typeof(TimeSpan);
        }

        public override void Validate(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if ((TimeSpan)value <= TimeSpan.Zero)
                throw new ArgumentException(SR.Validator_timespan_value_must_be_positive);
        }
    }
}
