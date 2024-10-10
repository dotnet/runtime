// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Generators
{
    public partial class EventSourceEventGenerator
    {
        private const string Category = "System.Diagnostics.Tracing";

        private static DiagnosticDescriptor EventSourceNoSupportType { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB2000", //I don't know what id need I set
            title: new LocalizableResourceString(nameof(SR.EventSourceGeneratorTitle), SR.ResourceManager, typeof(FxResources.System.Private.CoreLib.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.EventSourceNoSupportType), SR.ResourceManager, typeof(FxResources.System.Private.CoreLib.Generators.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);

        private static DiagnosticDescriptor ContextClassesMustBePartial { get; } = DiagnosticDescriptorHelper.Create(
            id: "SYSLIB2001", //I don't know what id need I set
            title: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialTitle), SR.ResourceManager, typeof(FxResources.System.Private.CoreLib.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialMessageFormat), SR.ResourceManager, typeof(FxResources.System.Private.CoreLib.Generators.SR)),
            category: Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.NotConfigurable);
    }
}
