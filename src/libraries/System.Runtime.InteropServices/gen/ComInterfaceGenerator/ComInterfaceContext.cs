// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.Interop
{
    public sealed partial class ComInterfaceGenerator
    {
        private sealed record ComInterfaceContext(ComInterfaceInfo Info, ComInterfaceContext? Base)
        {
            /// <summary>
            /// Takes a list of ComInterfaceInfo, and creates a list of ComInterfaceContext. Does not guarantee the ordering of the output.
            /// </summary>
            public static IEnumerable<ComInterfaceContext> GetContexts(ImmutableArray<ComInterfaceInfo> data, CancellationToken _)
            {
                Dictionary<string, ComInterfaceInfo> symbolToInterfaceInfoMap = new();
                foreach (var iface in data)
                {
                    symbolToInterfaceInfoMap.Add(iface.ThisInterfaceKey, iface);
                }
                Dictionary<string, ComInterfaceContext> symbolToContextMap = new();

                foreach (var iface in data)
                {
                    yield return AddContext(iface);
                }

                ComInterfaceContext AddContext(ComInterfaceInfo iface)
                {
                    if (symbolToContextMap.TryGetValue(iface.ThisInterfaceKey, out var cachedValue))
                    {
                        return cachedValue;
                    }

                    if (iface.BaseInterfaceKey is null)
                    {
                        var baselessCtx = new ComInterfaceContext(iface, null);
                        symbolToContextMap[iface.ThisInterfaceKey] = baselessCtx;
                        return baselessCtx;
                    }

                    if (!symbolToContextMap.TryGetValue(iface.BaseInterfaceKey, out var baseContext))
                    {
                        baseContext = AddContext(symbolToInterfaceInfoMap[iface.BaseInterfaceKey]);
                    }
                    var ctx = new ComInterfaceContext(iface, baseContext);
                    symbolToContextMap[iface.ThisInterfaceKey] = ctx;
                    return ctx;
                }
            }
        }
    }
}
