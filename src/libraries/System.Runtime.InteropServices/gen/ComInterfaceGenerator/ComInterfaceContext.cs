// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal sealed record ComInterfaceContext(ComInterfaceInfo Info, ComInterfaceContext? Base)
    {
        /// <summary>
        /// Takes a list of ComInterfaceInfo, and creates a list of ComInterfaceContext.
        /// </summary>
        public static ImmutableArray<(ComInterfaceContext? Context, Diagnostic? Diagnostic)> GetContexts(ImmutableArray<ComInterfaceInfo> data, CancellationToken _)
        {
            Dictionary<string, ComInterfaceInfo> nameToInterfaceInfoMap = new();
            var accumulator = ImmutableArray.CreateBuilder<(ComInterfaceContext? Context, Diagnostic? Diagnostic)>(data.Length);
            foreach (var iface in data)
            {
                nameToInterfaceInfoMap.Add(iface.ThisInterfaceKey, iface);
            }
            Dictionary<string, (ComInterfaceContext? Context, Diagnostic? Diagnostic)> nameToContextCache = new();

            foreach (var iface in data)
            {
                accumulator.Add(AddContext(iface));
            }
            return accumulator.MoveToImmutable();

            (ComInterfaceContext? Context, Diagnostic? Diagnostic) AddContext(ComInterfaceInfo iface)
            {
                if (nameToContextCache.TryGetValue(iface.ThisInterfaceKey, out var cachedValue))
                {
                    return cachedValue;
                }

                if (iface.BaseInterfaceKey is null)
                {
                    var baselessCtx = new ComInterfaceContext(iface, null);
                    nameToContextCache[iface.ThisInterfaceKey] = (baselessCtx, null);
                    return (baselessCtx, null);
                }

                if (
                    // Cached base info has a diagnostic - failure
                    (nameToContextCache.TryGetValue(iface.BaseInterfaceKey, out var basePair) && basePair.Diagnostic is not null)
                    // Cannot find base ComInterfaceInfo - failure (failed ComInterfaceInfo creation)
                    || !nameToInterfaceInfoMap.TryGetValue(iface.BaseInterfaceKey, out var baseInfo)
                    // Newly calculated base context pair has a diagnostic - failure
                    || (AddContext(baseInfo) is { } baseReturnPair && baseReturnPair.Diagnostic is not null))
                {
                    // The base has failed generation at some point, so this interface cannot be generated
                    (ComInterfaceContext, Diagnostic?) diagnosticPair = (null,
                        Diagnostic.Create(
                            GeneratorDiagnostics.BaseInterfaceIsNotGenerated,
                            iface.DiagnosticLocation.AsLocation(), iface.ThisInterfaceKey, iface.BaseInterfaceKey));
                    nameToContextCache[iface.ThisInterfaceKey] = diagnosticPair;
                    return diagnosticPair;
                }
                var baseContext = basePair.Context ?? baseReturnPair.Context;
                Debug.Assert(baseContext != null);
                var ctx = new ComInterfaceContext(iface, baseContext);
                (ComInterfaceContext, Diagnostic?) contextPair = (ctx, null);
                nameToContextCache[iface.ThisInterfaceKey] = contextPair;
                return contextPair;
            }
        }

        internal ComInterfaceContext GetTopLevelBase()
        {
            var currBase = Base;
            while (currBase is not null)
                currBase = currBase.Base;
            return currBase;
        }
    }
}
