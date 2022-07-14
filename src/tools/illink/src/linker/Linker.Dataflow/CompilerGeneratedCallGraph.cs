// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	sealed class CompilerGeneratedCallGraph
	{
		readonly Dictionary<IMemberDefinition, HashSet<IMemberDefinition>> _callGraph;

		public CompilerGeneratedCallGraph () => _callGraph = new Dictionary<IMemberDefinition, HashSet<IMemberDefinition>> ();

		void TrackCallInternal (IMemberDefinition fromMember, IMemberDefinition toMember)
		{
			if (!_callGraph.TryGetValue (fromMember, out HashSet<IMemberDefinition>? toMembers)) {
				toMembers = new HashSet<IMemberDefinition> ();
				_callGraph.Add (fromMember, toMembers);
			}
			toMembers.Add (toMember);
		}

		public void TrackCall (MethodDefinition fromMethod, MethodDefinition toMethod)
		{
			Debug.Assert (CompilerGeneratedNames.IsLambdaOrLocalFunction (toMethod.Name));
			TrackCallInternal (fromMethod, toMethod);
		}

		public void TrackCall (MethodDefinition fromMethod, TypeDefinition toType)
		{
			Debug.Assert (CompilerGeneratedNames.IsStateMachineType (toType.Name));
			TrackCallInternal (fromMethod, toType);
		}

		public void TrackCall (TypeDefinition fromType, MethodDefinition toMethod)
		{
			Debug.Assert (CompilerGeneratedNames.IsStateMachineType (fromType.Name));
			Debug.Assert (CompilerGeneratedNames.IsLambdaOrLocalFunction (toMethod.Name));
			TrackCallInternal (fromType, toMethod);
		}

		public IEnumerable<IMemberDefinition> GetReachableMembers (MethodDefinition start)
		{
			Queue<IMemberDefinition> queue = new ();
			HashSet<IMemberDefinition> visited = new ();
			visited.Add (start);
			queue.Enqueue (start);
			while (queue.TryDequeue (out IMemberDefinition? method)) {
				if (!_callGraph.TryGetValue (method, out HashSet<IMemberDefinition>? callees))
					continue;

				foreach (var callee in callees) {
					if (visited.Add (callee)) {
						queue.Enqueue (callee);
						yield return callee;
					}
				}
			}
		}
	}
}
