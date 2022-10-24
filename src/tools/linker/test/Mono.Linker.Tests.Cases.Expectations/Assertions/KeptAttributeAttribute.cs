// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class KeptAttributeAttribute : KeptAttribute
    {

        public KeptAttributeAttribute(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(attributeName));
        }

        public KeptAttributeAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
        }
    }
}
