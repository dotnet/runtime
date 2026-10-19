// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ExpectedXmlElementAttribute : BaseExpectedLinkedBehaviorAttribute
{
    public ExpectedXmlElementAttribute(string fileName, string xpath, int count = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(xpath);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
    }
}
