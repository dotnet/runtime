// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions;

/// <summary>
/// This attribute is used to disable removing il verification errors that appear in the input assembly from the output verification results.
///
/// The original motivation for this is to make it easier to write a test that mostly verifies that the test frameworks ability to check il is working
/// correctly.
/// </summary>
[AttributeUsage (AttributeTargets.Class)]
public class DisableILVerifyDiffingAttribute : BaseExpectedLinkedBehaviorAttribute
{
}
