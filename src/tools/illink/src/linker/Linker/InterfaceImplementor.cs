// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
		public InterfaceImplementation InterfaceImplementation { get; }
		/// <summary>
		/// The type of the interface that is implemented by <see cref="InterfaceImplementor.Implementor"/>
		/// </summary>
		public TypeDefinition InterfaceType { get; }

		public InterfaceImplementor (TypeDefinition implementor, InterfaceImplementation interfaceImplementation, TypeDefinition interfaceType, IMetadataResolver resolver)
		{
			Implementor = implementor;
			InterfaceImplementation = interfaceImplementation;
			InterfaceType = interfaceType;
			Debug.Assert(resolver.Resolve (interfaceImplementation.InterfaceType) == interfaceType);
		}

		public static InterfaceImplementor Create(TypeDefinition implementor, TypeDefinition interfaceType, IMetadataResolver resolver)
		{
			foreach(InterfaceImplementation iface in implementor.Interfaces) {
				if (resolver.Resolve(iface.InterfaceType) == interfaceType) {
					return new InterfaceImplementor(implementor, iface, interfaceType, resolver);
				}
			}

			Queue<TypeDefinition> ifacesToCheck = new ();
			ifacesToCheck.Enqueue(implementor);
			while (ifacesToCheck.Count > 0) {
				var currentIface = ifacesToCheck.Dequeue ();

				foreach(InterfaceImplementation ifaceImpl in currentIface.Interfaces) {
					var iface = resolver.Resolve (ifaceImpl.InterfaceType);
					if (iface == interfaceType) {
						return new InterfaceImplementor(implementor, ifaceImpl, interfaceType, resolver);
					}
					ifacesToCheck.Enqueue (iface);
				}
			}
			throw new InvalidOperationException ($"Type '{implementor.FullName}' does not implement interface '{interfaceType.FullName}' directly or through any interfaces");
		}
	}
}
