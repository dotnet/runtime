// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILCompiler.Dataflow
{
    sealed class CompilerGeneratedCallGraph
    {
        readonly Dictionary<TypeSystemEntity, HashSet<TypeSystemEntity>> _callGraph;

        public CompilerGeneratedCallGraph() => _callGraph = new Dictionary<TypeSystemEntity, HashSet<TypeSystemEntity>>();

        void TrackCallInternal(TypeSystemEntity fromMember, TypeSystemEntity toMember)
        {
            if (!_callGraph.TryGetValue(fromMember, out HashSet<TypeSystemEntity>? toMembers))
            {
                toMembers = new HashSet<TypeSystemEntity>();
                _callGraph.Add(fromMember, toMembers);
            }
            toMembers.Add(toMember);
        }

        public void TrackCall(MethodDesc fromMethod, MethodDesc toMethod)
        {
            Debug.Assert(fromMethod.IsTypicalMethodDefinition);
            Debug.Assert(toMethod.IsTypicalMethodDefinition);
            Debug.Assert(CompilerGeneratedNames.IsLambdaOrLocalFunction(toMethod.Name));
            TrackCallInternal(fromMethod, toMethod);
        }

        public void TrackCall(MethodDesc fromMethod, DefType toType)
        {
            Debug.Assert(fromMethod.IsTypicalMethodDefinition);
            Debug.Assert(toType.IsTypeDefinition);
            Debug.Assert(CompilerGeneratedNames.IsStateMachineType(toType.Name));
            TrackCallInternal(fromMethod, toType);
        }

        public void TrackCall(DefType fromType, MethodDesc toMethod)
        {
            Debug.Assert(fromType.IsTypeDefinition);
            Debug.Assert(toMethod.IsTypicalMethodDefinition);
            Debug.Assert(CompilerGeneratedNames.IsStateMachineType(fromType.Name));
            Debug.Assert(CompilerGeneratedNames.IsLambdaOrLocalFunction(toMethod.Name));
            TrackCallInternal(fromType, toMethod);
        }

        public IEnumerable<TypeSystemEntity> GetReachableMembers(MethodDesc start)
        {
            Queue<TypeSystemEntity> queue = new();
            HashSet<TypeSystemEntity> visited = new();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.TryDequeue(out TypeSystemEntity? method))
            {
                if (!_callGraph.TryGetValue(method, out HashSet<TypeSystemEntity>? callees))
                    continue;

                foreach (var callee in callees)
                {
                    if (visited.Add(callee))
                    {
                        queue.Enqueue(callee);
                        yield return callee;
                    }
                }
            }
        }
    }
}
