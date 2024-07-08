// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct MethodProxy : IEquatable<MethodProxy>
	{
		public MethodProxy (MethodDefinition method) => Method = method;

		public static implicit operator MethodProxy (MethodDefinition method) => new (method);

		public readonly MethodDefinition Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => Method.IsDeclaredOnType (fullTypeName);

		internal partial bool HasMetadataParameters () => Method.HasMetadataParameters ();

		/// <summary>
		/// Gets the number of entries in the 'Parameters' section of a method's metadata (i.e. excludes the implicit 'this' from the count)
		/// </summary>
		internal partial int GetMetadataParametersCount () => Method.GetMetadataParametersCount ();

		/// <summary>
		/// Returns the number of parameters that are passed to the method in IL (including the implicit 'this' parameter).
		/// In pseudocode: <code>method.HasImplicitThis() ? 1 + MetadataParametersCount : MetadataParametersCount;</code>
		/// </summary>
		internal partial int GetParametersCount () => Method.GetParametersCount ();

		/// <summary>
		/// Use only when iterating over all parameters. When wanting to index, use GetParameters(ParameterIndex)
		/// </summary>
		internal partial ParameterProxyEnumerable GetParameters () => Method.GetParameters ();

		internal partial ParameterProxy GetParameter (ParameterIndex index) => Method.GetParameter (index);

		internal partial bool HasGenericParameters () => Method.HasGenericParameters;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.GenericParameters.Count == genericParameterCount;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (!Method.HasGenericParameters)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Method.GenericParameters.Count);
			foreach (var genericParameter in Method.GenericParameters) {
				builder.Add (new GenericParameterProxy (genericParameter));
			}

			return builder.ToImmutableArray ();
		}

		internal partial bool IsConstructor () => Method.IsConstructor;

		internal partial bool IsStatic () => Method.IsStatic;

		internal partial bool HasImplicitThis () => Method.HasImplicitThis ();

		internal partial bool ReturnsVoid () => Method.ReturnsVoid ();

		public override string ToString () => Method.ToString ();

		public bool Equals (MethodProxy other) => Method.Equals (other.Method);

		public override bool Equals (object? obj) => obj is MethodProxy other && Equals (other);

		public override int GetHashCode () => Method.GetHashCode ();
	}
}
