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
        public static ImmutableArray<DiagnosticOr<ComInterfaceContext>> GetContexts(ImmutableArray<ComInterfaceInfo> data, CancellationToken _)
        {
            Dictionary<string, ComInterfaceInfo> nameToInterfaceInfoMap = new();
            var accumulator = ImmutableArray.CreateBuilder<DiagnosticOr<ComInterfaceContext>>(data.Length);
            foreach (var iface in data)
            {
                nameToInterfaceInfoMap.Add(iface.ThisInterfaceKey, iface);
            }
            Dictionary<string, DiagnosticOr<ComInterfaceContext>> nameToContextCache = new();

            foreach (var iface in data)
            {
                accumulator.Add(AddContext(iface));
            }
            return accumulator.MoveToImmutable();

            DiagnosticOr<ComInterfaceContext> AddContext(ComInterfaceInfo iface)
            {
                if (nameToContextCache.TryGetValue(iface.ThisInterfaceKey, out var cachedValue))
                {
                    return cachedValue;
                }

                if (iface.BaseInterfaceKey is null)
                {
                    var baselessCtx = DiagnosticOr<ComInterfaceContext>.From(new ComInterfaceContext(iface, null));
                    nameToContextCache[iface.ThisInterfaceKey] = baselessCtx;
                    return baselessCtx;
                }

                DiagnosticOr<ComInterfaceContext> baseReturnedValue;
                if (
                    // Cached base info is a diagnostic - failure
                    (nameToContextCache.TryGetValue(iface.BaseInterfaceKey, out var baseCachedValue) && baseCachedValue.IsDiagnostic)
                    // Cannot find base ComInterfaceInfo - failure (failed ComInterfaceInfo creation)
                    || !nameToInterfaceInfoMap.TryGetValue(iface.BaseInterfaceKey, out var baseInfo)
                    // Newly calculated base context pair is a diagnostic - failure
                    || (baseReturnedValue = AddContext(baseInfo)).IsDiagnostic)
                {
                    // The base has failed generation at some point, so this interface cannot be generated
                    var diagnostic = DiagnosticOr<ComInterfaceContext>.From(
                        Diagnostic.Create(
                            GeneratorDiagnostics.BaseInterfaceIsNotGenerated,
                            iface.DiagnosticLocation.AsLocation(), iface.ThisInterfaceKey, iface.BaseInterfaceKey));
                    nameToContextCache[iface.ThisInterfaceKey] = diagnostic;
                    return diagnostic;
                }
                DiagnosticOr<ComInterfaceContext> baseContext = baseCachedValue ?? baseReturnedValue;
                Debug.Assert(baseContext.IsValue);
                var ctx = DiagnosticOr<ComInterfaceContext>.From(new ComInterfaceContext(iface, baseContext.Value));
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
