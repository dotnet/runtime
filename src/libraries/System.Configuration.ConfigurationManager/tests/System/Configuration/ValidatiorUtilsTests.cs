// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class ValidatiorUtilsTests
    {
        [Theory,
            InlineData(@"a", typeof(string), false),
            InlineData(null, typeof(string), false),
            InlineData(@"a", typeof(int), true),
            InlineData(1, typeof(string), true),
            ]
        public void HelperParamValidation(object value, Type allowedType, bool shouldThrow)
        {
            Action action = () => ValidatorUtils.HelperParamValidation(value, allowedType);
            if (!shouldThrow)
            {
                action();
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("", action);
            }
        }

        [Theory,
            // Exclusive in range
            InlineData(1, 1, 1, 1, true, true, "Validation_scalar_range_violation_not_different"),
            InlineData(1, 1, 2, 1, true, true, "Validation_scalar_range_violation_not_outside_range"),
            InlineData(2, 1, 2, 1, true, true, "Validation_scalar_range_violation_not_outside_range"),
            // Not exclusive in range
            InlineData(1, 1, 1, 1, false, false, null),
            InlineData(1, 1, 2, 1, false, false, null),
            InlineData(2, 1, 2, 1, false, false, null),
            // Exclusive out of range
            InlineData(2, 1, 1, 1, true, false, null),
            InlineData(3, 1, 2, 1, true, false, null),
            InlineData(3, 1, 2, 1, true, false, null),
            // Not exclusive out of range
            InlineData(2, 1, 1, 1, false, true, "Validation_scalar_range_violation_not_equal"),
            InlineData(3, 1, 2, 1, false, true, "Validation_scalar_range_violation_not_in_range"),
            InlineData(3, 1, 2, 1, false, true, "Validation_scalar_range_violation_not_in_range")
            ]
        public void ValidateIntScalar(int value, int min, int max, int resolution, bool exclusiveRange, bool shouldThrow, string message)
        {
            Action action = () => ValidatorUtils.ValidateScalar(value, min, max, resolution, exclusiveRange);
            if (!shouldThrow)
            {
                action();
            }
            else
            {
                Assert.Equal(
                    string.Format(SR.GetResourceString(message, null), min, max),
                    AssertExtensions.Throws<ArgumentException>(null, action).Message);
            }
        }

        [Theory,
            InlineData(1, 1, 1, 1, false, false, null),
            InlineData(1, 1, 1, 2, false, true, "Validator_scalar_resolution_violation")
            ]
        public void ValidateIntBadResolution(int value, int min, int max, int resolution, bool exclusiveRange, bool shouldThrow, string message)
        {
            Action action = () => ValidatorUtils.ValidateScalar(value, min, max, resolution, exclusiveRange);
            if (!shouldThrow)
            {
                action();
            }
            else
            {
                Assert.Equal(
                    string.Format(SR.GetResourceString(message, null), resolution),
                    AssertExtensions.Throws<ArgumentException>(null, action).Message);
            }
        }
    }
}
