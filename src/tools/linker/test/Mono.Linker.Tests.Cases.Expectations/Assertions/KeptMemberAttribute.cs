// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public sealed class KeptMemberAttribute : KeptAttribute
    {

        public KeptMemberAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Value cannot be null or empty.", nameof(name));
        }
    }
}
