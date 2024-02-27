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
		public InterfaceImplementation[] InterfaceImplementations { get; }
		/// <summary>
		/// The type of the interface that is implemented by <see cref="InterfaceImplementor.Implementor"/>
		/// </summary>
		public TypeDefinition InterfaceType { get; }

		public InterfaceImplementor (TypeDefinition implementor, InterfaceImplementation[] interfaceImplementations, TypeDefinition interfaceType)
		{
			Implementor = implementor;
			InterfaceImplementations = interfaceImplementations;
			InterfaceType = interfaceType;
		}

		public static InterfaceImplementor Create(TypeDefinition implementor, TypeDefinition interfaceType, IMetadataResolver resolver)
		{
			foreach(InterfaceImplementation iface in implementor.Interfaces) {
				if (resolver.Resolve(iface.InterfaceType) == interfaceType) {
					return new InterfaceImplementor(implementor, [iface], interfaceType);
				}
			}
			var baseTypeRef = implementor.BaseType;
			while (baseTypeRef is not null) {
				var baseType = resolver.Resolve (baseTypeRef);
				foreach(InterfaceImplementation iface in baseType.Interfaces) {
					if (resolver.Resolve(iface.InterfaceType) == interfaceType) {
						return new InterfaceImplementor(implementor, [iface], interfaceType);
					}
				}
				baseTypeRef = baseType.BaseType;
			}

			Queue<(TypeDefinition, IEnumerable<InterfaceImplementation>)> ifacesToCheck = new ();
			ifacesToCheck.Enqueue((implementor, []));
			while (ifacesToCheck.Count > 0) {
				var (myFace, interfaceImpls) = ifacesToCheck.Dequeue ();

				foreach(InterfaceImplementation ifaceImpl in myFace.Interfaces) {
					var iface = resolver.Resolve (ifaceImpl.InterfaceType);
					if (iface == interfaceType) {

						return new InterfaceImplementor(implementor, interfaceImpls.Append(ifaceImpl).ToArray(), interfaceType);
					}
					ifacesToCheck.Enqueue ((iface, interfaceImpls.Append(ifaceImpl)));
				}
			}
			throw new InvalidOperationException ($"Type '{implementor.FullName}' does not implement interface '{interfaceType.FullName}' directly or through any base types or interfaces");
		}
	}
}
