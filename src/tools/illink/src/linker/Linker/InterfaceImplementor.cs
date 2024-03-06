// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Mono.Cecil;

namespace Mono.Linker
{
	public class InterfaceImplementor
	{
		/// <summary>
		/// The type that implements <see cref="InterfaceImplementor.InterfaceType"/>.
		/// </summary>
		public TypeDefinition Implementor { get; }

		/// <summary>
		/// The type of the interface that is implemented by <see cref="InterfaceImplementor.Implementor"/>
		/// </summary>
		public TypeDefinition? InterfaceType { get; }

		public ImplNode[] InterfaceImplementationNode { get; }

		public TypeReference InflatedInterface { get; }

		public InterfaceImplementor (TypeDefinition implementor, TypeDefinition? interfaceType, TypeReference inflatedInterface, ImplNode[] implNode, LinkContext context)
		{
			Implementor = implementor;
			InterfaceType = interfaceType;
			InterfaceImplementationNode = implNode;
			InflatedInterface = inflatedInterface;
			Debug.Assert (context.Resolve (inflatedInterface) == interfaceType);
			Debug.Assert (implNode.Length != 0);
			Debug.Assert (implNode.All (i => interfaceType == context.Resolve (i.GetLast ().InterfaceType)));
			// Ensure the ImplNode is sorted by Length
			Debug.Assert (
				implNode.Aggregate(
					(true, 0),
					(acc, next) => {
						if (!acc.Item1) return acc;
						if (acc.Item2 <= next.Length)
							return (true, next.Length);
						return (false, next.Length);
					})
					.Item1);
		}

		public IEnumerable<InterfaceImplementation> ShortestInterfaceImplementationChain()
		{
			var curr = InterfaceImplementationNode[0];
			yield return curr.InterfaceImplementation;
			while (curr.Next.Length != 0) {
				curr = curr.Next[0];
				yield return curr.InterfaceImplementation;
			}
		}
		public void MarkShortestImplementation(AnnotationStore annotations, in DependencyInfo reason, in MessageOrigin origin)
		{
			InterfaceImplementationNode[0].MarkShortestImplementation (annotations, reason, origin);
		}
		public bool IsMarked(AnnotationStore annotations)
		{
			foreach(var i in InterfaceImplementationNode) {
				if (i.IsMarked(annotations))
					return true;
			}
			return false;
		}
	}

	public sealed record ImplNode (InterfaceImplementation InterfaceImplementation, TypeDefinition InterfaceImplementationProvider, ImmutableArray<ImplNode> Next)
	{
		int _length = -1;
		public int Length {
			get {
				if (_length != -1)
					return _length;
				if (Next.Length == 0)
					return _length = 0;
				return _length = Next[0].Length + 1;
			}
		}

		public void MarkShortestImplementation (AnnotationStore annotations, in DependencyInfo reason, in MessageOrigin origin)
		{
			ImplNode curr = this;
			annotations.Mark (curr.InterfaceImplementation, reason, origin);
			while (curr.Next.Length > 0) {
				curr = curr.Next[0];
				annotations.Mark (curr.InterfaceImplementation, reason, origin);
			}
		}

		public bool IsMarked (AnnotationStore annotations)
		{
			if (!annotations.IsMarked (InterfaceImplementation))
				return false;

			if (Next.Length == 0)
				return true;

			foreach(var impl in Next) {
				if (impl.IsMarked (annotations))
					return true;
			}
			return false;
		}

		public InterfaceImplementation GetLast ()
		{
			var curr = this;
			while (curr.Next.Length > 0) {
				curr = curr.Next[0];
			}
			return curr.InterfaceImplementation;
		}
	}
}
