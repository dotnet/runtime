// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using ILCompiler;
using Internal.IL.Stubs;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;
using Mono.Cecil;
using MetadataType = Internal.TypeSystem.MetadataType;

namespace Mono.Linker.Tests.TestCasesRunner
{
	internal readonly struct AssemblyQualifiedToken : IEquatable<AssemblyQualifiedToken>
	{
		public string? AssemblyName { get; }
		public int Token { get; }

		public AssemblyQualifiedToken (string? assemblyName, int token) => (AssemblyName, Token) = (assemblyName, token);

		public AssemblyQualifiedToken (TypeSystemEntity entity) =>
			(AssemblyName, Token) = entity switch {
				EcmaType type => (type.Module.Assembly.GetName ().Name, MetadataTokens.GetToken (type.Handle)),
				EcmaMethod method => (method.Module.Assembly.GetName ().Name, MetadataTokens.GetToken (method.Handle)),
				EcmaField field => (field.Module.Assembly.GetName ().Name, MetadataTokens.GetToken (field.Handle)),
				PropertyPseudoDesc property => (((EcmaType) property.OwningType).Module.Assembly.GetName ().Name, MetadataTokens.GetToken (property.Handle)),
				EventPseudoDesc @event => (((EcmaType) @event.OwningType).Module.Assembly.GetName ().Name, MetadataTokens.GetToken (@event.Handle)),
				ILStubMethod => (null, 0), // Ignore compiler generated methods
				MetadataType mt when mt.GetType().Name == "BoxedValueType" => (null, 0),
				_ => throw new NotSupportedException ($"The infra doesn't support getting a token for {entity} yet.")
			};

		public AssemblyQualifiedToken (IMemberDefinition member) =>
			(AssemblyName, Token) = member switch {
				TypeDefinition type => (type.Module.Assembly.Name.Name, type.MetadataToken.ToInt32 ()),
				MethodDefinition method => (method.Module.Assembly.Name.Name, method.MetadataToken.ToInt32 ()),
				PropertyDefinition property => (property.Module.Assembly.Name.Name, property.MetadataToken.ToInt32 ()),
				EventDefinition @event => (@event.Module.Assembly.Name.Name, @event.MetadataToken.ToInt32 ()),
				FieldDefinition field => (field.Module.Assembly.Name.Name, field.MetadataToken.ToInt32 ()),
				_ => throw new NotSupportedException ($"The infra doesn't support getting a token for {member} yet.")
			};

		public override int GetHashCode () => AssemblyName == null ? 0 : AssemblyName.GetHashCode () ^ Token.GetHashCode ();
		public override string ToString () => $"{AssemblyName}: {Token}";
		public bool Equals (AssemblyQualifiedToken other) =>
			string.CompareOrdinal (AssemblyName, other.AssemblyName) == 0 && Token == other.Token;
		public override bool Equals ([NotNullWhen (true)] object? obj) => ((AssemblyQualifiedToken?) obj)?.Equals (this) == true;

		public bool IsNil => AssemblyName == null;
	}
}
