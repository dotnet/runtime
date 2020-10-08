// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{

	public sealed partial class DocumentationSignatureGenerator
	{
		/// <summary>
		///  A visitor that generates the part of the documentation comment after the initial type
		///  and colon.
		///  Adapted from Roslyn's DocumentattionCommentIDVisitor.PartVisitor:
		///  https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.PartVisitor.cs
		/// </summary>
		internal sealed class PartVisitor
		{
			internal static readonly PartVisitor Instance = new PartVisitor ();

			private PartVisitor ()
			{
			}

			public void VisitArrayType (ArrayType arrayType, StringBuilder builder)
			{
				VisitTypeReference (arrayType.ElementType, builder);

				// Rank-one arrays are displayed different than rectangular arrays
				if (arrayType.IsVector) {
					builder.Append ("[]");
				} else {
					// C# arrays only support zero lower bounds
					if (arrayType.Dimensions[0].LowerBound != 0)
						throw new NotImplementedException ();
					builder.Append ("[0:");
					for (int i = 1; i < arrayType.Rank; i++) {
						if (arrayType.Dimensions[0].LowerBound != 0)
							throw new NotImplementedException ();
						builder.Append (",0:");
					}

					builder.Append (']');
				}
			}

			public void VisitField (FieldDefinition field, StringBuilder builder)
			{
				VisitTypeReference (field.DeclaringType, builder);
				builder.Append ('.').Append (field.Name);
			}

			private void VisitParameters (IEnumerable<ParameterDefinition> parameters, bool isVararg, StringBuilder builder)
			{
				builder.Append ('(');
				bool needsComma = false;

				foreach (var parameter in parameters) {
					if (needsComma)
						builder.Append (',');

					// byrefs are tracked on the parameter type, not the parameter,
					// so we don't have VisitParameter that Roslyn uses.
					VisitTypeReference (parameter.ParameterType, builder);
					needsComma = true;
				}

				// note: the C# doc comment generator outputs an extra comma for varargs
				// methods that also have fixed parameters
				if (isVararg && needsComma)
					builder.Append (',');

				builder.Append (')');
			}

			public void VisitMethodDefinition (MethodDefinition method, StringBuilder builder)
			{
				VisitTypeReference (method.DeclaringType, builder);
				builder.Append ('.').Append (GetEscapedMetadataName (method));

				if (method.HasGenericParameters)
					builder.Append ("``").Append (method.GenericParameters.Count);

				if (method.HasParameters || (method.CallingConvention == MethodCallingConvention.VarArg))
					VisitParameters (method.Parameters, method.CallingConvention == MethodCallingConvention.VarArg, builder);

				if (method.Name == "op_Implicit" || method.Name == "op_Explicit") {
					builder.Append ('~');
					VisitTypeReference (method.ReturnType, builder);
				}
			}

			public void VisitProperty (PropertyDefinition property, StringBuilder builder)
			{
				VisitTypeReference (property.DeclaringType, builder);
				builder.Append ('.').Append (GetEscapedMetadataName (property));

				if (property.Parameters.Count > 0)
					VisitParameters (property.Parameters, false, builder);
			}

			public void VisitEvent (EventDefinition evt, StringBuilder builder)
			{
				VisitTypeReference (evt.DeclaringType, builder);
				builder.Append ('.').Append (GetEscapedMetadataName (evt));
			}

			public static void VisitGenericParameter (GenericParameter genericParameter, StringBuilder builder)
			{
				Debug.Assert (genericParameter.DeclaringMethod == null ^ genericParameter.DeclaringType == null);
				// Is this a type parameter on a type?
				if (genericParameter.DeclaringMethod != null) {
					builder.Append ("``");
				} else {
					Debug.Assert (genericParameter.DeclaringType != null);

					// If the containing type is nested within other types.
					// e.g. A<T>.B<U>.M<V>(T t, U u, V v) should be M(`0, `1, ``0).
					// Roslyn needs to add generic arities of parents, but the innermost type redeclares 
					// all generic parameters so we don't need to add them.
					builder.Append ('`');
				}

				builder.Append (genericParameter.Position);
			}

			public void VisitTypeReference (TypeReference typeReference, StringBuilder builder)
			{
				switch (typeReference) {
				case ByReferenceType byReferenceType:
					VisitByReferenceType (byReferenceType, builder);
					return;
				case PointerType pointerType:
					VisitPointerType (pointerType, builder);
					return;
				case ArrayType arrayType:
					VisitArrayType (arrayType, builder);
					return;
				case GenericParameter genericParameter:
					VisitGenericParameter (genericParameter, builder);
					return;
				}

				if (typeReference.IsNested) {
					VisitTypeReference (typeReference.GetInflatedDeclaringType (), builder);
					builder.Append ('.');
				}

				if (!String.IsNullOrEmpty (typeReference.Namespace))
					builder.Append (typeReference.Namespace).Append ('.');

				// This includes '`n' for mangled generic types
				builder.Append (typeReference.Name);

				// For uninstantiated generic types (we already built the mangled name)
				// or non-generic types, we are done.
				if (typeReference.HasGenericParameters || !typeReference.IsGenericInstance)
					return;

				var genericInstance = typeReference as GenericInstanceType;

				// Compute arity counting only the newly-introduced generic parameters
				var declaringType = genericInstance.DeclaringType;
				var declaringArity = 0;
				if (declaringType != null && declaringType.HasGenericParameters)
					declaringArity = declaringType.GenericParameters.Count;
				var totalArity = genericInstance.GenericArguments.Count;
				var arity = totalArity - declaringArity;

				// Un-mangle the generic type name
				var suffixLength = arity.ToString ().Length + 1;
				builder.Remove (builder.Length - suffixLength, suffixLength);

				// Append type arguments excluding arguments for re-declared parent generic parameters
				builder.Append ('{');
				bool needsComma = false;
				for (int i = totalArity - arity; i < totalArity; ++i) {
					if (needsComma)
						builder.Append (',');
					var typeArgument = genericInstance.GenericArguments[i];
					VisitTypeReference (typeArgument, builder);
					needsComma = true;
				}
				builder.Append ('}');
			}

			public void VisitPointerType (PointerType pointerType, StringBuilder builder)
			{
				VisitTypeReference (pointerType.ElementType, builder);
				builder.Append ('*');
			}

			public void VisitByReferenceType (ByReferenceType byReferenceType, StringBuilder builder)
			{
				VisitTypeReference (byReferenceType.ElementType, builder);
				builder.Append ('@');
			}

			private static string GetEscapedMetadataName (IMemberDefinition member)
			{
				var name = member.Name.Replace ('.', '#');
				// Not sure if the following replacements are necessary, but
				// they are included to match Roslyn.
				return name.Replace ('<', '{').Replace ('>', '}');
			}
		}
	}
}