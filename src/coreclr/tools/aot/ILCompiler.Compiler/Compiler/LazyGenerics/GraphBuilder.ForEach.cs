// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private sealed partial class GraphBuilder
        {
            /// <summary>
            /// Walk through the type expression and find any embedded generic parameter references. For each one found,
            /// invoke the collector delegate with that generic parameter and a boolean indicate whether this is
            /// a proper embedding (i.e. there is something actually nesting this.)
            ///
            /// Typically, the type expression is something that a generic type formal is being bound to, and we're
            /// looking to see if another other generic type formals are referenced within that type expression.
            ///
            /// This method also records bindings for any generic instances it finds inside the tree expression.
            /// Sometimes, this side-effect is all that's wanted - in such cases, invoke this method with a null collector.
            /// </summary>
            private void ForEachEmbeddedGenericFormal(TypeDesc typeExpression, Instantiation typeContext, Instantiation methodContext, System.Action<EcmaGenericParameter, bool> collector = null)
            {
                System.Action<EcmaGenericParameter, int> wrappedCollector =
                    delegate(EcmaGenericParameter embedded, int depth)
                    {
                        bool isProperEmbedding = (depth > 0);
                        collector?.Invoke(embedded, isProperEmbedding);
                        return;
                    };
                ForEachEmbeddedGenericFormalWorker(typeExpression, typeContext, methodContext, wrappedCollector, depth: 0);
            }

            private void ForEachEmbeddedGenericFormalWorker(TypeDesc type, Instantiation typeContext, Instantiation methodContext, System.Action<EcmaGenericParameter, int> collector, int depth)
            {
                switch (type.Category)
                {
                    case TypeFlags.Array:
                    case TypeFlags.SzArray:
                    case TypeFlags.ByRef:
                    case TypeFlags.Pointer:
                        ForEachEmbeddedGenericFormalWorker(((ParameterizedType)type).ParameterType, typeContext, methodContext, collector, depth + 1);
                        return;
                    case TypeFlags.FunctionPointer:
                        return;
                    case TypeFlags.SignatureMethodVariable:
                        var methodParam = (EcmaGenericParameter)methodContext[((SignatureMethodVariable)type).Index];
                        collector(methodParam, depth);
                        return;
                    case TypeFlags.SignatureTypeVariable:
                        var typeParam = (EcmaGenericParameter)typeContext[((SignatureTypeVariable)type).Index];
                        collector(typeParam, depth);
                        return;
                    default:
                        Debug.Assert(type.IsDefType);

                        // Non-constructed type. End of recursion.
                        if (!type.HasInstantiation || type.IsGenericDefinition)
                            return;

                        TypeDesc genericTypeDefinition = type.GetTypeDefinition();
                        Instantiation genericTypeParameters = genericTypeDefinition.Instantiation;
                        Instantiation genericTypeArguments = type.Instantiation;
                        for (int i = 0; i < genericTypeArguments.Length; i++)
                        {
                            var genericTypeParameter = (EcmaGenericParameter)genericTypeParameters[i];
                            TypeDesc genericTypeArgument = genericTypeArguments[i];

                            int newDepth = depth + 1;
                            ForEachEmbeddedGenericFormalWorker(
                                genericTypeArgument,
                                typeContext,
                                methodContext,
                                delegate (EcmaGenericParameter embedded, int depth2)
                                {
                                    collector(embedded, depth2);
                                    bool isProperEmbedding = (depth2 > newDepth);
                                    RecordBinding(genericTypeParameter, embedded, isProperEmbedding);
                                },
                                newDepth
                            );
                        }
                        return;
                }
            }
        }
    }
}
