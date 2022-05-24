// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	// Currently this is implemented using heuristics
	public class CompilerGeneratedState
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, MethodDefinition> _compilerGeneratedTypeToUserCodeMethod;
		readonly Dictionary<TypeDefinition, TypeArgumentInfo> _generatedTypeToTypeArgumentInfo;
		readonly record struct TypeArgumentInfo (
			/// <summary>The method which calls the ctor for the given type</summary>
			MethodDefinition CreatingMethod,
			/// <summary>Attributes for the type, pulled from the creators type arguments</summary>
			IReadOnlyList<ICustomAttributeProvider>? OriginalAttributes);

		readonly Dictionary<MethodDefinition, MethodDefinition> _compilerGeneratedMethodToUserCodeMethod;

		readonly HashSet<TypeDefinition> _typesWithPopulatedCache;

		public CompilerGeneratedState (LinkContext context)
		{
			_context = context;
			_compilerGeneratedTypeToUserCodeMethod = new Dictionary<TypeDefinition, MethodDefinition> ();
			_generatedTypeToTypeArgumentInfo = new Dictionary<TypeDefinition, TypeArgumentInfo> ();
			_compilerGeneratedMethodToUserCodeMethod = new Dictionary<MethodDefinition, MethodDefinition> ();
			_typesWithPopulatedCache = new HashSet<TypeDefinition> ();
		}

		static IEnumerable<TypeDefinition> GetCompilerGeneratedNestedTypes (TypeDefinition type)
		{
			foreach (var nestedType in type.NestedTypes) {
				if (!CompilerGeneratedNames.IsGeneratedMemberName (nestedType.Name))
					continue;

				yield return nestedType;

				foreach (var recursiveNestedType in GetCompilerGeneratedNestedTypes (nestedType))
					yield return recursiveNestedType;
			}
		}

		/// <summary>
		/// Walks the type and its descendents to find Roslyn-compiler generated
		/// code and gather information to map it back to original user code. If
		/// a compiler-generated type is passed in directly, this method will walk
		/// up and find the nearest containing user type. Returns the nearest user type,
		/// or null if none was found.
		/// </summary>
		TypeDefinition? PopulateCacheForType (TypeDefinition type)
		{
			var originalType = type;

			// Look in the declaring type if this is a compiler-generated type (state machine or display class).
			// State machines can be emitted into display classes, so we may also need to go one more level up.
			// To avoid depending on implementation details, we go up until we see a non-compiler-generated type.
			// This is the counterpart to GetCompilerGeneratedNestedTypes.
			while (type != null && CompilerGeneratedNames.IsGeneratedMemberName (type.Name))
				type = type.DeclaringType;

			if (type is null)
				return null;

			// Avoid repeat scans of the same type
			if (!_typesWithPopulatedCache.Add (type))
				return type;

			var callGraph = new CompilerGeneratedCallGraph ();
			var callingMethods = new HashSet<MethodDefinition> ();

			void ProcessMethod (MethodDefinition method)
			{
				bool isStateMachineMember = CompilerGeneratedNames.IsStateMachineType (method.DeclaringType.Name);
				if (!CompilerGeneratedNames.IsLambdaOrLocalFunction (method.Name)) {
					if (!isStateMachineMember) {
						// If it's not a nested function, track as an entry point to the call graph.
						var added = callingMethods.Add (method);
						Debug.Assert (added);
					}
				} else {
					// We don't expect lambdas or local functions to be emitted directly into
					// state machine types.
					Debug.Assert (!isStateMachineMember);
				}

				// Discover calls or references to lambdas or local functions. This includes
				// calls to local functions, and lambda assignments (which use ldftn).
				if (method.Body != null) {
					foreach (var instruction in method.Body.Instructions) {
						if (instruction.OpCode.OperandType != OperandType.InlineMethod)
							continue;

						MethodDefinition? lambdaOrLocalFunction = _context.TryResolve ((MethodReference) instruction.Operand);
						if (lambdaOrLocalFunction == null)
							continue;

						if (lambdaOrLocalFunction.IsConstructor &&
							lambdaOrLocalFunction.DeclaringType is var generatedType &&
							// Don't consider calls in the same type, like inside a static constructor
							method.DeclaringType != generatedType &&
							CompilerGeneratedNames.IsLambdaDisplayClass (generatedType.Name)) {
							// fill in null for now, attribute providers will be filled in later
							if (!_generatedTypeToTypeArgumentInfo.TryAdd (generatedType, new TypeArgumentInfo (method, null))) {
								var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[generatedType];
								_context.LogWarning (new MessageOrigin (method), DiagnosticId.MethodsAreAssociatedWithUserMethod, method.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), generatedType.GetDisplayName ());
							}
							continue;
						}

						if (!CompilerGeneratedNames.IsLambdaOrLocalFunction (lambdaOrLocalFunction.Name))
							continue;

						if (isStateMachineMember) {
							callGraph.TrackCall (method.DeclaringType, lambdaOrLocalFunction);
						} else {
							callGraph.TrackCall (method, lambdaOrLocalFunction);
						}
					}
				}

				// Discover state machine methods.
				if (!method.HasCustomAttributes)
					return;

				foreach (var attribute in method.CustomAttributes) {
					if (attribute.AttributeType.Namespace != "System.Runtime.CompilerServices")
						continue;

					switch (attribute.AttributeType.Name) {
					case "AsyncIteratorStateMachineAttribute":
					case "AsyncStateMachineAttribute":
					case "IteratorStateMachineAttribute":
						TypeDefinition? stateMachineType = GetFirstConstructorArgumentAsType (attribute);
						if (stateMachineType == null)
							break;
						Debug.Assert (stateMachineType.DeclaringType == type ||
							(CompilerGeneratedNames.IsGeneratedMemberName (stateMachineType.DeclaringType.Name) &&
							 stateMachineType.DeclaringType.DeclaringType == type));
						callGraph.TrackCall (method, stateMachineType);
						if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd (stateMachineType, method)) {
							var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
							_context.LogWarning (new MessageOrigin (method), DiagnosticId.MethodsAreAssociatedWithStateMachine, method.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), stateMachineType.GetDisplayName ());
						}
						// Already warned above if multiple methods map to the same type
						// Fill in null for argument providers now, the real providers will be filled in later
						_ = _generatedTypeToTypeArgumentInfo.TryAdd (stateMachineType, new TypeArgumentInfo (method, null));
						break;
					}
				}
			}

			// Look for state machine methods, and methods which call local functions.
			foreach (MethodDefinition method in type.Methods)
				ProcessMethod (method);

			// Also scan compiler-generated state machine methods (in case they have calls to nested functions),
			// and nested functions inside compiler-generated closures (in case they call other nested functions).

			// State machines can be emitted into lambda display classes, so we need to go down at least two
			// levels to find calls from iterator nested functions to other nested functions. We just recurse into
			// all compiler-generated nested types to avoid depending on implementation details.

			foreach (var nestedType in GetCompilerGeneratedNestedTypes (type)) {
				foreach (var method in nestedType.Methods)
					ProcessMethod (method);
			}

			// Now we've discovered the call graphs for calls to nested functions.
			// Use this to map back from nested functions to the declaring user methods.

			// Note: This maps all nested functions back to the user code, not to the immediately
			// declaring local function. The IL doesn't contain enough information in general for
			// us to determine the nesting of local functions and lambdas.

			// Note: this only discovers nested functions which are referenced from the user
			// code or its referenced nested functions. There is no reliable way to determine from
			// IL which user code an unused nested function belongs to.
			foreach (var userDefinedMethod in callingMethods) {
				foreach (var compilerGeneratedMember in callGraph.GetReachableMembers (userDefinedMethod)) {
					switch (compilerGeneratedMember) {
					case MethodDefinition nestedFunction:
						Debug.Assert (CompilerGeneratedNames.IsLambdaOrLocalFunction (nestedFunction.Name));
						// Nested functions get suppressions from the user method only.
						if (!_compilerGeneratedMethodToUserCodeMethod.TryAdd (nestedFunction, userDefinedMethod)) {
							var alreadyAssociatedMethod = _compilerGeneratedMethodToUserCodeMethod[nestedFunction];
							_context.LogWarning (new MessageOrigin (userDefinedMethod), DiagnosticId.MethodsAreAssociatedWithUserMethod, userDefinedMethod.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), nestedFunction.GetDisplayName ());
						}
						break;
					case TypeDefinition stateMachineType:
						// Types in the call graph are always state machine types
						// For those all their methods are not tracked explicitly in the call graph; instead, they
						// are represented by the state machine type itself.
						// We are already tracking the association of the state machine type to the user code method
						// above, so no need to track it here.
						Debug.Assert (CompilerGeneratedNames.IsStateMachineType (stateMachineType.Name));
						break;
					default:
						throw new InvalidOperationException ();
					}
				}
			}

			// Now that we have instantiating methods fully filled out, walk the generated types and fill in the attribute
			// providers
			foreach (var generatedType in _generatedTypeToTypeArgumentInfo.Keys) {
				if (HasGenericParameters (generatedType))
					MapGeneratedTypeTypeParameters (generatedType);
			}

			return type;

			/// <summary>
			/// Check if the type itself is generic. The only difference is that
			/// if the type is a nested type, the generic parameters from its
			/// parent type don't count.
			/// </summary>
			static bool HasGenericParameters (TypeDefinition typeDef)
			{
				if (!typeDef.IsNested)
					return typeDef.HasGenericParameters;

				return typeDef.GenericParameters.Count > typeDef.DeclaringType.GenericParameters.Count;
			}

			void MapGeneratedTypeTypeParameters (TypeDefinition generatedType)
			{
				Debug.Assert (CompilerGeneratedNames.IsGeneratedType (generatedType.Name));

				var typeInfo = _generatedTypeToTypeArgumentInfo[generatedType];
				if (typeInfo.OriginalAttributes is not null) {
					return;
				}
				var method = typeInfo.CreatingMethod;
				if (method.Body is { } body) {
					var typeArgs = new ICustomAttributeProvider[generatedType.GenericParameters.Count];
					var typeRef = ScanForInit (generatedType, body);
					if (typeRef is null) {
						return;
					}
					for (int i = 0; i < typeRef.GenericArguments.Count; i++) {
						var typeArg = typeRef.GenericArguments[i];
						// Start with the existing parameters, in case we can't find the mapped one
						ICustomAttributeProvider userAttrs = generatedType.GenericParameters[i];
						// The type parameters of the state machine types are alpha renames of the
						// the method parameters, so the type ref should always be a GenericParameter. However,
						// in the case of nesting, there may be multiple renames, so if the parameter is a method
						// we know we're done, but if it's another state machine, we have to keep looking to find
						// the original owner of that state machine.
						if (typeArg is GenericParameter { Owner: { } owner } param) {
							if (owner is MethodReference) {
								userAttrs = param;
							} else {
								// Must be a type ref
								var owningRef = (TypeReference) owner;
								if (!CompilerGeneratedNames.IsGeneratedType (owningRef.Name)) {
									userAttrs = param;
								} else if (_context.TryResolve ((TypeReference) param.Owner) is { } owningType) {
									MapGeneratedTypeTypeParameters (owningType);
									if (_generatedTypeToTypeArgumentInfo[owningType].OriginalAttributes is { } owningAttrs) {
										userAttrs = owningAttrs[param.Position];
									} else {
										Debug.Assert (false, "This should be impossible in valid code");
									}
								}
							}
						}

						typeArgs[i] = userAttrs;
					}
					_generatedTypeToTypeArgumentInfo[generatedType] = typeInfo with { OriginalAttributes = typeArgs };
				}
			}

			GenericInstanceType? ScanForInit (TypeDefinition stateMachineType, MethodBody body)
			{
				foreach (var instr in body.Instructions) {
					switch (instr.OpCode.Code) {
					case Code.Initobj:
					case Code.Newobj:
						if (instr.Operand is MethodReference { DeclaringType: GenericInstanceType typeRef }
							&& stateMachineType == _context.TryResolve (typeRef)) {
							return typeRef;
						}
						break;
					}
				}
				return null;
			}
		}

		static TypeDefinition? GetFirstConstructorArgumentAsType (CustomAttribute attribute)
		{
			if (!attribute.HasConstructorArguments)
				return null;

			return attribute.ConstructorArguments[0].Value as TypeDefinition;
		}

		/// <summary>
		/// Gets the attributes on the "original" method of a generated type, i.e. the
		/// attributes on the corresponding type parameters from the owning method.
		/// </summary>
		public IReadOnlyList<ICustomAttributeProvider>? GetGeneratedTypeAttributes (TypeDefinition generatedType)
		{
			Debug.Assert (CompilerGeneratedNames.IsGeneratedType (generatedType.Name));

			var typeToCache = PopulateCacheForType (generatedType);
			if (typeToCache is null)
				return null;

			if (_generatedTypeToTypeArgumentInfo.TryGetValue (generatedType, out var typeInfo)) {
				return typeInfo.OriginalAttributes;
			}
			return null;
		}

		// For state machine types/members, maps back to the state machine method.
		// For local functions and lambdas, maps back to the owning method in user code (not the declaring
		// lambda or local function, because the IL doesn't contain enough information to figure this out).
		public bool TryGetOwningMethodForCompilerGeneratedMember (IMemberDefinition sourceMember, [NotNullWhen (true)] out MethodDefinition? owningMethod)
		{
			owningMethod = null;
			if (sourceMember == null)
				return false;

			MethodDefinition? compilerGeneratedMethod = sourceMember as MethodDefinition;
			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out owningMethod))
					return true;
			}

			TypeDefinition sourceType = (sourceMember as TypeDefinition) ?? sourceMember.DeclaringType;

			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (sourceType, out owningMethod))
				return true;

			if (!CompilerGeneratedNames.IsGeneratedMemberName (sourceMember.Name) && !CompilerGeneratedNames.IsGeneratedMemberName (sourceType.Name))
				return false;

			// sourceType is a state machine type, or the type containing a lambda or local function.
			// Search all methods to find the one which points to the type as its
			// state machine implementation.
			var typeToCache = PopulateCacheForType (sourceType);
			if (typeToCache is null)
				return false;

			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out owningMethod))
					return true;
			}

			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (sourceType, out owningMethod))
				return true;

			return false;
		}
	}
}
