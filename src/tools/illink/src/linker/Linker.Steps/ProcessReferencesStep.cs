// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using ILLink.Shared;

namespace Mono.Linker.Steps
{
	public class ProcessReferencesStep : BaseStep
	{
		protected override void Process ()
		{
			// Walk over all -reference inputs and resolve any that may need to be rooted.

			// For example:
			// -reference dir/Unreferenced.dll --action copy --trim-mode copyused
			//     In this case we need to check whether Unreferenced has the
			//     IsTrimmable attribute, and root it if not.
			// -reference dir/Unreferenced.dll --action copy --trim-mode copyused --action link Unreferenced
			//     The per-assembly action wins over the default --action or --trim-mode,
			//     so we don't need to load the assembly to check for IsTrimmable attribute.
			// -reference dir/Unreferenced.dll --action link --trim-mode link
			//     In this case, we don't need to load the assembly up-front, because it will
			//     not get the copy/save action, regardless of the IsTrimmable attribute.

			// Note that we don't do the same for assemblies which may be resolved from input directories - such
			// assemblies will only be rooted if something loads them.
			foreach (var assemblyPath in GetInputAssemblyPaths ()) {
				var assemblyName = Path.GetFileNameWithoutExtension (assemblyPath);

				// If there's no way that this reference could have the copy/save action,
				// we don't need to load it up-front.
				if (!MaybeIsFullyPreservedAssembly (assemblyName))
					continue;

				// For the remaining references, we need to resolve them (which looks for IsTrimmable attribute)
				// to determine the action.
				var assembly = Context.TryResolve (assemblyName);
				if (assembly == null) {
					Context.LogError (null, DiagnosticId.ReferenceAssemblyCouldNotBeLoaded, assemblyPath);
					continue;
				}

				// If the assigned action (now taking into account the IsTrimmable attribute) requires us
				// to root the assembly, do so.
				if (IsFullyPreservedAction (Annotations.GetAction (assembly)))
					Annotations.Mark (assembly.MainModule, new DependencyInfo (DependencyKind.AssemblyAction, assembly), new MessageOrigin (assembly));
			}
		}

		IEnumerable<string> GetInputAssemblyPaths ()
		{
			var assemblies = new HashSet<string> ();
			foreach (var referencePath in Context.Resolver.GetReferencePaths ()) {
				var assemblyName = Path.GetFileNameWithoutExtension (referencePath);
				if (assemblies.Add (assemblyName))
					yield return referencePath;
			}
		}

		public static bool IsFullyPreservedAction (AssemblyAction action)
		{
			return action == AssemblyAction.Copy || action == AssemblyAction.Save;
		}

		bool MaybeIsFullyPreservedAssembly (string assemblyName)
		{
			if (Context.Actions.TryGetValue (assemblyName, out AssemblyAction action))
				return IsFullyPreservedAction (action);

			return IsFullyPreservedAction (Context.DefaultAction) || IsFullyPreservedAction (Context.TrimAction);
		}
	}
}
