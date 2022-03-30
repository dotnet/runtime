// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		readonly Dictionary<MethodDefinition, MethodDefinition> _compilerGeneratedMethodToUserCodeMethod;
		readonly HashSet<TypeDefinition> _typesWithPopulatedCache;

		public CompilerGeneratedState (LinkContext context)
		{
			_context = context;
			_compilerGeneratedTypeToUserCodeMethod = new Dictionary<TypeDefinition, MethodDefinition> ();
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

		void PopulateCacheForType (TypeDefinition type)
		{
			// Avoid repeat scans of the same type
			if (!_typesWithPopulatedCache.Add (type))
				return;

			var callGraph = new CallGraph ();
			var callingMethods = new HashSet<MethodDefinition> ();

			void ProcessMethod (MethodDefinition method)
			{
				if (!CompilerGeneratedNames.IsLambdaOrLocalFunction (method.Name)) {
					// If it's not a nested function, track as an entry point to the call graph.
					var added = callingMethods.Add (method);
					Debug.Assert (added);
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

						if (!CompilerGeneratedNames.IsLambdaOrLocalFunction (lambdaOrLocalFunction.Name))
							continue;

						callGraph.TrackCall (method, lambdaOrLocalFunction);
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
						if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd (stateMachineType, method)) {
							var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
							_context.LogWarning (new MessageOrigin (method), DiagnosticId.MethodsAreAssociatedWithStateMachine, method.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), stateMachineType.GetDisplayName ());
						}

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
				foreach (var nestedFunction in callGraph.GetReachableMethods (userDefinedMethod)) {
					Debug.Assert (CompilerGeneratedNames.IsLambdaOrLocalFunction (nestedFunction.Name));
					_compilerGeneratedMethodToUserCodeMethod.Add (nestedFunction, userDefinedMethod);
				}
			}
		}

		static TypeDefinition? GetFirstConstructorArgumentAsType (CustomAttribute attribute)
		{
			if (!attribute.HasConstructorArguments)
				return null;

			return attribute.ConstructorArguments[0].Value as TypeDefinition;
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
			var typeToCache = sourceType;

			// Look in the declaring type if this is a compiler-generated type (state machine or display class).
			// State machines can be emitted into display classes, so we may also need to go one more level up.
			// To avoid depending on implementation details, we go up until we see a non-compiler-generated type.
			// This is the counterpart to GetCompilerGeneratedNestedTypes.
			while (typeToCache != null && CompilerGeneratedNames.IsGeneratedMemberName (typeToCache.Name))
				typeToCache = typeToCache.DeclaringType;

			if (typeToCache == null)
				return false;

			// Search all methods to find the one which points to the type as its
			// state machine implementation.
			PopulateCacheForType (typeToCache);
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
