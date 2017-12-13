using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Linker
{
	static class TypeReferenceExtensions
	{
		public static TypeReference GetInflatedBaseType (this TypeReference type)
		{
			if (type == null)
				return null;

			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			var sentinelType = type as SentinelType;
			if (sentinelType != null)
				return sentinelType.ElementType.GetInflatedBaseType ();

			var pinnedType = type as PinnedType;
			if (pinnedType != null)
				return pinnedType.ElementType.GetInflatedBaseType ();

			var requiredModifierType = type as RequiredModifierType;
			if (requiredModifierType != null)
				return requiredModifierType.ElementType.GetInflatedBaseType ();

			var genericInstance = type as GenericInstanceType;
			if (genericInstance != null) {
				var baseType = type.Resolve ().BaseType;
				var baseTypeGenericInstance = baseType as GenericInstanceType;

				if (baseTypeGenericInstance != null)
					return InflateGenericType (genericInstance, baseType);

				return baseType;
			}

			return type.Resolve ().BaseType;
		}

		public static IEnumerable<TypeReference> GetInflatedInterfaces (this TypeReference typeRef)
		{
			var typeDef = typeRef.Resolve ();

			if (!typeDef.HasInterfaces)
				yield break;

			var genericInstance = typeRef as GenericInstanceType;
			if (genericInstance != null) {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return InflateGenericType (genericInstance, interfaceImpl.InterfaceType);
			} else {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return interfaceImpl.InterfaceType;
			}
		}

		public static TypeReference InflateGenericType (GenericInstanceType genericInstanceProvider, TypeReference typeToInflate)
		{
			var arrayType = typeToInflate as ArrayType;
			if (arrayType != null) {
				var inflatedElementType = InflateGenericType (genericInstanceProvider, arrayType.ElementType);

				if (inflatedElementType != arrayType.ElementType)
					return new ArrayType (inflatedElementType, arrayType.Rank);

				return arrayType;
			}

			var genericInst = typeToInflate as GenericInstanceType;
			if (genericInst != null)
				return MakeGenericType (genericInstanceProvider, genericInst);

			var genericParameter = typeToInflate as GenericParameter;
			if (genericParameter != null) {
				if (genericParameter.Owner is MethodReference)
					return genericParameter;

				var elementType = genericInstanceProvider.ElementType.Resolve ();
				var parameter = elementType.GenericParameters.Single (p => p == genericParameter);
				return genericInstanceProvider.GenericArguments [parameter.Position];
			}

			var functionPointerType = typeToInflate as FunctionPointerType;
			if (functionPointerType != null) {
				var result = new FunctionPointerType ();
				result.ReturnType = InflateGenericType (genericInstanceProvider, functionPointerType.ReturnType);

				for (int i = 0; i < functionPointerType.Parameters.Count; i++) {
					var inflatedParameterType = InflateGenericType(genericInstanceProvider, functionPointerType.Parameters [i].ParameterType);
					result.Parameters.Add (new ParameterDefinition (inflatedParameterType));
				}

				return result;
			}

			var modifierType = typeToInflate as IModifierType;
			if (modifierType != null) {
				var modifier = InflateGenericType (genericInstanceProvider, modifierType.ModifierType);
				var elementType = InflateGenericType (genericInstanceProvider, modifierType.ElementType);

				if (modifierType is OptionalModifierType) {
					return new OptionalModifierType (modifier, elementType);
				}

				return new RequiredModifierType (modifier, elementType);
			}

			var pinnedType = typeToInflate as PinnedType;
			if (pinnedType != null) {
				var elementType = InflateGenericType (genericInstanceProvider, pinnedType.ElementType);

				if (elementType != pinnedType.ElementType)
					return new PinnedType (elementType);

				return pinnedType;
			}

			var pointerType = typeToInflate as PointerType;
			if (pointerType != null) {
				var elementType = InflateGenericType (genericInstanceProvider, pointerType.ElementType);

				if (elementType != pointerType.ElementType)
					return new PointerType (elementType);

				return pointerType;
			}

			var byReferenceType = typeToInflate as ByReferenceType;
			if (byReferenceType != null) {
				var elementType = InflateGenericType (genericInstanceProvider, byReferenceType.ElementType);

				if (elementType != byReferenceType.ElementType)
					return new ByReferenceType (elementType);

				return byReferenceType;
			}

			var sentinelType = typeToInflate as SentinelType;
			if (sentinelType != null) {
				var elementType = InflateGenericType (genericInstanceProvider, sentinelType.ElementType);

				if (elementType != sentinelType.ElementType)
					return new SentinelType (elementType);

				return sentinelType;
			}

			return typeToInflate;
		}

		private static GenericInstanceType MakeGenericType (GenericInstanceType genericInstanceProvider, GenericInstanceType type)
		{
			var result = new GenericInstanceType (type.ElementType);

			for (var i = 0; i < type.GenericArguments.Count; ++i) {
				result.GenericArguments.Add (InflateGenericType (genericInstanceProvider, type.GenericArguments [i]));
			}

			return result;
		}

		public static IEnumerable<MethodReference> GetMethods (this TypeReference type)
		{
			var typeDef = type.Resolve ();

			if (!typeDef.HasMethods)
				yield break;

			var genericInstanceType = type as GenericInstanceType;
			if (genericInstanceType != null) {
				foreach (var methodDef in typeDef.Methods)
					yield return MakeMethodReferenceForGenericInstanceType (genericInstanceType, methodDef);
			} else {
				foreach (var method in typeDef.Methods)
					yield return method;
			}
		}

		private static MethodReference MakeMethodReferenceForGenericInstanceType (GenericInstanceType genericInstanceType, MethodDefinition methodDef)
		{
			var method = new MethodReference (methodDef.Name, methodDef.ReturnType, genericInstanceType) {
				HasThis = methodDef.HasThis,
				ExplicitThis = methodDef.ExplicitThis,
				CallingConvention = methodDef.CallingConvention
			};

			foreach (var parameter in methodDef.Parameters)
				method.Parameters.Add (new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));

			foreach (var gp in methodDef.GenericParameters)
				method.GenericParameters.Add (new GenericParameter (gp.Name, method));

			return method;
		}
	}
}
