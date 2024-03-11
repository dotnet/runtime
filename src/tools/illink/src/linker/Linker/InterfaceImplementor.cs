// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

		public ImmutableArray<ImplNode> InterfaceImplementationNode { get; }

		public TypeReference InflatedInterface { get; }

		public InterfaceImplementor (TypeDefinition implementor, TypeDefinition? interfaceType, TypeReference inflatedInterface, ImmutableArray<ImplNode> implNode, LinkContext context)
		{
			Implementor = implementor;
			InterfaceType = interfaceType;
			InterfaceImplementationNode = implNode;
			InflatedInterface = inflatedInterface;
			Debug.Assert (context.Resolve (inflatedInterface) == interfaceType);
			Debug.Assert (implNode.Length != 0);
			Debug.Assert (implNode.All (i => interfaceType == context.Resolve (i.GetLast ().InterfaceType)));
		}

		public ShortestInterfaceImplementationChainEnumerator MostDirectInterfaceImplementationPath => new (this);

		public struct ShortestInterfaceImplementationChainEnumerator
		{
			ImplNode _current;
			bool _hasMoved;
			public ShortestInterfaceImplementationChainEnumerator (InterfaceImplementor implementor)
			{
				_current = implementor.InterfaceImplementationNode[0];
				_hasMoved = false;
			}
			public ShortestInterfaceImplementationChainEnumerator GetEnumerator() => this;
			public InterfaceImplementation Current => _current.InterfaceImplementation;

			public bool MoveNext()
			{
				if (!_hasMoved) {
					_hasMoved = true;
					return true;
				}
				if (_current.Next.Length == 0)
					return false;
				_current = _current.Next[0];
				return true;
			}
		}

		public void MarkShortestImplementation(AnnotationStore annotations, in DependencyInfo reason, in MessageOrigin origin)
		{
			InterfaceImplementationNode[0].MarkShortestImplementation (annotations, reason, origin);
		}

		public bool IsMostDirectImplementationMarked(AnnotationStore annotations)
		{
			return InterfaceImplementationNode[0].IsMostDirectImplementationMarked (annotations);
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

	public sealed record ImplNode (InterfaceImplementation InterfaceImplementation, TypeDefinition InterfaceImplementationProvider, ImmutableArray<ImplNode> Next) : IComparable<ImplNode>
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

		public bool IsMostDirectImplementationMarked(AnnotationStore annotations)
		{
			if (!annotations.IsMarked (InterfaceImplementation))
				return false;
			if (Next.Length == 0)
				return true;
			return Next[0].IsMostDirectImplementationMarked (annotations);
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

		public int CompareTo (ImplNode? other) => this.Length - other!.Length;
	}
}
