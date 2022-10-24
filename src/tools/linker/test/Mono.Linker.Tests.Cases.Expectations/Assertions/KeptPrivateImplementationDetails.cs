// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class KeptPrivateImplementationDetailsAttribute : KeptAttribute
    {
        public KeptPrivateImplementationDetailsAttribute(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(methodName));
        }
    }
}
