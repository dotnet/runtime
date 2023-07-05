// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	///  Generates a signature for a member, in the format used for C# Documentation Comments:
	///  https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
	///  Adapted from Roslyn's DocumentationCommentIDVisitor:
	///  https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/DocumentationComments/DocumentationCommentIDVisitor.cs
	/// </summary>
	public sealed partial class DocumentationSignatureGenerator
	{
		internal const string MethodPrefix = "M:";
		internal const string FieldPrefix = "F:";
		internal const string EventPrefix = "E:";
		internal const string PropertyPrefix = "P:";
		internal const string TypePrefix = "T:";

		private DocumentationSignatureGenerator ()
		{
		}

		public static void VisitMember (IMemberDefinition member, StringBuilder builder, ITryResolveMetadata resolver)
		{
			switch (member.MetadataToken.TokenType) {
			case TokenType.TypeDef:
				VisitTypeDefinition ((TypeDefinition) member, builder, resolver);
				break;
			case TokenType.Method:
				VisitMethod ((MethodDefinition) member, builder, resolver);
				break;
			case TokenType.Property:
				VisitProperty ((PropertyDefinition) member, builder, resolver);
				break;
			case TokenType.Field:
				VisitField ((FieldDefinition) member, builder, resolver);
				break;
			case TokenType.Event:
				VisitEvent ((EventDefinition) member, builder, resolver);
				break;
			default:
				break;
			}
		}

		private static void VisitMethod (MethodDefinition method, StringBuilder builder, ITryResolveMetadata resolver)
		{
			builder.Append (MethodPrefix);
			PartVisitor.Instance.VisitMethodDefinition (method, builder, resolver);
		}

		private static void VisitField (FieldDefinition field, StringBuilder builder, ITryResolveMetadata resolver)
		{
			builder.Append (FieldPrefix);
			PartVisitor.Instance.VisitField (field, builder, resolver);
		}

		private static void VisitEvent (EventDefinition evt, StringBuilder builder, ITryResolveMetadata resolver)
		{
			builder.Append (EventPrefix);
			PartVisitor.Instance.VisitEvent (evt, builder, resolver);
		}

		private static void VisitProperty (PropertyDefinition property, StringBuilder builder, ITryResolveMetadata resolver)
		{
			builder.Append (PropertyPrefix);
			PartVisitor.Instance.VisitProperty (property, builder, resolver);
		}

		private static void VisitTypeDefinition (TypeDefinition type, StringBuilder builder, ITryResolveMetadata resolver)
		{
			builder.Append (TypePrefix);
			PartVisitor.Instance.VisitTypeReference (type, builder, resolver);
		}
	}
}
