// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            /// Sometimes, this side-effect is all that's wanted - in such cases, invoke this method with a default collector.
            /// </summary>
            private static void ForEachEmbeddedGenericFormal(TypeDesc typeExpression, Instantiation typeContext, Instantiation methodContext, ref EmbeddingStateList collector)
            {
                ForEachEmbeddedGenericFormalWorker(typeExpression, typeContext, methodContext, ref collector, depth: 0);
            }

            private static void ForEachEmbeddedGenericFormalWorker(TypeDesc type, Instantiation typeContext, Instantiation methodContext, ref EmbeddingStateList collector, int depth)
            {
                switch (type.Category)
                {
                    case TypeFlags.Array:
                    case TypeFlags.SzArray:
                    case TypeFlags.ByRef:
                    case TypeFlags.Pointer:
                        ForEachEmbeddedGenericFormalWorker(((ParameterizedType)type).ParameterType, typeContext, methodContext, ref collector, depth + 1);
                        return;
                    case TypeFlags.FunctionPointer:
                        return;
                    case TypeFlags.SignatureMethodVariable:
                        var methodParam = (EcmaGenericParameter)methodContext[((SignatureMethodVariable)type).Index];
                        collector.Collect(methodParam, depth);
                        return;
                    case TypeFlags.SignatureTypeVariable:
                        var typeParam = (EcmaGenericParameter)typeContext[((SignatureTypeVariable)type).Index];
                        collector.Collect(typeParam, depth);
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
                            collector.Push(static delegate (in EmbeddingState state, GraphBuilder builder, EcmaGenericParameter embedded, int depth)
                            {
                                bool isProperEmbedding = (depth > state.NewDepth);
                                builder.RecordBinding(state.GenericTypeParameter, embedded, isProperEmbedding);
                            }, genericTypeParameter, newDepth);
                            ForEachEmbeddedGenericFormalWorker(
                                genericTypeArgument,
                                typeContext,
                                methodContext,
                                ref collector,
                                newDepth
                            );
                            collector.Pop();
                        }
                        return;
                }
            }

            private delegate void Collector(in EmbeddingState state, GraphBuilder builder, EcmaGenericParameter embedded, int depth);

            private struct EmbeddingState
            {
                private readonly Collector _collector;
                public readonly EcmaGenericParameter GenericTypeParameter;
                public readonly int NewDepth;

                public EmbeddingState(Collector collector, EcmaGenericParameter genericTypeParameter, int newDepth)
                    => (_collector, GenericTypeParameter, NewDepth) = (collector, genericTypeParameter, newDepth);

                public void Invoke(GraphBuilder builder, EcmaGenericParameter embedded, int depth2) => _collector(this, builder, embedded, depth2);
            }

            private struct EmbeddingStateList
            {
                private int _numItems;

                private GraphBuilder _builder;

                private EmbeddingState _item0;
                private EmbeddingState _item1;
                private EmbeddingState _item2;
                private EmbeddingState _item3;

                private List<EmbeddingState> _overflow;

                public EmbeddingStateList(GraphBuilder builder) => _builder = builder;

                public void Push(Collector collector, EcmaGenericParameter genericTypeParameter, int newDepth)
                {
                    EmbeddingState state = new EmbeddingState(collector, genericTypeParameter, newDepth);
                    switch (_numItems)
                    {
                        case 0: _item0 = state; break;
                        case 1: _item1 = state; break;
                        case 2: _item2 = state; break;
                        case 3: _item3 = state; break;
                        default:
                            (_overflow ??= new List<EmbeddingState>()).Add(state);
                            break;
                    }

                    _numItems++;
                }

                public void Pop()
                {
                    if (_numItems > 4)
                        _overflow.RemoveAt(_overflow.Count - 1);
                    _numItems--;
                }

                public void Collect(EcmaGenericParameter embedded, int depth2)
                {
                    int numItems = _numItems;
                    if (numItems > 0)
                        _item0.Invoke(_builder, embedded, depth2);
                    if (numItems > 1)
                        _item1.Invoke(_builder, embedded, depth2);
                    if (numItems > 2)
                        _item2.Invoke(_builder, embedded, depth2);
                    if (numItems > 3)
                        _item3.Invoke(_builder, embedded, depth2);
                    if (numItems > 4)
                    {
                        foreach (EmbeddingState state in _overflow)
                            state.Invoke(_builder, embedded, depth2);
                    }
                }
            }
        }
    }
}
