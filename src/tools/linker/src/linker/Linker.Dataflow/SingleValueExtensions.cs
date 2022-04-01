// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	// These are extension methods because we want to allow the use of them on null 'this' pointers.
	internal static class SingleValueExtensions
	{
		/// <summary>
		/// Returns true if a ValueNode graph contains a cycle
		/// </summary>
		/// <param name="node">Node to evaluate</param>
		/// <param name="seenNodes">Set of nodes previously seen on the current arc. Callers may pass a non-empty set
		/// to test whether adding that set to this node would create a cycle. Contents will be modified by the walk
		/// and should not be used by the caller after returning</param>
		/// <param name="allNodesSeen">Optional. The set of all nodes encountered during a walk after DetectCycle returns</param>
		/// <returns></returns>
		public static bool DetectCycle (this SingleValue node, HashSet<SingleValue> seenNodes, HashSet<SingleValue>? allNodesSeen)
		{
			if (node == null)
				return false;

			if (seenNodes.Contains (node))
				return true;

			seenNodes.Add (node);

			if (allNodesSeen != null) {
				allNodesSeen.Add (node);
			}

			bool foundCycle = false;
			switch (node) {
			//
			// Leaf nodes
			//
			case UnknownValue:
			case NullValue:
			case SystemTypeValue:
			case RuntimeTypeHandleValue:
			case KnownStringValue:
			case ConstIntValue:
			case MethodParameterValue:
			case MethodThisParameterValue:
			case MethodReturnValue:
			case GenericParameterValue:
			case RuntimeTypeHandleForGenericParameterValue:
			case SystemReflectionMethodBaseValue:
			case RuntimeMethodHandleValue:
			case FieldValue:
				break;

			//
			// Nodes with children
			//
			case ArrayValue:
				ArrayValue av = (ArrayValue) node;
				foundCycle = av.Size.DetectCycle (seenNodes, allNodesSeen);
				foreach (ValueBasicBlockPair pair in av.IndexValues.Values) {
					foreach (var v in pair.Value) {
						foundCycle |= v.DetectCycle (seenNodes, allNodesSeen);
					}
				}
				break;

			case RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers value:
				foundCycle = value.UnderlyingTypeValue.DetectCycle (seenNodes, allNodesSeen);
				break;

			case NullableValueWithDynamicallyAccessedMembers value:
				foundCycle = value.UnderlyingTypeValue.DetectCycle (seenNodes, allNodesSeen);
				break;

			default:
				throw new Exception (String.Format ("Unknown node type: {0}", node.GetType ().Name));
			}
			seenNodes.Remove (node);

			return foundCycle;
		}
	}
}