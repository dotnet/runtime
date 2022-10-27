// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, AllowMultiple = true, Inherited = false)]
    public class AddedPseudoAttributeAttribute : BaseExpectedLinkedBehaviorAttribute
    {
        public AddedPseudoAttributeAttribute(uint value)
        {
        }
    }
}
