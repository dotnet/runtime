// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// A base class for attributes that make assertions about a particular member.
    // The test infrastructure is expected to check the assertion on the member to which
    // the attribute is applied.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Delegate, AllowMultiple = true)]
    public abstract class BaseMemberAssertionAttribute : Attribute
    {
    }
}
