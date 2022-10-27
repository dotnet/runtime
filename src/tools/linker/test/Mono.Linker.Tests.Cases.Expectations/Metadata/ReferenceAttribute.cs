// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ReferenceAttribute : BaseMetadataAttribute
    {

        public ReferenceAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
        }
    }
}
