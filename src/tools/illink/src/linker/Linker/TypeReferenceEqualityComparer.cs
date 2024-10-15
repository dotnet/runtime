// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker
{
	// Copied from https://github.com/jbevain/cecil/blob/master/Mono.Cecil/TypeReferenceComparer.cs
	internal sealed class TypeReferenceEqualityComparer : EqualityComparer<TypeReference>
	{
		public readonly ITryResolveMetadata _resolver;

		public TypeReferenceEqualityComparer(ITryResolveMetadata resolver)
		{
			_resolver = resolver;
		}

		public override bool Equals (TypeReference? x, TypeReference? y)
		{
			return AreEqual (x, y, _resolver);
		}

		public override int GetHashCode (TypeReference obj)
		{
			return GetHashCodeFor (obj);
		}

		public static bool AreEqual (TypeReference? a, TypeReference? b, ITryResolveMetadata resolver, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (ReferenceEquals (a, b))
				return true;

			if (a == null || b == null)
				return false;

			var aMetadataType = a.MetadataType;
			var bMetadataType = b.MetadataType;

			if (aMetadataType == MetadataType.GenericInstance || bMetadataType == MetadataType.GenericInstance) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual ((GenericInstanceType) a, (GenericInstanceType) b, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.Array || bMetadataType == MetadataType.Array) {
				if (aMetadataType != bMetadataType)
					return false;

				var a1 = (ArrayType) a;
				var b1 = (ArrayType) b;
				if (a1.Rank != b1.Rank)
					return false;

				return AreEqual (a1.ElementType, b1.ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.Var || bMetadataType == MetadataType.Var) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual ((GenericParameter) a, (GenericParameter) b, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.MVar || bMetadataType == MetadataType.MVar) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual ((GenericParameter) a, (GenericParameter) b, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.ByReference || bMetadataType == MetadataType.ByReference) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual (((ByReferenceType) a).ElementType, ((ByReferenceType) b).ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.Pointer || bMetadataType == MetadataType.Pointer) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual (((PointerType) a).ElementType, ((PointerType) b).ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.RequiredModifier || bMetadataType == MetadataType.RequiredModifier) {
				if (aMetadataType != bMetadataType)
					return false;

				var a1 = (RequiredModifierType) a;
				var b1 = (RequiredModifierType) b;

				return AreEqual (a1.ModifierType, b1.ModifierType, resolver, comparisonMode) && AreEqual (a1.ElementType, b1.ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.OptionalModifier || bMetadataType == MetadataType.OptionalModifier) {
				if (aMetadataType != bMetadataType)
					return false;

				var a1 = (OptionalModifierType) a;
				var b1 = (OptionalModifierType) b;

				return AreEqual (a1.ModifierType, b1.ModifierType, resolver, comparisonMode) && AreEqual (a1.ElementType, b1.ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.Pinned || bMetadataType == MetadataType.Pinned) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual (((PinnedType) a).ElementType, ((PinnedType) b).ElementType, resolver, comparisonMode);
			}

			if (aMetadataType == MetadataType.Sentinel || bMetadataType == MetadataType.Sentinel) {
				if (aMetadataType != bMetadataType)
					return false;

				return AreEqual (((SentinelType) a).ElementType, ((SentinelType) b).ElementType, resolver, comparisonMode);
			}

			if (!a.Name.Equals (b.Name) || !a.Namespace.Equals (b.Namespace))
				return false;

			var xDefinition = resolver.TryResolve (a);
			var yDefinition = resolver.TryResolve (b);
			if (xDefinition == null || yDefinition == null)
				return false;

			// For loose signature the types could be in different assemblies, as long as the type names match we will consider them equal
			if (comparisonMode == TypeComparisonMode.SignatureOnlyLoose) {
				if (xDefinition.Module.Name != yDefinition.Module.Name)
					return false;

				if (xDefinition.Module.Assembly.Name.Name != yDefinition.Module.Assembly.Name.Name)
					return false;

				return xDefinition.FullName == yDefinition.FullName;
			}

			return xDefinition == yDefinition;
		}

		static bool AreEqual (GenericParameter a, GenericParameter b, ITryResolveMetadata resolver, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (ReferenceEquals (a, b))
				return true;

			if (a.Position != b.Position)
				return false;

			if (a.Type != b.Type)
				return false;

			var aOwnerType = a.Owner as TypeReference;
			if (aOwnerType != null && AreEqual (aOwnerType, b.Owner as TypeReference, resolver, comparisonMode))
				return true;

			var aOwnerMethod = a.Owner as MethodReference;
			if (aOwnerMethod != null && comparisonMode != TypeComparisonMode.SignatureOnlyLoose && MethodReferenceComparer.AreEqual (aOwnerMethod, b.Owner as MethodReference, resolver))
				return true;

			return comparisonMode == TypeComparisonMode.SignatureOnly || comparisonMode == TypeComparisonMode.SignatureOnlyLoose;
		}

		static bool AreEqual (GenericInstanceType a, GenericInstanceType b, ITryResolveMetadata resolver, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (ReferenceEquals (a, b))
				return true;

			var aGenericArgumentsCount = a.GenericArguments.Count;
			if (aGenericArgumentsCount != b.GenericArguments.Count)
				return false;

			if (!AreEqual (a.ElementType, b.ElementType, resolver, comparisonMode))
				return false;

			for (int i = 0; i < aGenericArgumentsCount; i++)
				if (!AreEqual (a.GenericArguments[i], b.GenericArguments[i], resolver, comparisonMode))
					return false;

			return true;
		}

		public static int GetHashCodeFor (TypeReference obj)
		{
			// a very good prime number
			const int hashCodeMultiplier = 486187739;
			// prime numbers
			const int genericInstanceTypeMultiplier = 31;
			const int byReferenceMultiplier = 37;
			const int pointerMultiplier = 41;
			const int requiredModifierMultiplier = 43;
			const int optionalModifierMultiplier = 47;
			const int pinnedMultiplier = 53;
			const int sentinelMultiplier = 59;

			var metadataType = obj.MetadataType;

			if (metadataType == MetadataType.GenericInstance) {
				var genericInstanceType = (GenericInstanceType) obj;
				var hashCode = GetHashCodeFor (genericInstanceType.ElementType) * hashCodeMultiplier + genericInstanceTypeMultiplier;
				for (var i = 0; i < genericInstanceType.GenericArguments.Count; i++)
					hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor (genericInstanceType.GenericArguments[i]);
				return hashCode;
			}

			if (metadataType == MetadataType.Array) {
				var arrayType = (ArrayType) obj;
				return GetHashCodeFor (arrayType.ElementType) * hashCodeMultiplier + arrayType.Rank.GetHashCode ();
			}

			if (metadataType == MetadataType.Var || metadataType == MetadataType.MVar) {
				var genericParameter = (GenericParameter) obj;
				var hashCode = genericParameter.Position.GetHashCode () * hashCodeMultiplier + ((int) metadataType).GetHashCode ();

				var ownerTypeReference = genericParameter.Owner as TypeReference;
				if (ownerTypeReference != null)
					return hashCode * hashCodeMultiplier + GetHashCodeFor (ownerTypeReference);

				var ownerMethodReference = genericParameter.Owner as MethodReference;
				if (ownerMethodReference != null)
					return hashCode * hashCodeMultiplier + MethodReferenceComparer.GetHashCodeFor (ownerMethodReference);

				throw new InvalidOperationException ("Generic parameter encountered with invalid owner");
			}

			if (metadataType == MetadataType.ByReference) {
				var byReferenceType = (ByReferenceType) obj;
				return GetHashCodeFor (byReferenceType.ElementType) * hashCodeMultiplier * byReferenceMultiplier;
			}

			if (metadataType == MetadataType.Pointer) {
				var pointerType = (PointerType) obj;
				return GetHashCodeFor (pointerType.ElementType) * hashCodeMultiplier * pointerMultiplier;
			}

			if (metadataType == MetadataType.RequiredModifier) {
				var requiredModifierType = (RequiredModifierType) obj;
				var hashCode = GetHashCodeFor (requiredModifierType.ElementType) * requiredModifierMultiplier;
				hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor (requiredModifierType.ModifierType);
				return hashCode;
			}

			if (metadataType == MetadataType.OptionalModifier) {
				var optionalModifierType = (OptionalModifierType) obj;
				var hashCode = GetHashCodeFor (optionalModifierType.ElementType) * optionalModifierMultiplier;
				hashCode = hashCode * hashCodeMultiplier + GetHashCodeFor (optionalModifierType.ModifierType);
				return hashCode;
			}

			if (metadataType == MetadataType.Pinned) {
				var pinnedType = (PinnedType) obj;
				return GetHashCodeFor (pinnedType.ElementType) * hashCodeMultiplier * pinnedMultiplier;
			}

			if (metadataType == MetadataType.Sentinel) {
				var sentinelType = (SentinelType) obj;
				return GetHashCodeFor (sentinelType.ElementType) * hashCodeMultiplier * sentinelMultiplier;
			}

			if (metadataType == MetadataType.FunctionPointer) {
				throw new NotImplementedException ("We currently don't handle function pointer types.");
			}

			return obj.Namespace.GetHashCode () * hashCodeMultiplier + obj.FullName.GetHashCode ();
		}
	}
}
