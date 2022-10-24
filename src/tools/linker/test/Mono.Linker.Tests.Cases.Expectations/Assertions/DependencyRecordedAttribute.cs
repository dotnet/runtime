// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DependencyRecordedAttribute : BaseExpectedLinkedBehaviorAttribute
    {
        public DependencyRecordedAttribute(string source, string target, string marked = null)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Value cannot be null or empty.", nameof(source));

            if (string.IsNullOrEmpty(target))
                throw new ArgumentException("Value cannot be null or empty.", nameof(target));
        }
    }
}
