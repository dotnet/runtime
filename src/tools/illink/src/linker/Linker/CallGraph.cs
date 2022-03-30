// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	class CallGraph
	{
		readonly Dictionary<MethodDefinition, HashSet<MethodDefinition>> callGraph;

		public CallGraph () => callGraph = new Dictionary<MethodDefinition, HashSet<MethodDefinition>> ();

		public void TrackCall (MethodDefinition fromMethod, MethodDefinition toMethod)
		{
			if (!callGraph.TryGetValue (fromMethod, out HashSet<MethodDefinition>? toMethods)) {
				toMethods = new HashSet<MethodDefinition> ();
				callGraph.Add (fromMethod, toMethods);
			}
			toMethods.Add (toMethod);
		}

		public IEnumerable<MethodDefinition> GetReachableMethods (MethodDefinition start)
		{
			Queue<MethodDefinition> queue = new ();
			HashSet<MethodDefinition> visited = new ();
			visited.Add (start);
			queue.Enqueue (start);
			while (queue.TryDequeue (out MethodDefinition? method)) {
				if (!callGraph.TryGetValue (method, out HashSet<MethodDefinition>? callees))
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