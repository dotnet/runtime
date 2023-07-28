// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace XUnitWrapperGenerator;

public static class Descriptors
{
    public static readonly DiagnosticDescriptor XUWG1001 =
        new DiagnosticDescriptor(
            "XUW1001",
            "Projects in merged tests group should not have entry points",
            "Projects in merged tests group should not have entry points. Convert to Facts or Theories.",
            "XUnitWrapperGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor XUWG1002 =
        new DiagnosticDescriptor(
            "XUW1002",
            "Tests should not unconditionally return 100",
            "Tests should not unconditionally return 100. Convert to a void return.",
            "XUnitWrapperGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor XUWG1003 =
        new DiagnosticDescriptor(
            "XUW1003",
            "Test methods must be public",
            "Test methods must be public. Add or change the visibility modifier of the test method to public.",
            "XUnitWrapperGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor XUWG1004 =
        new DiagnosticDescriptor(
            "XUW1004",
            "Test classes must be public",
            "Test classes must be public. Add or change the visibility modifier of the test class to public.",
            "XUnitWrapperGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
