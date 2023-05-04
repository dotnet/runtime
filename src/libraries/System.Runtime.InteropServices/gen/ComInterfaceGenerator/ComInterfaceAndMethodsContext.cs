// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed partial class ComInterfaceGenerator
    {
        /// <summary>
        /// Represents an interface and all of the methods that need to be generated for it (methods declared on the interface and methods inherited from base interfaces).
        /// </summary>
        private sealed record ComInterfaceAndMethodsContext(ComInterfaceContext Interface, SequenceEqualImmutableArray<ComMethodContext> Methods, SequenceEqualImmutableArray<ComMethodContext> ShadowingMethods)
        {
            /// <summary>
            /// COM methods that are declared on the attributed interface declaration.
            /// </summary>
            public IEnumerable<ComMethodContext> DeclaredMethods => Methods.Where((m => m.DeclaringInterface == Interface));

            /// <summary>
            /// COM methods that are declared on an interface the interface inherits from.
            /// </summary>
            public IEnumerable<ComMethodContext> InheritedMethods => Methods.Where(m => m.DeclaringInterface != Interface);

            //internal static ComInterfaceAndMethodsContext From((ComInterfaceContext, SequenceEqualImmutableArray<ComMethodContext>) data, CancellationToken _)
            //    => new ComInterfaceAndMethodsContext(data.Item1, data.Item2);

            public static IEnumerable<ComInterfaceAndMethodsContext> CalculateAllMethods(ValueEqualityImmutableDictionary<ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>> ifaceToDeclaredMethodsMap, StubEnvironment environment, CancellationToken ct)
            {
                Dictionary<ComInterfaceContext, (ImmutableArray<ComMethodContext> Methods, ImmutableArray<ComMethodContext> ShadowingMethods)> allMethodsCache = new();

                foreach (var kvp in ifaceToDeclaredMethodsMap)
                {
                    AddMethods(kvp.Key, kvp.Value);
                }

                return allMethodsCache.Select(kvp => new ComInterfaceAndMethodsContext(kvp.Key, kvp.Value.Methods.ToSequenceEqual(), kvp.Value.ShadowingMethods.ToSequenceEqualImmutableArray()));

                (ImmutableArray<ComMethodContext> Methods, ImmutableArray<ComMethodContext> ShadowingMethods) AddMethods(ComInterfaceContext iface, IEnumerable<ComMethodInfo> declaredMethods)
                {
                    if (allMethodsCache.TryGetValue(iface, out var cachedValue))
                    {
                        return cachedValue;
                    }

                    int startingIndex = 3;
                    List<ComMethodContext> methods = new();
                    // If we have a base interface, we should add the inherited methods to our list in vtable order
                    if (iface.Base is not null)
                    {
                        var baseComIface = iface.Base;
                        ImmutableArray<ComMethodContext> baseMethods;
                        if (!allMethodsCache.TryGetValue(baseComIface, out var pair))
                        {
                            baseMethods = AddMethods(baseComIface, ifaceToDeclaredMethodsMap[baseComIface]).Methods;
                        }
                        else
                        {
                            baseMethods = pair.Methods;
                        }
                        methods.AddRange(baseMethods);
                    }
                    var shadowingMethods = methods.Select(method =>
                    {
                        var info = method.MethodInfo;
                        var ctx = CalculateStubInformation(info.Syntax, info.Symbol, startingIndex, environment, iface.Info.Type, ct);
                        return new ComMethodContext(iface, info, startingIndex++, ctx);
                    }).ToImmutableArray();
                    // Then we append the declared methods in vtable order
                    foreach (var method in declaredMethods)
                    {
                        var ctx = CalculateStubInformation(method.Syntax, method.Symbol, startingIndex, environment, iface.Info.Type, ct);
                        methods.Add(new ComMethodContext(iface, method, startingIndex++, ctx));
                    }
                    // Cache so we don't recalculate if many interfaces inherit from the same one
                    var finalPair = (methods.ToImmutableArray(), shadowingMethods);
                    allMethodsCache[iface] = finalPair;
                    return finalPair;
                }
            }
        }
    }
}
