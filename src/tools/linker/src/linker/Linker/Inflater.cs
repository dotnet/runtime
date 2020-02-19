using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker {
	class Inflater {
		public static TypeReference InflateType(GenericContext context, TypeReference typeReference)
		{
			var typeDefinition = InflateTypeWithoutException(context, typeReference);
			if (typeDefinition == null)
				throw new InvalidOperationException($"Unable to resolve a reference to the type '{typeReference.FullName}' in the assembly '{typeReference.Module.Assembly.FullName}'. Does this type exist in a different assembly in the project?");
	
			return typeDefinition;
		}
	
		public static GenericInstanceType InflateType(GenericContext context, TypeDefinition typeDefinition)
		{
			return ConstructGenericType(context, typeDefinition, typeDefinition.GenericParameters);
		}
	
		public static GenericInstanceType InflateType(GenericContext context, GenericInstanceType genericInstanceType)
		{
			var inflatedType = ConstructGenericType(context, genericInstanceType.Resolve(), genericInstanceType.GenericArguments);
			inflatedType.MetadataToken = genericInstanceType.MetadataToken;
			return inflatedType;
		}
	
		public static TypeReference InflateTypeWithoutException(GenericContext context, TypeReference typeReference)
		{
			if (typeReference is GenericParameter genericParameter)
			{
				if (context.Method == null && genericParameter.Type != GenericParameterType.Type)
				{
					// If no method is specified assume only partial inflation is desired.
					return typeReference;
				}
				
				var genericArgumentType = genericParameter.Type == GenericParameterType.Type
					? context.Type.GenericArguments[genericParameter.Position]
					: context.Method.GenericArguments[genericParameter.Position];
	
				var inflatedType = genericArgumentType;
				return inflatedType;
			}
			if (typeReference is GenericInstanceType genericInstanceType)
				return InflateType (context, genericInstanceType);

			if (typeReference is ArrayType arrayType)
				return new ArrayType(InflateType(context, arrayType.ElementType), arrayType.Rank);
	
			if (typeReference is ByReferenceType byReferenceType)
				return new ByReferenceType(InflateType(context, byReferenceType.ElementType));
	
			if (typeReference is PointerType pointerType)
				return new PointerType(InflateType(context, pointerType.ElementType));
	
			if (typeReference is RequiredModifierType reqModType)
				return InflateTypeWithoutException(context, reqModType.ElementType);
	
			if (typeReference is OptionalModifierType optModType)
				return InflateTypeWithoutException(context, optModType.ElementType);
	
			return typeReference.Resolve();
		}
	
		static GenericInstanceType ConstructGenericType(GenericContext context, TypeDefinition typeDefinition, IEnumerable<TypeReference> genericArguments)
		{
			var inflatedType = new GenericInstanceType(typeDefinition);
	
			foreach (var genericArgument in genericArguments)
				inflatedType.GenericArguments.Add(InflateType(context, genericArgument));
	
			return inflatedType;
		}

		public class GenericContext
		{
			private readonly GenericInstanceType _type;
			private readonly GenericInstanceMethod _method;

			public GenericContext(GenericInstanceType type, GenericInstanceMethod method)
			{
				_type = type;
				_method = method;
			}

			public GenericInstanceType Type
			{
				get { return _type; }
			}

			public GenericInstanceMethod Method
			{
				get { return _method; }
			}
		}
	}
}