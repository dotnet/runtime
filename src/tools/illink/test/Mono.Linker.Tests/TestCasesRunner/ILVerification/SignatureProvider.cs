// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace Mono.Linker.Tests.TestCasesRunner.ILVerification;

public class SignatureProvider : ISignatureTypeProvider<string, object>
{
	public string GetPrimitiveType (PrimitiveTypeCode typeCode)
		=> typeCode switch {
			PrimitiveTypeCode.Boolean => "bool",
			PrimitiveTypeCode.Byte => "uint8",
			PrimitiveTypeCode.Char => "char",
			PrimitiveTypeCode.Double => "float64",
			PrimitiveTypeCode.Int16 => "int16",
			PrimitiveTypeCode.Int32 => "int32",
			PrimitiveTypeCode.Int64 => "int64",
			PrimitiveTypeCode.IntPtr => "native int",
			PrimitiveTypeCode.Object => "object",
			PrimitiveTypeCode.SByte => "int8",
			PrimitiveTypeCode.Single => "float32",
			PrimitiveTypeCode.String => "string",
			PrimitiveTypeCode.TypedReference => "typedref",
			PrimitiveTypeCode.UInt16 => "uint16",
			PrimitiveTypeCode.UInt32 => "uint32",
			PrimitiveTypeCode.UInt64 => "uint64",
			PrimitiveTypeCode.UIntPtr => "native uint",
			PrimitiveTypeCode.Void => "void",
			_ => "<bad metadata>"
		};

	public string GetTypeFromDefinition (MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
		=> handle.GetTypeFullName (reader);

	public string GetTypeFromReference (MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
		=> handle.GetTypeFullName (reader);

	public string GetTypeFromSpecification (MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind = 0)
		=> throw new NotImplementedException ();

	public string GetSZArrayType (string elementType)
		=> elementType + "[]";

	public string GetPointerType (string elementType)
		=> elementType + "*";

	public string GetByReferenceType (string elementType)
		=> elementType + "&";

	public string GetGenericMethodParameter (object genericContext, int index)
		=> "!!" + index;

	public string GetGenericTypeParameter (object genericContext, int index)
		=> "!" + index;

	public string GetPinnedType (string elementType)
		=> elementType + " pinned";

	public string GetGenericInstantiation (string genericType, ImmutableArray<string> typeArguments)
		=> genericType + "<" + string.Join (",", typeArguments) + ">";

	public string GetModifiedType (string modifierType, string unmodifiedType, bool isRequired)
		=> unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";

	public string GetArrayType (string elementType, ArrayShape shape)
	{
		var builder = new StringBuilder ();

		builder.Append (elementType);
		builder.Append ('[');

		for (int i = 0; i < shape.Rank; i++) {
			int lowerBound = 0;

			if (i < shape.LowerBounds.Length) {
				lowerBound = shape.LowerBounds[i];
				builder.Append (lowerBound);
			}

			builder.Append ("...");

			if (i < shape.Sizes.Length) {
				builder.Append (lowerBound + shape.Sizes[i] - 1);
			}

			if (i < shape.Rank - 1) {
				builder.Append (',');
			}
		}

		builder.Append (']');
		return builder.ToString ();
	}

	public string GetFunctionPointerType (MethodSignature<string> signature)
		=> $"{signature.ReturnType} *({string.Join (",", signature.ParameterTypes)})";
}
