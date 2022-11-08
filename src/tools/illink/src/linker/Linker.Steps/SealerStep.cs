// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	public class SealerStep : BaseStep
	{
		HashSet<TypeDefinition>? referencedBaseTypeCache;

		public SealerStep ()
		{
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (!Context.CanApplyOptimization (CodeOptimizations.Sealer, assembly))
				return;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		protected override void EndProcess ()
		{
			referencedBaseTypeCache = null;
		}

		bool IsSubclassed (TypeDefinition type)
		{
			if (referencedBaseTypeCache == null) {
				referencedBaseTypeCache = new HashSet<TypeDefinition> ();
				foreach (var a in Context.GetAssemblies ()) {
					if (!Annotations.IsMarked (a))
						continue;

					PopulateCache (a.MainModule.Types);

					void PopulateCache (Collection<TypeDefinition> types)
					{
						foreach (var t in types) {
							var btd = Context.TryResolve (t.BaseType);
							if (btd != null)
								referencedBaseTypeCache.Add (btd);

							if (t.HasNestedTypes) {
								PopulateCache (t.NestedTypes);
							}
						}
					}
				}
			}

			var bt = Context.TryResolve (type);
			return bt != null && referencedBaseTypeCache.Contains (bt);
		}

		void ProcessType (TypeDefinition type)
		{
			if (type.HasNestedTypes) {
				foreach (var nt in type.NestedTypes) {
					ProcessType (nt);
				}
			}

			//
			// interface members are virtual (and we cannot change this)
			//
			if (type.IsInterface)
				return;

			//
			// the code does not include any subclass for this type
			//
			if (!type.IsAbstract && !type.IsSealed && !IsSubclassed (type))
				SealType (type);

			if (!type.HasMethods)
				return;

			// process methods to see if we can seal or devirtualize them
			foreach (var method in type.Methods) {
				if (method.IsFinal || !method.IsVirtual || method.IsAbstract || method.IsRuntime)
					continue;

				Debug.Assert (Annotations.IsMarked (method));
				if (!Annotations.IsMarked (method))
					continue;

				var overrides = Annotations.GetOverrides (method);

				//
				// cannot de-virtualize nor seal methods if something overrides them
				//
				if (IsAnyOverrideMarked (overrides))
					continue;

				SealMethod (method);

				// subclasses might need this method to satisfy an interface requirement
				// and requires dispatch/virtual support
				if (!type.IsSealed)
					continue;

				var bases = Annotations.GetBaseMethods (method);
				// Devirtualize if a method is not override to existing marked methods
				if (!IsAnyBaseMarked (bases))
					method.IsVirtual = method.IsFinal = method.IsNewSlot = false;
			}
		}

		protected virtual void SealType (TypeDefinition type)
		{
			type.IsSealed = true;
		}

		protected virtual void SealMethod (MethodDefinition method)
		{
			method.IsFinal = true;
		}

		bool IsAnyOverrideMarked (IEnumerable<OverrideInformation>? list)
		{
			if (list == null)
				return false;

			foreach (var m in list) {
				if (Annotations.IsMarked (m.Override))
					return true;
			}
			return false;
		}

		bool IsAnyBaseMarked (IEnumerable<OverrideInformation>? list)
		{
			if (list == null)
				return false;

			foreach (var m in list) {
				if (Annotations.IsMarked (m.Base))
					return true;
			}
			return false;
		}
	}
}
