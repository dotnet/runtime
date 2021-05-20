using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public static class TypeReferenceExtensions
	{
		public static string GetDisplayName (this TypeReference type)
		{
			var builder = GetDisplayNameWithoutNamespace (type);
			var namespaceDisplayName = type.GetNamespaceDisplayName ();
			if (!string.IsNullOrEmpty (namespaceDisplayName)) {
				builder.Insert (0, ".");
				builder.Insert (0, namespaceDisplayName);
			}

			return builder.ToString ();
		}

		public static StringBuilder GetDisplayNameWithoutNamespace (this TypeReference type)
		{
			var sb = new StringBuilder ();
			if (type == null)
				return sb;

			Stack<TypeReference> genericArguments = null;
			while (true) {
				switch (type) {
				case ArrayType arrayType:
					AppendArrayType (arrayType, sb);
					break;
				case GenericInstanceType genericInstanceType:
					genericArguments = new Stack<TypeReference> (genericInstanceType.GenericArguments);
					type = genericInstanceType.ElementType;
					continue;
				default:
					if (type.HasGenericParameters) {
						int genericParametersCount = type.GenericParameters.Count;
						int declaringTypeGenericParametersCount = type.DeclaringType?.GenericParameters?.Count ?? 0;

						string simpleName;
						if (genericParametersCount > declaringTypeGenericParametersCount) {
							if (genericArguments?.Count > 0)
								PrependGenericArguments (genericArguments, genericParametersCount - declaringTypeGenericParametersCount, sb);
							else
								PrependGenericParameters (type.GenericParameters.Skip (declaringTypeGenericParametersCount).ToList (), sb);

							int explicitArityIndex = type.Name.IndexOf ('`');
							simpleName = explicitArityIndex != -1 ? type.Name.Substring (0, explicitArityIndex) : type.Name;
						} else
							simpleName = type.Name;

						sb.Insert (0, simpleName);
						break;
					}

					sb.Insert (0, type.Name);
					break;
				}

				type = type.DeclaringType;
				if (type == null)
					break;

				sb.Insert (0, '.');
			}

			return sb;
		}

		internal static void PrependGenericParameters (IList<GenericParameter> genericParameters, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericParameters[genericParameters.Count - 1]);
			for (int i = genericParameters.Count - 2; i >= 0; i--)
				sb.Insert (0, ',').Insert (0, genericParameters[i]);

			sb.Insert (0, '<');
		}

		static void PrependGenericArguments (Stack<TypeReference> genericArguments, int argumentsToTake, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericArguments.Pop ().GetDisplayNameWithoutNamespace ().ToString ());
			while (--argumentsToTake > 0)
				sb.Insert (0, ',').Insert (0, genericArguments.Pop ().GetDisplayNameWithoutNamespace ().ToString ());

			sb.Insert (0, '<');
		}

		static void AppendArrayType (ArrayType arrayType, StringBuilder sb)
		{
			void parseArrayDimensions (ArrayType at)
			{
				sb.Append ('[');
				for (int i = 0; i < at.Dimensions.Count - 1; i++)
					sb.Append (',');

				sb.Append (']');
			}

			sb.Append (arrayType.Name.Substring (0, arrayType.Name.IndexOf ('[')));
			parseArrayDimensions (arrayType);
			var element = arrayType.ElementType as ArrayType;
			while (element != null) {
				parseArrayDimensions (element);
				element = element.ElementType as ArrayType;
			}
		}

		public static TypeReference GetInflatedDeclaringType (this TypeReference type)
		{
			if (type == null)
				return null;

			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			if (type is SentinelType sentinelType)
				return sentinelType.ElementType.GetInflatedDeclaringType ();

			if (type is PinnedType pinnedType)
				return pinnedType.ElementType.GetInflatedDeclaringType ();

			if (type is RequiredModifierType requiredModifierType)
				return requiredModifierType.ElementType.GetInflatedDeclaringType ();

			if (type is GenericInstanceType genericInstance) {
				var declaringType = genericInstance.DeclaringType;

				if (declaringType.HasGenericParameters) {
					var result = new GenericInstanceType (declaringType);
					for (var i = 0; i < declaringType.GenericParameters.Count; ++i)
						result.GenericArguments.Add (genericInstance.GenericArguments[i]);

					return result;
				}

				return declaringType;
			}

			var resolved = type.Resolve ();
			System.Diagnostics.Debug.Assert (resolved == type);
			return resolved?.DeclaringType;
		}

		public static IEnumerable<(TypeReference InflatedInterface, InterfaceImplementation OriginalImpl)> GetInflatedInterfaces (this TypeReference typeRef)
		{
			var typeDef = typeRef.Resolve ();

			if (typeDef?.HasInterfaces != true)
				yield break;

			if (typeRef is GenericInstanceType genericInstance) {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return (InflateGenericType (genericInstance, interfaceImpl.InterfaceType), interfaceImpl);
			} else {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return (interfaceImpl.InterfaceType, interfaceImpl);
			}
		}

		public static TypeReference InflateGenericType (GenericInstanceType genericInstanceProvider, TypeReference typeToInflate)
		{
			if (typeToInflate is ArrayType arrayType) {
				var inflatedElementType = InflateGenericType (genericInstanceProvider, arrayType.ElementType);

				if (inflatedElementType != arrayType.ElementType)
					return new ArrayType (inflatedElementType, arrayType.Rank);

				return arrayType;
			}

			if (typeToInflate is GenericInstanceType genericInst)
				return MakeGenericType (genericInstanceProvider, genericInst);

			if (typeToInflate is GenericParameter genericParameter) {
				if (genericParameter.Owner is MethodReference)
					return genericParameter;

				var elementType = genericInstanceProvider.ElementType.Resolve ();
				var parameter = elementType.GenericParameters.Single (p => p == genericParameter);
				return genericInstanceProvider.GenericArguments[parameter.Position];
			}

			if (typeToInflate is FunctionPointerType functionPointerType) {
				var result = new FunctionPointerType {
					ReturnType = InflateGenericType (genericInstanceProvider, functionPointerType.ReturnType)
				};

				for (int i = 0; i < functionPointerType.Parameters.Count; i++) {
					var inflatedParameterType = InflateGenericType (genericInstanceProvider, functionPointerType.Parameters[i].ParameterType);
					result.Parameters.Add (new ParameterDefinition (inflatedParameterType));
				}

				return result;
			}

			if (typeToInflate is IModifierType modifierType) {
				var modifier = InflateGenericType (genericInstanceProvider, modifierType.ModifierType);
				var elementType = InflateGenericType (genericInstanceProvider, modifierType.ElementType);

				if (modifierType is OptionalModifierType) {
					return new OptionalModifierType (modifier, elementType);
				}

				return new RequiredModifierType (modifier, elementType);
			}

			if (typeToInflate is PinnedType pinnedType) {
				var elementType = InflateGenericType (genericInstanceProvider, pinnedType.ElementType);

				if (elementType != pinnedType.ElementType)
					return new PinnedType (elementType);

				return pinnedType;
			}

			if (typeToInflate is PointerType pointerType) {
				var elementType = InflateGenericType (genericInstanceProvider, pointerType.ElementType);

				if (elementType != pointerType.ElementType)
					return new PointerType (elementType);

				return pointerType;
			}

			if (typeToInflate is ByReferenceType byReferenceType) {
				var elementType = InflateGenericType (genericInstanceProvider, byReferenceType.ElementType);

				if (elementType != byReferenceType.ElementType)
					return new ByReferenceType (elementType);

				return byReferenceType;
			}

			if (typeToInflate is SentinelType sentinelType) {
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
				result.GenericArguments.Add (InflateGenericType (genericInstanceProvider, type.GenericArguments[i]));
			}

			return result;
		}

		public static IEnumerable<MethodReference> GetMethods (this TypeReference type, LinkContext context)
		{
			TypeDefinition typeDef = context.Resolve (type);
			if (typeDef?.HasMethods != true)
				yield break;

			if (type is GenericInstanceType genericInstanceType) {
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
				method.Parameters.Add (new ParameterDefinition (parameter.Name, parameter.Attributes, parameter.ParameterType));

			foreach (var gp in methodDef.GenericParameters)
				method.GenericParameters.Add (new GenericParameter (gp.Name, method));

			return method;
		}

		public static string ToCecilName (this string fullTypeName)
		{
			return fullTypeName.Replace ('+', '/');
		}

		public static bool HasDefaultConstructor (this TypeDefinition type)
		{
			foreach (var m in type.Methods) {
				if (m.HasParameters)
					continue;

				var definition = m.Resolve ();
				if (definition?.IsDefaultConstructor () == true)
					return true;
			}

			return false;
		}

		public static MethodReference GetDefaultInstanceConstructor (this TypeDefinition type)
		{
			foreach (var m in type.Methods) {
				if (m.HasParameters)
					continue;

				var definition = m.Resolve ();
				if (!definition.IsDefaultConstructor ())
					continue;

				return m;
			}

			throw new NotImplementedException ();
		}

		public static bool IsTypeOf (this TypeReference type, string ns, string name)
		{
			return type.Name == name
				&& type.Namespace == ns;
		}

		public static bool IsTypeOf<T> (this TypeReference tr)
		{
			var type = typeof (T);
			return tr.Name == type.Name && tr.Namespace == tr.Namespace;
		}

		public static bool IsSubclassOf (this TypeReference type, string ns, string name)
		{
			TypeDefinition baseType = type.Resolve ();
			while (baseType != null) {
				if (baseType.IsTypeOf (ns, name))
					return true;
				baseType = baseType.BaseType?.Resolve ();
			}

			return false;
		}
	}
}
