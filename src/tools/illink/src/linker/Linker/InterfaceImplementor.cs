// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
		/// The .interfaceimpl on <see cref="InterfaceImplementor.Implementor"/>that points to <see cref="InterfaceImplementor.InterfaceType"/>
		/// </summary>
		public InterfaceImplementation[] InterfaceImplementations => InterfaceImplChain.ToArray ();

		public ImplNode InterfaceImplChain { get; }

		/// <summary>
		/// The type of the interface that is implemented by <see cref="InterfaceImplementor.Implementor"/>
		/// </summary>
		public TypeDefinition? InterfaceType { get; }

		public TypeReference InterfaceTypeReference => InterfaceImplChain.GetLast ().InterfaceType;
		public TypeReference InflatedInterface { get; }

		public InterfaceImplementor (TypeDefinition implementor, TypeDefinition? interfaceType, TypeReference inflatedInterface, ImplNode implNode)
		{
			Implementor = implementor;
			InterfaceType = interfaceType;
			InterfaceImplChain = implNode;
			InflatedInterface = inflatedInterface;
		}

		public InterfaceImplementor WithImplementor (TypeDefinition implementor)
		{
			return new InterfaceImplementor (implementor, InterfaceType, InterfaceImplChain);
		}

		public static InterfaceImplementor Create (TypeDefinition implementor, TypeDefinition interfaceType, IMetadataResolver resolver)
		{
			foreach (InterfaceImplementation iface in implementor.Interfaces) {
				if (resolver.Resolve (iface.InterfaceType) == interfaceType) {
					return new InterfaceImplementor (implementor, [iface], interfaceType);
				}
			}
			var baseTypeRef = implementor.BaseType;
			while (baseTypeRef is not null) {
				var baseType = resolver.Resolve (baseTypeRef);
				foreach (InterfaceImplementation iface in baseType.Interfaces) {
					if (resolver.Resolve (iface.InterfaceType) == interfaceType) {
						return new InterfaceImplementor (implementor, [iface], interfaceType);
					}
				}
				baseTypeRef = baseType.BaseType;
			}

			Queue<(TypeDefinition, IEnumerable<InterfaceImplementation>)> ifacesToCheck = new ();
			ifacesToCheck.Enqueue ((implementor, []));
			while (ifacesToCheck.Count > 0) {
				var (myFace, interfaceImpls) = ifacesToCheck.Dequeue ();

				foreach (InterfaceImplementation ifaceImpl in myFace.Interfaces) {
					var iface = resolver.Resolve (ifaceImpl.InterfaceType);
					if (iface == interfaceType) {

						return new InterfaceImplementor (implementor, interfaceImpls.Append (ifaceImpl).ToArray (), interfaceType);
					}
					ifacesToCheck.Enqueue ((iface, interfaceImpls.Append (ifaceImpl)));
				}
			}
			throw new InvalidOperationException ($"Type '{implementor.FullName}' does not implement interface '{interfaceType.FullName}' directly or through any base types or interfaces");
		}

		Dictionary<TypeDefinition, TypeDefinition[]> _ifacesRecursively;
		void FindMostDerivedImplsForEachInterface (TypeDefinition type)
		{

			Dictionary<TypeDefinition, List<TypeDefinition>> MostDerivedImpls = new ();
			Dictionary<TypeDefinition, List<TypeDefinition>> IsMostDerivedImplOf = new ();
			// All InterfaceImplementations on this type and base type
			foreach (var iface in _ifacesRecursively[type]) {
				var ifaceType = iface;
				foreach (var impledIface in _ifacesRecursively[ifaceType]) {
					if (MostDerivedImpls.TryGetValue (impledIface, out var mostDerived)) {
						// If this interface derives from all the currently most derived implementors, this interface (or the most derived implementor of it) is the new most derived
						if (mostDerived.All (d => _ifacesRecursively[ifaceType].Contains (d))) {
							SetAsMoreDerivedThan (impledIface, ifaceType);
						}
						else {
							bool addToList = true;
							for (int i = 0; i < MostDerivedImpls[impledIface].Count; i++) {
								if (_ifacesRecursively[ifaceType].Contains(MostDerivedImpls[impledIface][i])) {
									MostDerivedImpls[impledIface][i] = ifaceType;
									addToList = false;
								}
								if (MostDerivedImpls[ifaceType].Contains (MostDerivedImpls[impledIface][i])) {
									addToList = false;
									break;
								}
							}
							if (addToList)
								MostDerivedImpls[impledIface].Add (ifaceType);
						}
					} else {
						MostDerivedImpls.Add (impledIface, [ifaceType]);
					}
				}
			}

			void SetAsMoreDerivedThan (TypeDefinition type, TypeDefinition moreDerivedType)
			{
				MostDerivedImpls[type] = MostDerivedImpls[moreDerivedType!];

				if (IsMostDerivedImplOf.TryGetValue(type, out var mostDerivedImpls)) {
					foreach(var lessDerivedThanType in mostDerivedImpls) {
						SetAsMoreDerivedThan (lessDerivedThanType, moreDerivedType);
					}
				}

				List<TypeDefinition>? marr;
				if (!IsMostDerivedImplOf.TryGetValue (moreDerivedType!, out marr)) {
					marr = [];
					IsMostDerivedImplOf[moreDerivedType!] = marr;
				}
				marr.Add (type);
			}
		}
	}
	public sealed record ImplNode (InterfaceImplementation Value, TypeDefinition InterfaceImplementationProvider, ImplNode? Next) : IEnumerable<InterfaceImplementation>
	{
		sealed class Enumerator : IEnumerator<InterfaceImplementation>
		{
			public Enumerator (ImplNode original)
			{
				_current = original;
				_original = original;

			}
			ImplNode _current;
			ImplNode _original;
			public InterfaceImplementation Current => _current.Value;

			object IEnumerator.Current => _current.Value;

			public void Dispose () { }
			public bool MoveNext ()
			{
				if (_current.Next == null)
					return false;
				_current = _current.Next!;
				return true;
			}

			public void Reset () => _current = _original;
		}
		public IEnumerator<InterfaceImplementation> GetEnumerator () => new Enumerator (this);

		public InterfaceImplementation GetLast ()
		{
			var curr = this;
			while (curr.Next is not null) {
				curr = curr.Next;
			}
			return curr.Value;
		}
		public InterfaceImplementation[] ToArray ()
		{
			List<InterfaceImplementation> builder = new ();
			var curr = this;
			while (curr is not null) {
				builder.Add (curr.Value);
				curr = curr.Next;
			}
			return builder.ToArray ();
		}

		IEnumerator IEnumerable.GetEnumerator () => new Enumerator (this);
	}
}
