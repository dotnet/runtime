// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal sealed record ComInterfaceContext(ComInterfaceInfo Info, ComInterfaceContext? Base)
    {
        /// <summary>
        /// Takes a list of ComInterfaceInfo, and creates a list of ComInterfaceContext.
        /// </summary>
        public static ImmutableArray<(ComInterfaceContext? Context, Diagnostic? Diagnostic)> GetContexts(ImmutableArray<ComInterfaceInfo> data, CancellationToken _)
        {
            Dictionary<string, ComInterfaceInfo> keyToInterfaceInfoMap = new();
            var accumulator = ImmutableArray.CreateBuilder<(ComInterfaceContext? Context, Diagnostic? Diagnostic)>(data.Length);
            foreach (var iface in data)
            {
                keyToInterfaceInfoMap.Add(iface.ThisInterfaceKey, iface);
            }
            Dictionary<string, ComInterfaceContext> symbolToContextMap = new();

            foreach (var iface in data)
            {
                accumulator.Add(AddContext(iface));
            }
            return accumulator.MoveToImmutable();

            (ComInterfaceContext? Context, Diagnostic? Diagnostic) AddContext(ComInterfaceInfo iface)
            {
                if (symbolToContextMap.TryGetValue(iface.ThisInterfaceKey, out var cachedValue))
                {
                    return (cachedValue, null);
                }

                if (iface.BaseInterfaceKey is null)
                {
                    var baselessCtx = new ComInterfaceContext(iface, null);
                    symbolToContextMap[iface.ThisInterfaceKey] = baselessCtx;
                    return (baselessCtx, null);
                }

                if (!symbolToContextMap.TryGetValue(iface.BaseInterfaceKey, out var baseContext))
                {
                    if(!keyToInterfaceInfoMap.TryGetValue(iface.BaseInterfaceKey, out var baseInfo))
                    {
                        //Diagnostic that there is an issue with the base, so the interface cannot be
                        return (null,
                            Diagnostic.Create(
                                GeneratorDiagnostics.BaseInterfaceIsNotGenerated,
                                iface.DiagnosticLocation.AsLocation()));

                    }
                    (baseContext, var baseDiag) = AddContext(baseInfo);
                }
                var ctx = new ComInterfaceContext(iface, baseContext);
                symbolToContextMap[iface.ThisInterfaceKey] = ctx;
                return (ctx, null);
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
