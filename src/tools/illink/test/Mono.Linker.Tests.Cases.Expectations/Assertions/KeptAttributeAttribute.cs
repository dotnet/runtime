// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            ArgumentNullException.ThrowIfNull(type);
        }

        /// <summary>
        /// Use this constructor when you want to explicitly verify that an attribute with specific parameters survived.
        ///
        /// This is useful when you have a test that has multiple attributes of the same type but passed each is passed different parameters
        /// and you want to verify which one(s) survived
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        public KeptAttributeAttribute(Type type, params object[] args)
        {
            ArgumentNullException.ThrowIfNull(type);
        }
    }
}
