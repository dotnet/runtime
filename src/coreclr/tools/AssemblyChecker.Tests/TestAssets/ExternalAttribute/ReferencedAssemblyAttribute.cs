// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace AssemblyChecker.Tests.ExternalAttribute;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ReferencedAssemblyAttribute : Attribute
{
}
