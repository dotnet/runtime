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
        public static ImmutableArray<DiagnosticsOr<ComInterfaceContext>> GetContexts(ImmutableArray<ComInterfaceInfo> data, CancellationToken _)
        {
            Dictionary<string, ComInterfaceInfo> nameToInterfaceInfoMap = new();
            var accumulator = ImmutableArray.CreateBuilder<DiagnosticsOr<ComInterfaceContext>>(data.Length);
            foreach (var iface in data)
            {
                nameToInterfaceInfoMap.Add(iface.ThisInterfaceKey, iface);
            }
            Dictionary<string, DiagnosticsOr<ComInterfaceContext>> nameToContextCache = new();

            foreach (var iface in data)
            {
                accumulator.Add(AddContext(iface));
            }
            return accumulator.MoveToImmutable();

            DiagnosticsOr<ComInterfaceContext> AddContext(ComInterfaceInfo iface)
            {
                if (nameToContextCache.TryGetValue(iface.ThisInterfaceKey, out var cachedValue))
                {
                    return cachedValue;
                }

                if (iface.BaseInterfaceKey is null)
                {
                    var baselessCtx = DiagnosticsOr<ComInterfaceContext>.From(new ComInterfaceContext(iface, null));
                    nameToContextCache[iface.ThisInterfaceKey] = baselessCtx;
                    return baselessCtx;
                }

                DiagnosticsOr<ComInterfaceContext> baseReturnedValue;
                if (
                    // Cached base info is a diagnostic - failure
                    (nameToContextCache.TryGetValue(iface.BaseInterfaceKey, out var baseCachedValue) && baseCachedValue.HasDiagnostic)
                    // Cannot find base ComInterfaceInfo - failure (failed ComInterfaceInfo creation)
                    || !nameToInterfaceInfoMap.TryGetValue(iface.BaseInterfaceKey, out var baseInfo)
                    // Newly calculated base context pair is a diagnostic - failure
                    || (baseReturnedValue = AddContext(baseInfo)).HasDiagnostic)
                {
                    // The base has failed generation at some point, so this interface cannot be generated
                    var diagnostic = DiagnosticOr<ComInterfaceContext>.From(
                        DiagnosticInfo.Create(
                            GeneratorDiagnostics.BaseInterfaceIsNotGenerated,
                            iface.DiagnosticLocation, iface.ThisInterfaceKey, iface.BaseInterfaceKey));
                    nameToContextCache[iface.ThisInterfaceKey] = diagnostic;
                    return diagnostic;
                }
                DiagnosticsOr<ComInterfaceContext> baseContext = baseCachedValue ?? baseReturnedValue;
                Debug.Assert(baseContext.HasValue);
                var ctx = DiagnosticsOr<ComInterfaceContext>.From(new ComInterfaceContext(iface, baseContext.Value));
                nameToContextCache[iface.ThisInterfaceKey] = ctx;
                return ctx;
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
