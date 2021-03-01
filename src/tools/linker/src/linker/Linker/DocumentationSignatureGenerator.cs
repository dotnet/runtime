// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

		public static void VisitMember (IMemberDefinition member, StringBuilder builder)
		{
			switch (member.MetadataToken.TokenType) {
			case TokenType.TypeDef:
				VisitTypeDefinition (member as TypeDefinition, builder);
				break;
			case TokenType.Method:
				VisitMethod (member as MethodDefinition, builder);
				break;
			case TokenType.Property:
				VisitProperty (member as PropertyDefinition, builder);
				break;
			case TokenType.Field:
				VisitField (member as FieldDefinition, builder);
				break;
			case TokenType.Event:
				VisitEvent (member as EventDefinition, builder);
				break;
			default:
				break;
			}
		}

		private static void VisitMethod (MethodDefinition method, StringBuilder builder)
		{
			builder.Append (MethodPrefix);
			PartVisitor.Instance.VisitMethodDefinition (method, builder);
		}

		private static void VisitField (FieldDefinition field, StringBuilder builder)
		{
			builder.Append (FieldPrefix);
			PartVisitor.Instance.VisitField (field, builder);
		}

		private static void VisitEvent (EventDefinition evt, StringBuilder builder)
		{
			builder.Append (EventPrefix);
			PartVisitor.Instance.VisitEvent (evt, builder);
		}

		private static void VisitProperty (PropertyDefinition property, StringBuilder builder)
		{
			builder.Append (PropertyPrefix);
			PartVisitor.Instance.VisitProperty (property, builder);
		}

		private static void VisitTypeDefinition (TypeDefinition type, StringBuilder builder)
		{
			builder.Append (TypePrefix);
			PartVisitor.Instance.VisitTypeReference (type, builder);
		}
	}
}