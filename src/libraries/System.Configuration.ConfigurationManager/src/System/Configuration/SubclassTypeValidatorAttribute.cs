// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SubclassTypeValidatorAttribute : ConfigurationValidatorAttribute
    {
        public SubclassTypeValidatorAttribute(Type baseClass)
        {
            BaseClass = baseClass;
        }

        public override ConfigurationValidatorBase ValidatorInstance => new SubclassTypeValidator(BaseClass);

        public Type BaseClass { get; }
    }
}
