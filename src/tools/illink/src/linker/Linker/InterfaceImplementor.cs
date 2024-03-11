// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
		/// Null if the type could not be resolved
		/// </summary>
		public TypeDefinition? InterfaceType { get; }

		/// <summary>
		/// A <see cref="TypeReference"/> to the <see cref="InterfaceType"/> with the generic parameters substituted.
		/// </summary>
		public TypeReference InflatedInterface { get; }

		/// <summary>
		/// The graphs of <see cref="InterfaceImplementation"/>s that make <see cref="Implementor"/> implement the <see cref="InflatedInterface"/>.
		/// There can be many ways a type implements an interface if an explicit interface implementation is given for an interface that is also implemented recursively due to another interface implementation.
		/// It will be in the following order:
		///  1. Explicit interface implementation on <see cref="Implementor"/>
		///  2. Explicit interface implementation on a base type of <see cref="Implementor"/>
		///  3. Recursive interface implementations on an explicitly implemented interface on <see cref="Implementor"/> or it's base types
		/// </summary>
		public readonly ImmutableArray<InterfaceImplementationNode> InterfaceImplementationNodes;

		public bool HasExplicitImplementation => InterfaceImplementationNodes[0].Length == 0;

		public InterfaceImplementor (TypeDefinition implementor, TypeDefinition? interfaceType, TypeReference inflatedInterface, ImmutableArray<InterfaceImplementationNode> implNode, LinkContext context)
		{
			Implementor = implementor;
			InterfaceType = interfaceType;
			InterfaceImplementationNodes = implNode;
			InflatedInterface = inflatedInterface;
			Debug.Assert (context.Resolve (inflatedInterface) == interfaceType);
			Debug.Assert (implNode.Length != 0);
			Debug.Assert (implNode.All (i => interfaceType == context.Resolve (i.GetLast ().InterfaceType)));
		}

		/// <summary>
		/// An Enumerable over the most direct <see cref="InterfaceImplementation"/> chain to the <see cref="InflatedInterface"/>
		/// </summary>
		public ShortestInterfaceImplementationChainEnumerator MostDirectInterfaceImplementationPath => new (this);

		public struct ShortestInterfaceImplementationChainEnumerator
		{
			InterfaceImplementationNode _current;
			bool _hasMoved;
			public ShortestInterfaceImplementationChainEnumerator (InterfaceImplementor implementor)
			{
				_current = implementor.InterfaceImplementationNodes[0];
				_hasMoved = false;
			}
			public ShortestInterfaceImplementationChainEnumerator GetEnumerator () => this;
			public InterfaceImplementation Current => _current.InterfaceImplementation;

			public bool MoveNext ()
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

		/// <summary>
		/// Returns true if the most direct implementation of <see cref="InflatedInterface"/> is marked. <see cref="Implementor"/> may still have a different recursive implementation marked.
		/// </summary>
		public bool IsMostDirectImplementationMarked (AnnotationStore annotations)
		{
			return InterfaceImplementationNodes[0].IsMostDirectImplementationMarked (annotations);
		}

		/// <summary>
		/// Returns true if <see cref="Implementor"/> implements <see cref="InflatedInterface"/> via any of the possible interface implementation chains.
		/// </summary>
		public bool IsMarked (AnnotationStore annotations)
		{
			foreach (var i in InterfaceImplementationNodes) {
				if (i.IsMarked (annotations))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Represents a node in the graph of a type implementing an interface.
		/// </summary>
		public sealed class InterfaceImplementationNode
		{
			/// <summary>
			/// The <see cref="Mono.Cecil.InterfaceImplementation"/> that is on <see cref="InterfaceImplementationProvider"/> that is part of the chain of interface implementations.
			/// </summary>
			public InterfaceImplementation InterfaceImplementation { get; }

			/// <summary>
			/// The type that has <see cref="InterfaceImplementation"/> in its <see cref="TypeDefinition.Interfaces"/>.
			/// </summary>
			public TypeDefinition InterfaceImplementationProvider { get; }

			/// <summary>
			/// The <see cref="InterfaceImplementationNode"/>s that are on the type pointed to by <see cref="InterfaceImplementation"/> that lead to the interface type.
			/// </summary>
			public ImmutableArray<InterfaceImplementationNode> Next { get; }

			/// <summary>
			/// The number of interface implementations on the most direct way the interface is implemented from <see cref="InterfaceImplementationProvider"/>
			/// </summary>
			public int Length {
				get {
					if (_length != -1)
						return _length;
					if (Next.Length == 0)
						return _length = 0;
					return _length = Next[0].Length + 1;
				}
			}
			int _length = -1;

			public InterfaceImplementationNode (InterfaceImplementation interfaceImplementation, TypeDefinition interfaceImplementationProvider, ImmutableArray<InterfaceImplementationNode> next)
			{
				InterfaceImplementation = interfaceImplementation;
				InterfaceImplementationProvider = interfaceImplementationProvider;
				Next = next;
			}

			public bool IsMostDirectImplementationMarked (AnnotationStore annotations)
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

				foreach (var impl in Next) {
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
}
