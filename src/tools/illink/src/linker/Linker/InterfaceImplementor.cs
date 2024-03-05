// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
		/// </summary>
		public TypeDefinition? InterfaceType { get; }

		public ImplNode InterfaceImplementationNode { get; }

		public TypeReference InflatedInterface { get; }

		public InterfaceImplementor (TypeDefinition implementor, TypeDefinition? interfaceType, TypeReference inflatedInterface, ImplNode implNode, LinkContext context)
		{
			Implementor = implementor;
			InterfaceType = interfaceType;
			InterfaceImplementationNode = implNode;
			InflatedInterface = inflatedInterface;
			Debug.Assert (interfaceType == context.Resolve (implNode.GetLast ().InterfaceType));
		}
	}

	public sealed record ImplNode (InterfaceImplementation InterfaceImplementation, TypeDefinition InterfaceImplementationProvider, ImplNode? Next) : IEnumerable<InterfaceImplementation>
	{
		public struct Enumerator : IEnumerator<InterfaceImplementation>
		{
			ImplNode _current;
			ImplNode _original;
			bool _hasBeenMovedOnce;

			public Enumerator (ImplNode original)
			{
				_current = original;
				_original = original;
				_hasBeenMovedOnce = false;
			}

			public InterfaceImplementation Current => _current.Value;

			object IEnumerator.Current => _current.Value;

			public void Dispose () { }

			public bool MoveNext ()
			{
				if (!_hasBeenMovedOnce) {
					_hasBeenMovedOnce = true;
					return true;
				}
				if (_current is null)
					throw new InvalidOperationException ();
				if (_current.Next is null)
					return false;
				_current = _current.Next;
				return true;
			}

			public void Reset () => _current = _original;
		}

		public Enumerator GetEnumerator () => new Enumerator (this);
		IEnumerator<InterfaceImplementation> IEnumerable<InterfaceImplementation>.GetEnumerator () => new Enumerator (this);

		public InterfaceImplementation GetLast ()
		{
			var curr = this;
			while (curr.Next is not null) {
				curr = curr.Next;
			}
			return curr.Value;
		}

		IEnumerator IEnumerable.GetEnumerator () => new Enumerator (this);
	}
}
