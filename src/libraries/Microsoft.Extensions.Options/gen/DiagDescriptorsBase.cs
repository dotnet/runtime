// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Options.Generators
{
    #pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    internal class DiagDescriptorsBase
    #pragma warning restore CA1052
    {
        protected static DiagnosticDescriptor Make(
                string id,
                string title,
                string messageFormat,
                string category,
                DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error,
                bool isEnabledByDefault = true)
        {
            return new(
                id,
                title,
                messageFormat,
                category,
                defaultSeverity,
                isEnabledByDefault);
        }
    }
}
