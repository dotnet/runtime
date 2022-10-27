// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
	[SuppressMessage ("ApiDesign", "RS0030:Do not used banned APIs", Justification = "This class provides wrapper methods around the banned Parameters property")]
	internal static class MethodDefinitionExtensions
	{
		public static bool IsDefaultConstructor (this MethodDefinition method)
		{
			return IsInstanceConstructor (method) && !method.HasMetadataParameters ();
		}

		public static bool IsInstanceConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && !method.IsStatic;
		}

		public static bool IsIntrinsic (this MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;

			foreach (var ca in method.CustomAttributes) {
				var caType = ca.AttributeType;
				if (caType.Name == "IntrinsicAttribute" && caType.Namespace == "System.Runtime.CompilerServices")
					return true;
			}

			return false;
		}

		public static bool IsPropertyMethod (this MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
		}

		public static bool IsPublicInstancePropertyMethod (this MethodDefinition md)
		{
			return md.IsPublic && !md.IsStatic && IsPropertyMethod (md);
		}

		public static bool IsEventMethod (this MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
		}

		public static bool TryGetProperty (this MethodDefinition md, [NotNullWhen (true)] out PropertyDefinition? property)
		{
			property = null;
			if (!md.IsPropertyMethod ())
				return false;

			TypeDefinition declaringType = md.DeclaringType;
			foreach (PropertyDefinition prop in declaringType.Properties)
				if (prop.GetMethod == md || prop.SetMethod == md) {
					property = prop;
					return true;
				}

			return false;
		}

		public static bool TryGetEvent (this MethodDefinition md, [NotNullWhen (true)] out EventDefinition? @event)
		{
			@event = null;
			if (!md.IsEventMethod ())
				return false;

			TypeDefinition declaringType = md.DeclaringType;
			foreach (EventDefinition evt in declaringType.Events)
				if (evt.AddMethod == md || evt.InvokeMethod == md || evt.RemoveMethod == md) {
					@event = evt;
					return true;
				}

			return false;
		}

		public static bool IsStaticConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && method.IsStatic;
		}

		public static void ClearDebugInformation (this MethodDefinition method)
		{
			// TODO: This always allocates, update when Cecil catches up
			var di = method.DebugInformation;
			di.SequencePoints.Clear ();
			if (di.Scope != null) {
				di.Scope.Variables.Clear ();
				di.Scope.Constants.Clear ();
				di.Scope = null;
			}
		}

		public static bool HasParameterOfType (this MethodDefinition method, ParameterIndex index, string typeName)
			=> method.TryGetParameter (index)?.ParameterType?.IsTypeOf (typeName) is true;

		/// <summary>
		/// Tries to get the <see cref="ParameterProxy"/> representing the parameter at index <paramref name="index"/> of method <paramref name="method"/>.
		/// Returns null if <paramref name="index"/> is not a valid parameter index for <paramref name="method"/>.
		/// <see cref="GetParameter(MethodDefinition, ParameterIndex)"/> for a non-nullable version if you know the index is valid.
		/// </summary>
		public static ParameterProxy? TryGetParameter (this MethodDefinition method, ParameterIndex index)
		{
			if (method.GetParametersCount () <= (int) index || (int) index < 0)
				return null;
			return new (new (method), index);
		}

		/// <summary>
		/// Gets the <see cref="ParameterProxy"/> representing the parameter at index <paramref name="index"/> of method <paramref name="method"/>.
		/// Throws if <paramref name="index"/> is not a valid parameter index for <paramref name="method"/>.
		/// <see cref="TryGetParameter(MethodDefinition, ParameterIndex)"/> for a non-throwing version if you're not sure the parameter exists on the method.
		/// </summary>
		public static ParameterProxy GetParameter (this MethodDefinition method, ParameterIndex index)
		{
			if (method.TryGetParameter (index) is not ParameterProxy param)
				throw new InvalidOperationException ($"Cannot get parameter #{(int) index} of method {method.GetDisplayName ()} with {method.GetParametersCount ()} parameters");
			return param;
		}

		/// <summary>
		/// Returns a foreach-enumerable collection of the parameters pushed onto the stack before the method call (including the implicit 'this' parameter)
		/// </summary>
		public static ParameterProxyEnumerable GetParameters (this MethodDefinition method)
		{
			int implicitThisOffset = method.HasImplicitThis () ? 1 : 0;
			return new ParameterProxyEnumerable (0, method.Parameters.Count + implicitThisOffset, method);
		}

		/// <summary>
		/// Returns a list of ParameterProxy representing the parameters listed in the "Parameters" metadata section (i.e. not including the implicit 'this' parameter)
		/// </summary>
		public static ParameterProxyEnumerable GetMetadataParameters (this MethodDefinition method)
		{
			int implicitThisOffset = method.HasImplicitThis () ? 1 : 0;
			return new ParameterProxyEnumerable (implicitThisOffset, method.Parameters.Count + implicitThisOffset, method);
		}
	}
}
