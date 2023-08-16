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
}
