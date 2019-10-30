// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // Default configuration validator
    // Can validate everything and never complains
    public sealed class DefaultValidator : ConfigurationValidatorBase
    {
        public override bool CanValidate(Type type)
        {
            return true;
        }

        public override void Validate(object value)
        {
            // Everything is OK with this validator
        }
    }
}