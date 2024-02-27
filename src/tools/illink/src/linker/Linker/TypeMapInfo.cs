// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// TypeMapInfo.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{

	public class TypeMapInfo
	{
		readonly HashSet<AssemblyDefinition> assemblies = new HashSet<AssemblyDefinition> ();
		readonly LinkContext context;
		protected readonly Dictionary<MethodDefinition, List<OverrideInformation>> base_methods = new Dictionary<MethodDefinition, List<OverrideInformation>> ();
		protected readonly Dictionary<MethodDefinition, List<OverrideInformation>> override_methods = new Dictionary<MethodDefinition, List<OverrideInformation>> ();
		protected readonly Dictionary<MethodDefinition, List<OverrideInformation>> default_interface_implementations = new Dictionary<MethodDefinition, List<OverrideInformation>> ();
		protected readonly Dictionary<TypeDefinition, List<InterfaceImplementor>> interfaces = new ();

		public TypeMapInfo (LinkContext context)
		{
			this.context = context;
		}

		public void EnsureProcessed (AssemblyDefinition assembly)
		{
			if (!assemblies.Add (assembly))
				return;

			foreach (TypeDefinition type in assembly.MainModule.Types)
				MapType (type);
		}

		public ICollection<MethodDefinition> MethodsWithOverrideInformation => override_methods.Keys;

		/// <summary>
		/// Returns a list of all known methods that override <paramref name="method"/>. The list may be incomplete if other overrides exist in assemblies that haven't been processed by TypeMapInfo yet
		/// </summary>
		public IEnumerable<OverrideInformation>? GetOverrides (MethodDefinition method)
		{
			EnsureProcessed (method.Module.Assembly);
			override_methods.TryGetValue (method, out List<OverrideInformation>? overrides);
			return overrides;
		}

		/// <summary>
		/// Returns all base methods that <paramref name="method"/> overrides.
		/// This includes the closest overridden virtual method on <paramref name="method"/>'s base types
		/// methods on an interface that <paramref name="method"/>'s declaring type implements,
		/// and methods an interface implemented by a derived type of <paramref name="method"/>'s declaring type if the derived type uses <paramref name="method"/> as the implementing method.
		/// The list may be incomplete if there are derived types in assemblies that havent been processed yet that use <paramref name="method"/> to implement an interface.
		/// </summary>
		public List<OverrideInformation>? GetBaseMethods (MethodDefinition method)
		{
			EnsureProcessed (method.Module.Assembly);
			base_methods.TryGetValue (method, out List<OverrideInformation>? bases);
			return bases;
		}

		/// <summary>
		/// Returns a list of all default interface methods that implement <paramref name="method"/> for a type.
		/// ImplementingType is the type that implements the interface,
		/// InterfaceImpl is the <see cref="InterfaceImplementation" /> for the interface <paramref name="method" /> is declared on, and
		/// DefaultInterfaceMethod is the method that implements <paramref name="method"/>.
		/// </summary>
		/// <param name="method">The interface method to find default implementations for</param>
		public IEnumerable<OverrideInformation>? GetDefaultInterfaceImplementations (MethodDefinition baseMethod)
		{
			default_interface_implementations.TryGetValue (baseMethod, out var ret);
			return ret;
		}

		public void AddBaseMethod (MethodDefinition method, MethodDefinition @base, InterfaceImplementor? interfaceImplementor)
		{
			if (!base_methods.TryGetValue (method, out List<OverrideInformation>? methods)) {
				methods = new List<OverrideInformation> ();
				base_methods[method] = methods;
			}

			methods.Add (new OverrideInformation (@base, method, interfaceImplementor));
		}

		public void AddOverride (MethodDefinition @base, MethodDefinition @override, InterfaceImplementor? interfaceImplementor = null)
		{
			if (!override_methods.TryGetValue (@base, out List<OverrideInformation>? methods)) {
				methods = new List<OverrideInformation> ();
				override_methods.Add (@base, methods);
			}

			methods.Add (new OverrideInformation (@base, @override, interfaceImplementor));
		}

		public void AddDefaultInterfaceImplementation (MethodDefinition @base, InterfaceImplementor interfaceImplementor, MethodDefinition defaultImplementationMethod)
		{
			Debug.Assert(@base.DeclaringType.IsInterface);
			if (!default_interface_implementations.TryGetValue (@base, out var implementations)) {
				implementations = new List<OverrideInformation> ();
				default_interface_implementations.Add (@base, implementations);
			}

			implementations.Add (new (@base, defaultImplementationMethod, interfaceImplementor));
		}

		protected virtual void MapType (TypeDefinition type)
		{
			MapInterfacesOnType (type);
			MapVirtualMethods (type);
			MapInterfaceMethodsInTypeHierarchy (type);

			if (!type.HasNestedTypes)
				return;

			foreach (var nested in type.NestedTypes)
				MapType (nested);
		}

		record IFaceImplr(TypeDefinition ImplementingType, TypeDefinition InterfaceImplProvider, TypeDefinition InterfaceType, ImplNode InterfaceImplChain);
		readonly Dictionary<TypeDefinition, List<IFaceImplr>> _ifaces = new ();
		record ImplNode (InterfaceImplementation Value, ImplNode? Next)
		{
			public InterfaceImplementation GetLast ()
			{
				var curr = this;
				while (curr.Next is not null) {
					curr = curr.Next;
				}
				return curr.Value;
			}
			public InterfaceImplementation[] ToArray()
			{
				List<InterfaceImplementation> builder = new();
				var curr = this;
				while (curr is not null) {
					builder.Add (curr.Value);
					curr = curr.Next;
				}
				return builder.ToArray ();
			}
		}
		protected void MapInterfacesOnType (TypeDefinition type)
		{
			if (_ifaces.ContainsKey (type))
				return;

			List<IFaceImplr> implrs = [];
			HashSet<TypeDefinition> interfacesSeen = [];
			foreach (var iface in type.Interfaces) {
				var ifaceType = context.Resolve (iface.InterfaceType);
				Debug.Assert (ifaceType is not null);
				implrs.Add (new (type, type, ifaceType, new ImplNode (iface, null)));
				interfacesSeen.Add (ifaceType);
			}
			if (type.BaseType is { } baseType && context.Resolve (baseType) is { } baseDef) {

				MapInterfacesOnType (baseDef);
				var baseInterfaces = _ifaces[baseDef];
				foreach(var item in baseInterfaces) {
					if (interfacesSeen.Add(item.InterfaceType)) {
						implrs.Add (item with { ImplementingType = type });
						interfacesSeen.Add (item.InterfaceType);
					}
				}
			}
			// Look into all interfaces for other interfaces this type might implement
			Queue<TypeDefinition> typeDefinitions = new();
			foreach(var iface in implrs) {
				MapInterfacesOnType (iface.InterfaceType);
				var baseInterfaces = _ifaces[iface.InterfaceType];
				foreach(var item in baseInterfaces) {
					if (interfacesSeen.Add(item.InterfaceType)) {
						// Don't add these to the seen interfaces list - we need to keep all of these to be able to find the most specific DIMs
						implrs.Add (item with {
							ImplementingType = type,
							InterfaceImplChain = new ImplNode (iface.InterfaceImplChain.Value, item.InterfaceImplChain)
						});
					}
				}
			}
			_ifaces.Add (type, implrs);
		}

		IEqualityComparer<TypeReference>? _inflatedInterfaceComparer;
		IEqualityComparer<TypeReference> InflatedInterfaceComparer => _inflatedInterfaceComparer ??= EqualityComparer<TypeReference>.Create ((a, b) => {
			Debug.Assert (a is not null && b is not null);
			// Skip comparing generic parameters since it's difficult to compare them correctly
			if (a is TypeSpecification || b is TypeSpecification)
				return false;
			var retval =  context.Resolve (a) is { } aDef && context.Resolve (b) is { } bDef && aDef == bDef;
			if (retval)
				_ = 0;
			return retval;
		}, obj => context.Resolve(obj)?.GetHashCode() ?? 0);

		public IEnumerable<(TypeReference InflatedInterface, ImplNode OriginalImpl)> GetInflatedInterfaces (TypeReference typeRef)
		{
			var typeDef = context.TryResolve (typeRef);

			if (typeDef?.HasInterfaces != true)
				yield break;

			if (typeRef is GenericInstanceType genericInstance) {
				foreach (var interfaceImpl in _ifaces[typeDef]) {
					// InflateGenericType only returns null when inflating generic parameters (and the generic instance type doesn't resolve).
					// Here we are not inflating a generic parameter but an interface type reference.
					yield return (TypeReferenceExtensions.InflateGenericType (genericInstance, interfaceImpl.InterfaceType, context), interfaceImpl.InterfaceImplChain)!;
				}
			} else {
				foreach (var interfaceImpl in _ifaces[typeDef])
					yield return (interfaceImpl.InterfaceType, interfaceImpl.InterfaceImplChain);
			}
		}

		void MapInterfaceMethodsInTypeHierarchy (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			// Foreach interface and for each newslot virtual method on the interface, try
			// to find the method implementation and record it.
			foreach (var interfaceImpl in GetInflatedInterfaces (type)) {
				foreach (MethodReference interfaceMethod in interfaceImpl.InflatedInterface.GetMethods (context)) {
					MethodDefinition? resolvedInterfaceMethod = context.TryResolve (interfaceMethod);
					if (resolvedInterfaceMethod == null)
						continue;

					// TODO-NICE: if the interface method is implemented explicitly (with an override),
					// we shouldn't need to run the below logic. This results in ILLink potentially
					// keeping more methods than needed.

					if (!resolvedInterfaceMethod.IsVirtual
						|| resolvedInterfaceMethod.IsFinal)
						continue;

					// Static methods on interfaces must be implemented only via explicit method-impl record
					// not by a signature match. So there's no point in running this logic for static methods.
					if (!resolvedInterfaceMethod.IsStatic) {
						// Try to find an implementation with a name/sig match on the current type
						MethodDefinition? exactMatchOnType = TryMatchMethod (type, interfaceMethod);
						if (exactMatchOnType != null) {
							AnnotateMethods (resolvedInterfaceMethod, exactMatchOnType, new (type, interfaceImpl.OriginalImpl.ToArray(), resolvedInterfaceMethod.DeclaringType));
							continue;
						}

						// Next try to find an implementation with a name/sig match in the base hierarchy
						var @base = GetBaseMethodInTypeHierarchy (type, interfaceMethod);
						if (@base != null) {
							AnnotateMethods (resolvedInterfaceMethod, @base, new (type, interfaceImpl.OriginalImpl.ToArray(), resolvedInterfaceMethod.DeclaringType));
							continue;
						}
					}

					// Look for a default implementation last.
					FindAndAddDefaultInterfaceImplementations (type, type, resolvedInterfaceMethod, interfaceImpl.OriginalImpl);
				}
			}
			Queue<(TypeReference InflatedInterface, InterfaceImplementation OriginalImpl)> ifacesToVisit = new (type.GetInflatedInterfaces(context));

			HashSet<TypeReference> visitedIfaces = new([], InflatedInterfaceComparer);

			List<InterfaceImplementation> interfaceImplementations = new (16);
			while (ifacesToVisit.Count > 0) {
				var interfaceImpl = ifacesToVisit.Dequeue ();
				foreach(var iface in interfaceImpl.InflatedInterface.GetInflatedInterfaces (context)) {
					ifacesToVisit.Enqueue (iface);
				}
				if (!visitedIfaces.Add (interfaceImpl.InflatedInterface))
					continue;

				foreach (MethodReference interfaceMethod in interfaceImpl.InflatedInterface.GetMethods (context)) {
					MethodDefinition? resolvedInterfaceMethod = context.TryResolve (interfaceMethod);
					if (resolvedInterfaceMethod == null)
						continue;

					// TODO-NICE: if the interface method is implemented explicitly (with an override),
					// we shouldn't need to run the below logic. This results in ILLink potentially
					// keeping more methods than needed.

					if (!resolvedInterfaceMethod.IsVirtual
						|| resolvedInterfaceMethod.IsFinal)
						continue;

					// Static methods on interfaces must be implemented only via explicit method-impl record
					// not by a signature match. So there's no point in running this logic for static methods.
					if (!resolvedInterfaceMethod.IsStatic) {
						// Try to find an implementation with a name/sig match on the current type
						MethodDefinition? exactMatchOnType = TryMatchMethod (type, interfaceMethod);
						if (exactMatchOnType != null) {
							AnnotateMethods (resolvedInterfaceMethod, exactMatchOnType, new (type, [interfaceImpl.OriginalImpl], resolvedInterfaceMethod.DeclaringType));
							continue;
						}

						// Next try to find an implementation with a name/sig match in the base hierarchy
						var @base = GetBaseMethodInTypeHierarchy (type, interfaceMethod);
						if (@base != null) {
							AnnotateMethods (resolvedInterfaceMethod, @base, new (type, [interfaceImpl.OriginalImpl], resolvedInterfaceMethod.DeclaringType));
							continue;
						}
					}

					// Look for a default implementation last.
					interfaceImplementations.Clear();
					interfaceImplementations.Add (interfaceImpl.OriginalImpl);
					FindAndAddDefaultInterfaceImplementations (type, type, resolvedInterfaceMethod, [interfaceImpl.OriginalImpl]);
				}
			}
		}

		void MapVirtualMethods (TypeDefinition type)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods) {
				// We do not proceed unless a method is virtual or is static
				// A static method with a .override could be implementing a static interface method
				if (!(method.IsStatic || method.IsVirtual))
					continue;

				if (method.IsVirtual)
					MapVirtualMethod (method);

				if (method.HasOverrides)
					MapOverrides (method);
			}
		}

		void MapVirtualMethod (MethodDefinition method)
		{
			MethodDefinition? @base = GetBaseMethodInTypeHierarchy (method);
			if (@base == null)
				return;

			Debug.Assert(!@base.DeclaringType.IsInterface);

			AnnotateMethods (@base, method);
		}

		void MapOverrides (MethodDefinition method)
		{
			foreach (MethodReference baseMethodRef in method.Overrides) {
				MethodDefinition? baseMethod = context.TryResolve (baseMethodRef);
				if (baseMethod == null)
					continue;
				if (!baseMethod.DeclaringType.IsInterface)
				{
					AnnotateMethods (baseMethod, method);
				}
				else
				{
					AnnotateMethods (baseMethod, method, InterfaceImplementor.Create (method.DeclaringType, baseMethod.DeclaringType, context));
				}
			}
		}

		void AnnotateMethods (MethodDefinition @base, MethodDefinition @override, InterfaceImplementor? interfaceImplementor = null)
		{
			AddBaseMethod (@override, @base, interfaceImplementor);
			AddOverride (@base, @override, interfaceImplementor);
		}

		MethodDefinition? GetBaseMethodInTypeHierarchy (MethodDefinition method)
		{
			return GetBaseMethodInTypeHierarchy (method.DeclaringType, method);
		}

		MethodDefinition? GetBaseMethodInTypeHierarchy (TypeDefinition type, MethodReference method)
		{
			TypeReference? @base = GetInflatedBaseType (type);
			while (@base != null) {
				MethodDefinition? base_method = TryMatchMethod (@base, method);
				if (base_method != null)
					return base_method;

				@base = GetInflatedBaseType (@base);
			}

			return null;
		}

		TypeReference? GetInflatedBaseType (TypeReference type)
		{
			if (type == null)
				return null;

			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			if (type is SentinelType sentinelType)
				return GetInflatedBaseType (sentinelType.ElementType);

			if (type is PinnedType pinnedType)
				return GetInflatedBaseType (pinnedType.ElementType);

			if (type is RequiredModifierType requiredModifierType)
				return GetInflatedBaseType (requiredModifierType.ElementType);

			if (type is GenericInstanceType genericInstance) {
				var baseType = context.TryResolve (type)?.BaseType;

				if (baseType is GenericInstanceType)
					return TypeReferenceExtensions.InflateGenericType (genericInstance, baseType, context);

				return baseType;
			}

			return context.TryResolve (type)?.BaseType;
		}

		/// <summary>
		/// Returns a list of default implementations of the given interface method on this type.
		/// Note that this returns a list to potentially cover the diamond case (more than one
		/// most specific implementation of the given interface methods). ILLink needs to preserve
		/// all the implementations so that the proper exception can be thrown at runtime.
		/// </summary>
		/// <param name="type">The type that implements (directly or via a base interface) the declaring interface of <paramref name="interfaceMethod"/></param>
		/// <param name="interfaceMethod">The method to find a default implementation for</param>
		/// <param name="implOfInterface">
		/// The InterfaceImplementation on <paramref name="type"/> that points to the DeclaringType of <paramref name="interfaceMethod"/>.
		/// </param>
		void FindAndAddDefaultInterfaceImplementations (TypeDefinition typeThatImplementsInterface, TypeDefinition typeThatMayHaveDIM, MethodDefinition interfaceMethodToBeImplemented, List<InterfaceImplementation> originalInterfaceImpl)
		{
			// Go over all interfaces, trying to find a method that is an explicit MethodImpl of the
			// interface method in question.
			foreach (var interfaceImpl in typeThatMayHaveDIM.Interfaces) {
				var potentialImplInterface = context.TryResolve (interfaceImpl.InterfaceType);
				if (potentialImplInterface == null)
					continue;

				bool foundImpl = false;

				foreach (var potentialImplMethod in potentialImplInterface.Methods) {
					if (potentialImplMethod == interfaceMethodToBeImplemented &&
						!potentialImplMethod.IsAbstract) {
						AddDefaultInterfaceImplementation (interfaceMethodToBeImplemented, new (typeThatImplementsInterface, originalInterfaceImpl.ToArray(), interfaceMethodToBeImplemented.DeclaringType), potentialImplMethod);
						foundImpl = true;
						break;
					}

					if (!potentialImplMethod.HasOverrides)
						continue;

					// This method is an override of something. Let's see if it's the method we are looking for.
					foreach (var baseMethod in potentialImplMethod.Overrides) {
						if (context.TryResolve (baseMethod) == interfaceMethodToBeImplemented) {
							AddDefaultInterfaceImplementation (interfaceMethodToBeImplemented, new (typeThatImplementsInterface, originalInterfaceImpl.ToArray(), interfaceMethodToBeImplemented.DeclaringType), @potentialImplMethod);
							foundImpl = true;
							break;
						}
					}

					if (foundImpl) {
						break;
					}
				}

				// We haven't found a MethodImpl on the current interface, but one of the interfaces
				// this interface requires could still provide it.
				if (!foundImpl) {
					var oldLength = originalInterfaceImpl.Count;
					originalInterfaceImpl.Add (interfaceImpl);
					FindAndAddDefaultInterfaceImplementations (typeThatImplementsInterface, potentialImplInterface, interfaceMethodToBeImplemented, originalInterfaceImpl);
					originalInterfaceImpl.RemoveRange(oldLength, originalInterfaceImpl.Count -  oldLength);
				}
			}
		}

		MethodDefinition? TryMatchMethod (TypeReference type, MethodReference method)
		{
			foreach (var candidate in type.GetMethods (context)) {
				var md = context.TryResolve (candidate);
				if (md?.IsVirtual != true)
					continue;

				if (MethodMatch (candidate, method))
					return md;
			}

			return null;
		}

		[SuppressMessage ("ApiDesign", "RS0030:Do not used banned APIs", Justification = "It's best to leave working code alone.")]
		bool MethodMatch (MethodReference candidate, MethodReference method)
		{
			if (candidate.HasParameters != method.HasMetadataParameters ())
				return false;

			if (candidate.Name != method.Name)
				return false;

			if (candidate.HasGenericParameters != method.HasGenericParameters)
				return false;

			// we need to track what the generic parameter represent - as we cannot allow it to
			// differ between the return type or any parameter
			if (candidate.GetReturnType (context) is not TypeReference candidateReturnType ||
				method.GetReturnType (context) is not TypeReference methodReturnType ||
				!TypeMatch (candidateReturnType, methodReturnType))
				return false;

			if (!candidate.HasMetadataParameters ())
				return true;

			var cp = candidate.Parameters;
			var mp = method.Parameters;
			if (cp.Count != mp.Count)
				return false;

			if (candidate.GenericParameters.Count != method.GenericParameters.Count)
				return false;

			for (int i = 0; i < cp.Count; i++) {
				if (candidate.GetInflatedParameterType (i, context) is not TypeReference candidateParameterType ||
					method.GetInflatedParameterType (i, context) is not TypeReference methodParameterType ||
					!TypeMatch (candidateParameterType, methodParameterType))
					return false;
			}

			return true;
		}

		static bool TypeMatch (IModifierType a, IModifierType b)
		{
			if (!TypeMatch (a.ModifierType, b.ModifierType))
				return false;

			return TypeMatch (a.ElementType, b.ElementType);
		}

		static bool TypeMatch (TypeSpecification a, TypeSpecification b)
		{
			if (a is GenericInstanceType gita)
				return TypeMatch (gita, (GenericInstanceType) b);

			if (a is IModifierType mta)
				return TypeMatch (mta, (IModifierType) b);

			if (a is FunctionPointerType fpta)
				return TypeMatch (fpta, (FunctionPointerType) b);

			return TypeMatch (a.ElementType, b.ElementType);
		}

		static bool TypeMatch (GenericInstanceType a, GenericInstanceType b)
		{
			if (!TypeMatch (a.ElementType, b.ElementType))
				return false;

			if (a.HasGenericArguments != b.HasGenericArguments)
				return false;

			if (!a.HasGenericArguments)
				return true;

			var gaa = a.GenericArguments;
			var gab = b.GenericArguments;
			if (gaa.Count != gab.Count)
				return false;

			for (int i = 0; i < gaa.Count; i++) {
				if (!TypeMatch (gaa[i], gab[i]))
					return false;
			}

			return true;
		}

		static bool TypeMatch (GenericParameter a, GenericParameter b)
		{
			if (a.Position != b.Position)
				return false;

			if (a.Type != b.Type)
				return false;

			return true;
		}

		static bool TypeMatch (FunctionPointerType a, FunctionPointerType b)
		{
			if (a.HasParameters != b.HasParameters)
				return false;

			if (a.CallingConvention != b.CallingConvention)
				return false;

			// we need to track what the generic parameter represent - as we cannot allow it to
			// differ between the return type or any parameter
			if (a.ReturnType is not TypeReference aReturnType ||
				b.ReturnType is not TypeReference bReturnType ||
				!TypeMatch (aReturnType, bReturnType))
				return false;

			if (!a.HasParameters)
				return true;

			var ap = a.Parameters;
			var bp = b.Parameters;
			if (ap.Count != bp.Count)
				return false;

			for (int i = 0; i < ap.Count; i++) {
				if (a.Parameters[i].ParameterType is not TypeReference aParameterType ||
					b.Parameters[i].ParameterType is not TypeReference bParameterType ||
					!TypeMatch (aParameterType, bParameterType))
					return false;
			}

			return true;
		}

		static bool TypeMatch (TypeReference a, TypeReference b)
		{
			if (a is TypeSpecification || b is TypeSpecification) {
				if (a.GetType () != b.GetType ())
					return false;

				return TypeMatch ((TypeSpecification) a, (TypeSpecification) b);
			}

			if (a is GenericParameter genericParameterA && b is GenericParameter genericParameterB)
				return TypeMatch (genericParameterA, genericParameterB);

			return a.FullName == b.FullName;
		}
	}
}
