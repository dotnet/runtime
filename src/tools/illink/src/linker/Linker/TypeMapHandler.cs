// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker
{
	class TypeMapHandler : IMarkHandler
	{
		readonly TypeMapResolver _lazyTypeMapResolver;

		readonly Dictionary<TypeReference, List<CustomAttribute>> _unmarkedTypeMapEntries = [];

		LinkContext _context = null!;

		public TypeMapHandler (AssemblyDefinition entryPointAssembly)
		{
			HashSet<AssemblyNameReference> assemblies = [];
			foreach (var attr in entryPointAssembly.CustomAttributes) {
				if (attr.AttributeType is not GenericInstanceType {
					Namespace: "System.Runtime.InteropServices",
					GenericArguments: [_]
				}) {
					continue; // Only interested in System.Runtime.InteropServices attributes
				}

				if (attr.AttributeType.Name is "TypeMapAttribute`1" or "TypeMapAssociation`1") {
					assemblies.Add (AssemblyNameReference.Parse (entryPointAssembly.FullName));
					continue;
				} else if (attr.AttributeType.Name != "TypeMapAssemblyTarget`1") {
					// Not a type map assembly target, skip it.
					continue;
				}

				if (attr.ConstructorArguments[0].Value is not string str) {
					// Invalid attribute, skip it.
					// Let the runtime handle the failure.
					continue;
				}

				assemblies.Add (AssemblyNameReference.Parse (str));
			}

			_lazyTypeMapResolver = new TypeMapResolver (assemblies);
		}

		public void Initialize (LinkContext context, MarkContext markContext)
		{
			_context = context;
			_lazyTypeMapResolver.Resolve (context, this);
			markContext.RegisterMarkTypeAction (ProcessType);
		}

		void ProcessType (TypeDefinition definition)
		{
			// Process any unmarked type map entries for this type
			if (_unmarkedTypeMapEntries.TryGetValue (definition, out List<CustomAttribute>? entries)) {
				foreach (var entry in entries) {
					_context.Annotations.Mark (entry, new DependencyInfo (DependencyKind.TypeMapEntry, definition));
				}
				_unmarkedTypeMapEntries.Remove (definition);
			}
		}

		void AddExternalTypeMapEntry (CustomAttribute attr)
		{
			if (attr.ConstructorArguments is [_, _, { Value: TypeReference trimTarget }]) {
				RecordTypeMapEntry (attr, trimTarget);
				return;
			}
			if (attr.ConstructorArguments is [_, { Value: TypeReference target }]) {
				// This is a TypeMapAssemblyTargetAttribute, which has a single type argument.
				RecordTypeMapEntry (attr, target);
				return;
			}
			// Invalid attribute, skip it.
			// Let the runtime handle the failure.
		}

		void AddProxyTypeMapEntry (CustomAttribute attr)
		{
			if (attr.ConstructorArguments is [{ Value: TypeReference sourceType }, _]) {
				// This is a TypeMapAssociationAttribute, which has a single type argument.
				RecordTypeMapEntry (attr, sourceType);
				return;
			}
		}

		void RecordTypeMapEntry (CustomAttribute attr, TypeReference trimTarget)
		{
			if (_context.Annotations.IsMarked (trimTarget)) {
				_context.Annotations.Mark (attr, new DependencyInfo (DependencyKind.TypeMapEntry, trimTarget));
			} else {
				if (!_unmarkedTypeMapEntries.TryGetValue (trimTarget, out List<CustomAttribute>? entries)) {
					entries = [];
					_unmarkedTypeMapEntries[trimTarget] = entries;
				}
				entries.Add (attr);
			}
		}

		class TypeMapResolver (IReadOnlySet<AssemblyNameReference> assemblies)
		{
			public void Resolve (LinkContext context, TypeMapHandler manager)
			{
				foreach (AssemblyNameReference assemblyName in assemblies) {
					if (context.TryResolve(assemblyName) is not AssemblyDefinition assembly) {
						// If we cannot find the assembly, skip it.
						// We'll fail at runtime as expected.
						continue;
					}
					foreach (var attr in assembly.CustomAttributes) {
						if (attr.AttributeType is not GenericInstanceType {
							Namespace: "System.Runtime.InteropServices",
							GenericArguments: [_]
						}) {
							continue; // Only interested in System.Runtime.InteropServices attributes
						}

						if (attr.AttributeType.Name is "TypeMapAttribute`1") {
							manager.AddExternalTypeMapEntry (attr);
						} else if (attr.AttributeType.Name is "TypeMapAssociationAttribute`1") {
							manager.AddProxyTypeMapEntry (attr);
						}
					}
				}
			}
		}
	}
}
