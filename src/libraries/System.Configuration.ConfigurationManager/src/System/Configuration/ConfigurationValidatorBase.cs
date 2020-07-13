// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    public abstract class ConfigurationValidatorBase
    {
        public virtual bool CanValidate(Type type)
        {
            return false;
        }

        public abstract void Validate(object value);
    }
}
