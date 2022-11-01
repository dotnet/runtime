// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	public static class MethodBodyScanner
	{
		public static bool IsWorthConvertingToThrow (MethodIL body)
		{
			// Some bodies are cheaper size wise to leave alone than to convert to a throw
			Instruction? previousMeaningful = null;
			int meaningfulCount = 0;
			foreach (var ins in body.Instructions) {
				// Handle ignoring noops because because (1) it's a valid case to ignore
				// and (2) When running the tests on .net core roslyn tosses in no ops
				// and that leads to a difference in test results between mcs and .net framework csc.
				if (ins.OpCode.Code == Code.Nop)
					continue;

				meaningfulCount++;

				if (meaningfulCount == 1 && ins.OpCode.Code == Code.Ret)
					return false;

				if (meaningfulCount == 2 && ins.OpCode.Code == Code.Ret && previousMeaningful != null) {
					if (previousMeaningful.OpCode.StackBehaviourPop == StackBehaviour.Pop0) {
						switch (previousMeaningful.OpCode.StackBehaviourPush) {
						case StackBehaviour.Pushi:
						case StackBehaviour.Pushi8:
						case StackBehaviour.Pushr4:
						case StackBehaviour.Pushr8:
							return false;
						}

						switch (previousMeaningful.OpCode.Code) {
						case Code.Ldnull:
							return false;
						}
					}
				}

				if (meaningfulCount >= 2)
					return true;

				previousMeaningful = ins;
			}

			return true;
		}
	}
	readonly struct InterfacesOnStackScanner
	{
		readonly LinkContext context;

		public InterfacesOnStackScanner (LinkContext context)
		{
			this.context = context;
		}

		public IEnumerable<(InterfaceImplementation, TypeDefinition)>? GetReferencedInterfaces (MethodIL methodIL)
		{
			var possibleStackTypes = AllPossibleStackTypes (methodIL);
			if (possibleStackTypes.Count == 0)
				return null;

			var interfaceTypes = possibleStackTypes.Where (t => t.IsInterface).ToArray ();
			if (interfaceTypes.Length == 0)
				return null;

			var interfaceImplementations = new HashSet<(InterfaceImplementation, TypeDefinition)> ();

			// If a type could be on the stack in the body and an interface it implements could be on the stack on the body
			// then we need to mark that interface implementation.  When this occurs it is not safe to remove the interface implementation from the type
			// even if the type is never instantiated
			foreach (var type in possibleStackTypes) {
				// We only sweep interfaces on classes so that's why we only care about classes
				if (!type.IsClass)
					continue;

				TypeDefinition? currentType = type;
				while (currentType?.BaseType != null) // Checking BaseType != null to skip System.Object
				{
					AddMatchingInterfaces (interfaceImplementations, currentType, interfaceTypes);
					currentType = context.TryResolve (currentType.BaseType);
				}
			}

			return interfaceImplementations;
		}

		HashSet<TypeDefinition> AllPossibleStackTypes (MethodIL methodIL)
		{
			var types = new HashSet<TypeDefinition> ();

			foreach (VariableDefinition var in methodIL.Variables)
				AddIfResolved (types, var.VariableType);

			foreach (var param in methodIL.Method.GetParameters ())
				AddIfResolved (types, param.ParameterType);

			foreach (ExceptionHandler eh in methodIL.ExceptionHandlers) {
				if (eh.HandlerType == ExceptionHandlerType.Catch) {
					AddIfResolved (types, eh.CatchType);
				}
			}

			foreach (Instruction instruction in methodIL.Instructions) {
				if (instruction.Operand is FieldReference fieldReference) {
					if (context.TryResolve (fieldReference)?.FieldType is TypeReference fieldType)
						AddIfResolved (types, fieldType);
				} else if (instruction.Operand is MethodReference methodReference) {
					if (methodReference is GenericInstanceMethod genericInstanceMethod)
						AddFromGenericInstance (types, genericInstanceMethod);

					if (methodReference.DeclaringType is GenericInstanceType genericInstanceType)
						AddFromGenericInstance (types, genericInstanceType);

					var resolvedMethod = context.TryResolve (methodReference);
					if (resolvedMethod != null) {
						if (resolvedMethod.HasMetadataParameters ()) {
							foreach (var param in resolvedMethod.GetParameters ())
								AddIfResolved (types, param.ParameterType);
						}

						AddFromGenericParameterProvider (types, resolvedMethod);
						AddFromGenericParameterProvider (types, resolvedMethod.DeclaringType);
						AddIfResolved (types, resolvedMethod.ReturnType);
					}
				}
			}


			return types;
		}

		void AddMatchingInterfaces (HashSet<(InterfaceImplementation, TypeDefinition)> results, TypeDefinition type, TypeDefinition[] interfaceTypes)
		{
			if (!type.HasInterfaces)
				return;

			foreach (var interfaceType in interfaceTypes) {
				if (HasInterface (type, interfaceType, out InterfaceImplementation? implementation))
					results.Add ((implementation, type));
			}
		}

		bool HasInterface (TypeDefinition type, TypeDefinition interfaceType, [NotNullWhen (true)] out InterfaceImplementation? implementation)
		{
			implementation = null;
			if (!type.HasInterfaces)
				return false;

			foreach (var iface in type.Interfaces) {
				if (context.TryResolve (iface.InterfaceType) == interfaceType) {
					implementation = iface;
					return true;
				}
			}

			return false;
		}

		void AddFromGenericInstance (HashSet<TypeDefinition> set, IGenericInstance instance)
		{
			if (!instance.HasGenericArguments)
				return;

			foreach (var genericArgument in instance.GenericArguments)
				AddIfResolved (set, genericArgument);
		}

		void AddFromGenericParameterProvider (HashSet<TypeDefinition> set, IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (var genericParameter in provider.GenericParameters) {
				foreach (var constraint in genericParameter.Constraints)
					AddIfResolved (set, constraint.ConstraintType);
			}
		}

		void AddIfResolved (HashSet<TypeDefinition> set, TypeReference item)
		{
			var resolved = context.TryResolve (item);
			if (resolved == null)
				return;

			set.Add (resolved);
		}
	}
}