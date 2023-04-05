// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
namespace Microsoft.Interop.Analyzers
{
    public static class AnalyzerDiagnostics
    {
        public static class Ids
        {
            public const string Prefix = "SYSLIB";
            public const string InvalidGeneratedComAttributeUsage = Prefix + "1090";
        }

        private const string Category = "ComInterfaceGenerator";

        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));
        }

        public static readonly DiagnosticDescriptor InterfaceTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComAttributeUsage,
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedTitle)),
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)));
    }
}
