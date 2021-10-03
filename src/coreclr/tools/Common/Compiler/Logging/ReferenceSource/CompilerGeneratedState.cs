// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	// Currently this is implemented using heuristics
	public class CompilerGeneratedState
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, MethodDefinition> _compilerGeneratedTypeToUserCodeMethod;
		readonly HashSet<TypeDefinition> _typesWithPopulatedCache;

		public CompilerGeneratedState (LinkContext context)
		{
			_context = context;
			_compilerGeneratedTypeToUserCodeMethod = new Dictionary<TypeDefinition, MethodDefinition> ();
			_typesWithPopulatedCache = new HashSet<TypeDefinition> ();
		}

		static bool HasRoslynCompilerGeneratedName (TypeDefinition type) =>
			type.Name.Contains ('<') || (type.DeclaringType != null && HasRoslynCompilerGeneratedName (type.DeclaringType));

		void PopulateCacheForType (TypeDefinition type)
		{
			// Avoid repeat scans of the same type
			if (!_typesWithPopulatedCache.Add (type))
				return;

			foreach (MethodDefinition method in type.Methods) {
				if (!method.HasCustomAttributes)
					continue;

				foreach (var attribute in method.CustomAttributes) {
					if (attribute.AttributeType.Namespace != "System.Runtime.CompilerServices")
						continue;

					switch (attribute.AttributeType.Name) {
					case "AsyncIteratorStateMachineAttribute":
					case "AsyncStateMachineAttribute":
					case "IteratorStateMachineAttribute":
						TypeDefinition stateMachineType = GetFirstConstructorArgumentAsType (attribute);
						if (stateMachineType != null) {
							if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd (stateMachineType, method)) {
								var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
								_context.LogWarning (
									$"Methods '{method.GetDisplayName ()}' and '{alreadyAssociatedMethod.GetDisplayName ()}' are both associated with state machine type '{stateMachineType.GetDisplayName ()}'. This is currently unsupported and may lead to incorrectly reported warnings.",
									2107,
									new MessageOrigin (method),
									MessageSubCategory.TrimAnalysis);
							}
						}

						break;
					}
				}
			}
		}

		static TypeDefinition GetFirstConstructorArgumentAsType (CustomAttribute attribute)
		{
			if (!attribute.HasConstructorArguments)
				return null;

			return attribute.ConstructorArguments[0].Value as TypeDefinition;
		}

		public MethodDefinition GetUserDefinedMethodForCompilerGeneratedMember (IMemberDefinition sourceMember)
		{
			if (sourceMember == null)
				return null;

			TypeDefinition compilerGeneratedType = (sourceMember as TypeDefinition) ?? sourceMember.DeclaringType;
			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (compilerGeneratedType, out MethodDefinition userDefinedMethod))
				return userDefinedMethod;

			// Only handle async or iterator state machine
			// So go to the declaring type and check if it's compiler generated (as a perf optimization)
			if (!HasRoslynCompilerGeneratedName (compilerGeneratedType) || compilerGeneratedType.DeclaringType == null)
				return null;

			// Now go to its declaring type and search all methods to find the one which points to the type as its
			// state machine implementation.
			PopulateCacheForType (compilerGeneratedType.DeclaringType);
			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (compilerGeneratedType, out userDefinedMethod))
				return userDefinedMethod;

			return null;
		}
	}
}
