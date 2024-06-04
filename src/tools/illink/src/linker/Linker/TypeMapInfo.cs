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

using System.Collections.Generic;
using System.Collections.Immutable;
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
		public List<OverrideInformation>? GetOverrides (MethodDefinition method)
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

		public void AddBaseMethod (MethodDefinition method, MethodDefinition @base, RuntimeInterfaceImplementation? runtimeInterface)
		{
			base_methods.AddToList (method, new OverrideInformation (@base, method, runtimeInterface));
		}

		public void AddOverride (MethodDefinition @base, MethodDefinition @override, RuntimeInterfaceImplementation? runtimeInterface = null)
		{
			override_methods.AddToList (@base, new OverrideInformation (@base, @override, runtimeInterface));
		}

		public void AddDefaultInterfaceImplementation (MethodDefinition @base, RuntimeInterfaceImplementation runtimeInterface, MethodDefinition defaultImplementationMethod)
		{
			Debug.Assert (@base.DeclaringType.IsInterface);
			default_interface_implementations.AddToList (@base, new OverrideInformation (@base, defaultImplementationMethod, runtimeInterface));
		}

		Dictionary<TypeDefinition, ImmutableArray<RuntimeInterfaceImplementation>> interfaces = new ();
		protected virtual void MapType (TypeDefinition type)
		{
			interfaces[type] = GetRecursiveInterfaceImplementations (type);
			MapVirtualMethods (type);
			MapInterfaceMethodsInTypeHierarchy (type);

			if (!type.HasNestedTypes)
				return;

			foreach (var nested in type.NestedTypes)
				MapType (nested);
		}

		internal ImmutableArray<RuntimeInterfaceImplementation>? GetRecursiveInterfaces (TypeDefinition type)
		{
			EnsureProcessed (type.Module.Assembly);
			if (interfaces.TryGetValue (type, out var value))
				return value;
			return null;
		}

		ImmutableArray<RuntimeInterfaceImplementation> GetRecursiveInterfaceImplementations (TypeDefinition type)
		{
			// We'll need at least this many interfaces, so let's preallocate
			var runtimeIfaces = ImmutableArray.CreateBuilder<RuntimeInterfaceImplementation> (type.Interfaces.Count);

			AddRecursiveInterfaces (type, type, [], runtimeIfaces, context);

			return runtimeIfaces.ToImmutable ();

			static void AddRecursiveInterfaces (TypeDefinition originalType, TypeReference typeRef, IEnumerable<InterfaceImplementation> pathToType, ImmutableArray<RuntimeInterfaceImplementation>.Builder implementors, LinkContext Context)
			{
				var type = Context.TryResolve (typeRef);
				if (type is null)
					return;
				// Get all explicit interfaces of this type
				foreach (var iface in type.Interfaces) {
					var interfaceType = iface.InterfaceType.TryInflateFrom (typeRef, Context);
					if (interfaceType is null) {
						continue;
					}
					if (!implementors.Any (i => TypeReferenceEqualityComparer.AreEqual (i.InflatedInterfaceType, interfaceType, Context))) {
						implementors.Add (new RuntimeInterfaceImplementation (originalType, typeRef, pathToType.Append (iface), interfaceType, Context));
					}
				}

				if (type.BaseType is not null) {
					var inflatedBaseType = type.BaseType.TryInflateFrom (typeRef, Context)!;
					AddRecursiveInterfaces (originalType, inflatedBaseType, [], implementors, Context);
				}

				// Recursive interfaces after all direct interfaces to preserve Inherit/Implement tree order
				foreach (var iface in type.Interfaces) {
					// If we can't resolve the interface type we can't find recursive interfaces
					var ifaceDirectlyOnType = iface.InterfaceType.TryInflateFrom (typeRef, Context);
					if (ifaceDirectlyOnType is null) {
						continue;
					}
					AddRecursiveInterfaces (originalType, ifaceDirectlyOnType, pathToType.Append (iface), implementors, Context);
				}
			}
		}

		void MapInterfaceMethodsInTypeHierarchy (TypeDefinition type)
		{
			var inflatedInterfaces = interfaces[type];
			// Foreach interface and for each newslot virtual method on the interface, try
			// to find the method implementation and record it.
			foreach (var interfaceImpl in inflatedInterfaces) {
				foreach ((MethodReference interfaceMethod, MethodDefinition resolvedInterfaceMethod) in interfaceImpl.InflatedInterfaceType.GetMethods (context)) {
					// TODO-NICE: if the interface method is implemented explicitly (with an override),
					// we shouldn't need to run the below logic. This results in ILLink potentially
					// keeping more methods than needed.

					if (!resolvedInterfaceMethod.IsVirtual
						|| resolvedInterfaceMethod.IsFinal)
						continue;

					// Note: methods with overrides are annotated in MapVirtualMethods

					// Static methods on interfaces must be implemented only via explicit method-impl record
					// not by a signature match. So there's no point in running this logic for static methods.
					if (!resolvedInterfaceMethod.IsStatic) {
						// Try to find an implementation with a name/sig match on the current type
						MethodDefinition? exactMatchOnType = TryMatchMethod (type, interfaceMethod);
						if (exactMatchOnType != null) {
							AnnotateMethods (resolvedInterfaceMethod, exactMatchOnType, interfaceImpl);
							continue;
						}

						// Next try to find an implementation with a name/sig match in the base hierarchy
						var @base = GetBaseMethodInTypeHierarchy (type, interfaceMethod);
						if (@base != null) {
							AnnotateMethods (resolvedInterfaceMethod, @base, interfaceImpl);
							continue;
						}
					}

					// Look for a default implementation last.
					FindAndAddDefaultInterfaceImplementations (interfaceImpl, interfaceMethod, resolvedInterfaceMethod);
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

			Debug.Assert (!@base.DeclaringType.IsInterface);

			AnnotateMethods (@base, method);
		}

		void MapOverrides (MethodDefinition method)
		{
			foreach (MethodReference baseMethodRef in method.Overrides) {
				MethodDefinition? baseMethod = context.TryResolve (baseMethodRef);
				if (baseMethod == null)
					continue;
				if (baseMethod.DeclaringType.IsInterface) {
					var found = interfaces.TryGetValue (method.DeclaringType, out var runtimeInterfaces);
					Debug.Assert (found);
					var runtimeInterface = runtimeInterfaces.Single (i => TypeReferenceEqualityComparer.AreEqual (i.InflatedInterfaceType, baseMethodRef.DeclaringType, context));
					AnnotateMethods (baseMethod, method, runtimeInterface);
				} else {
					AnnotateMethods (baseMethod, method);
				}
			}
		}

		void AnnotateMethods (MethodDefinition @base, MethodDefinition @override, RuntimeInterfaceImplementation? runtimeInterface = null)
		{
			AddBaseMethod (@override, @base, runtimeInterface);
			AddOverride (@base, @override, runtimeInterface);
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
		void FindAndAddDefaultInterfaceImplementations (RuntimeInterfaceImplementation originalInterfaceImpl, MethodReference inflatedInterfaceReference, MethodDefinition interfaceMethodToBeImplemented)
		{
			var typeThatImplementsInterface = originalInterfaceImpl.Implementor;
			// Go over all interfaces, trying to find a method that is an explicit MethodImpl of the
			// interface method in question.
			var hasInterfaces = interfaces.TryGetValue (typeThatImplementsInterface, out var runtimeIfaces);
			if (!hasInterfaces)
				return;

			bool findAllPossibleImplementations = inflatedInterfaceReference.DeclaringType is GenericInstanceType;
			foreach (var interfaceImpl in runtimeIfaces!) {
				var potentialImplInterface = interfaceImpl.InterfaceTypeDefinition;
				if (potentialImplInterface is null)
					continue;

				// We go by MethodDefinition here and may miss the correct implementation by exiting early after finding one.
				// To correct for this, we'll look for all possible implementations and add them all if the inflatedIntefaceMethodReference is a genericInstance
				// Otherwise, we're good to stop looking higher in the hierarchy after the first. Note we still need to look at other branches of the hierarchy to find any diamond cases.
				bool foundImpl = false;
				foreach (var potentialImplMethod in potentialImplInterface.Methods) {
					if (potentialImplMethod == interfaceMethodToBeImplemented &&
						!potentialImplMethod.IsAbstract) {
						AddDefaultInterfaceImplementation (interfaceMethodToBeImplemented, originalInterfaceImpl, potentialImplMethod);
						foundImpl = true;
						break;
					}

					if (!potentialImplMethod.HasOverrides)
						continue;

					// This method is an override of something. Let's see if it's the method we are looking for.
					foreach (var baseMethod in potentialImplMethod.Overrides) {
						if (context.TryResolve (baseMethod) == interfaceMethodToBeImplemented) {
							AddDefaultInterfaceImplementation (interfaceMethodToBeImplemented, originalInterfaceImpl, @potentialImplMethod);
							foundImpl = true && !findAllPossibleImplementations;
						}
					}
					if (foundImpl) {
						break;
					}
				}
			}
		}

		MethodDefinition? TryMatchMethod (TypeReference type, MethodReference method)
		{
			foreach (var (candidate, md) in type.GetMethods (context)) {
				if (md.IsVirtual != true)
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
