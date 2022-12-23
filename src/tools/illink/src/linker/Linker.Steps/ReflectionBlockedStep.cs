// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ReflectionBlockedStep : BaseStep
	{
		AssemblyDefinition? assembly;
		AssemblyDefinition Assembly {
			get {
				Debug.Assert (assembly != null);
				return assembly;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			this.assembly = assembly;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			if (!HasIndirectCallers (type)) {
				AddCustomAttribute (type);
				return;
			}

			//
			// We mark everything, otherwise we would need to check full visibility
			// hierarchy (e.g. public method inside private type)
			//
			foreach (var method in type.Methods) {
				if (!Annotations.IsIndirectlyCalled (method)) {
					AddCustomAttribute (method);
				}
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		bool HasIndirectCallers (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (Annotations.IsIndirectlyCalled (method))
					return true;
			}

			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes) {
					if (HasIndirectCallers (nested))
						return true;
				}
			}

			return false;
		}

		void AddCustomAttribute (ICustomAttributeProvider caProvider)
		{
			// We are using DisableReflectionAttribute which is not exact match but it's quite
			// close to what we need and it already exists in the BCL
			MethodReference ctor = Context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor ?? throw new InvalidOperationException ();
			ctor = Assembly.MainModule.ImportReference (ctor);

			var ca = new CustomAttribute (ctor);
			caProvider.CustomAttributes.Add (ca);
			Annotations.Mark (ca, new DependencyInfo (DependencyKind.DisablePrivateReflection, ca));
		}
	}
}