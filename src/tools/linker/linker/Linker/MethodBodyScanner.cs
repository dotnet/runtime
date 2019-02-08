using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker {
	public static class MethodBodyScanner {
		public static IEnumerable<InterfaceImplementation> GetReferencedInterfaces (AnnotationStore annotations, MethodBody body)
		{
			var possibleStackTypes = AllPossibleStackTypes (body.Method);
			if (possibleStackTypes.Count == 0)
				return null;

			var interfaceTypes = possibleStackTypes.Where (t => t.IsInterface).ToArray ();
			if (interfaceTypes.Length == 0)
				return null;

			var interfaceImplementations = new HashSet<InterfaceImplementation> ();

			// If a type could be on the stack in the body and an interface it implements could be on the stack on the body
			// then we need to mark that interface implementation.  When this occurs it is not safe to remove the interface implementation from the type
			// even if the type is never instantiated
			foreach (var type in possibleStackTypes) {
				// We only sweep interfaces on classes so that's why we only care about classes
				if (!type.IsClass)
					continue;

				AddMatchingInterfaces (interfaceImplementations, type, interfaceTypes);
				var bases = annotations.GetClassHierarchy (type);
				foreach (var @base in bases) {
					AddMatchingInterfaces (interfaceImplementations, @base, interfaceTypes);
				}
			}

			return interfaceImplementations;
		}

		static HashSet<TypeDefinition> AllPossibleStackTypes (MethodDefinition method)
		{
			if (!method.HasBody)
				throw new ArgumentException();

			var body = method.Body;
			var types = new HashSet<TypeDefinition> ();

			foreach (VariableDefinition var in body.Variables)
				AddIfResolved (types, var.VariableType);

			foreach (ExceptionHandler eh in body.ExceptionHandlers) {
				if (eh.HandlerType == ExceptionHandlerType.Catch) {
					AddIfResolved (types, eh.CatchType);
				}
			}

			foreach (Instruction instruction in body.Instructions) {
				if (instruction.Operand is FieldReference fieldReference) {
					AddIfResolved (types, fieldReference.Resolve ()?.FieldType);
				} else if (instruction.Operand is MethodReference methodReference) {
					if (methodReference is GenericInstanceMethod genericInstanceMethod)
						AddFromGenericInstance (types, genericInstanceMethod);

					if (methodReference.DeclaringType is GenericInstanceType genericInstanceType)
						AddFromGenericInstance (types, genericInstanceType);

					var resolvedMethod = methodReference.Resolve ();
					if (resolvedMethod != null) {
						if (resolvedMethod.HasParameters) {
							foreach (var param in resolvedMethod.Parameters)
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

		static void AddMatchingInterfaces (HashSet<InterfaceImplementation> results, TypeDefinition type, TypeDefinition [] interfaceTypes)
		{
			foreach (var interfaceType in interfaceTypes) {
				if (type.HasInterface (interfaceType, out InterfaceImplementation implementation))
					results.Add (implementation);
			}
		}

		static void AddFromGenericInstance (HashSet<TypeDefinition> set, IGenericInstance instance)
		{
			if (!instance.HasGenericArguments)
				return;

			foreach (var genericArgument in instance.GenericArguments)
				AddIfResolved (set, genericArgument);
		}

		static void AddFromGenericParameterProvider (HashSet<TypeDefinition> set, IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (var genericParameter in provider.GenericParameters) {
				foreach (var constraint in genericParameter.Constraints)
					AddIfResolved (set, constraint);
			}
		}

		static void AddIfResolved (HashSet<TypeDefinition> set, TypeReference item)
		{
			var resolved = item.Resolve ();
			if (resolved == null)
				return;
			set.Add (resolved);
		}
	}
}