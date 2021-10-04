// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	readonly struct LinkerAttributesInformation
	{
		readonly Dictionary<Type, List<Attribute>> _linkerAttributes;

		private LinkerAttributesInformation (Dictionary<Type, List<Attribute>> cache)
		{
			this._linkerAttributes = cache;
		}

		public static LinkerAttributesInformation Create (LinkContext context, ICustomAttributeProvider provider)
		{
			Debug.Assert (context.CustomAttributes.HasAny (provider));

			Dictionary<Type, List<Attribute>> cache = null;

			foreach (var customAttribute in context.CustomAttributes.GetCustomAttributes (provider)) {
				var attributeType = customAttribute.AttributeType;

				Attribute attributeValue;
				switch (attributeType.Name) {
				case "RequiresUnreferencedCodeAttribute" when attributeType.Namespace == "System.Diagnostics.CodeAnalysis":
					attributeValue = ProcessRequiresUnreferencedCodeAttribute (context, provider, customAttribute);
					break;
				case "DynamicDependencyAttribute" when attributeType.Namespace == "System.Diagnostics.CodeAnalysis":
					attributeValue = DynamicDependency.ProcessAttribute (context, provider, customAttribute);
					break;
				case "RemoveAttributeInstancesAttribute":
					if (provider is not TypeDefinition td)
						continue;

					// The attribute is never removed if it's explicitly preserved (e.g. via xml descriptor)
					if (context.Annotations.TryGetPreserve (td, out TypePreserve preserve) && preserve != TypePreserve.Nothing)
						continue;

					attributeValue = BuildRemoveAttributeInstancesAttribute (context, td, customAttribute);
					break;
				default:
					continue;
				}

				if (attributeValue == null)
					continue;

				if (cache == null)
					cache = new Dictionary<Type, List<Attribute>> ();

				Type attributeValueType = attributeValue.GetType ();
				if (!cache.TryGetValue (attributeValueType, out var attributeList)) {
					attributeList = new List<Attribute> ();
					cache.Add (attributeValueType, attributeList);
				}

				attributeList.Add (attributeValue);
			}

			return new LinkerAttributesInformation (cache);
		}

		public bool HasAttribute<T> () where T : Attribute
		{
			return _linkerAttributes != null && _linkerAttributes.ContainsKey (typeof (T));
		}

		public IEnumerable<T> GetAttributes<T> () where T : Attribute
		{
			if (_linkerAttributes == null || !_linkerAttributes.TryGetValue (typeof (T), out var attributeList))
				return Enumerable.Empty<T> ();

			if (attributeList == null || attributeList.Count == 0)
				throw new InvalidOperationException ("Unexpected list of attributes.");

			return attributeList.Cast<T> ();
		}

		static Attribute ProcessRequiresUnreferencedCodeAttribute (LinkContext context, ICustomAttributeProvider provider, CustomAttribute customAttribute)
		{
			if (!(provider is MethodDefinition || provider is TypeDefinition))
				return null;

			if (customAttribute.HasConstructorArguments && customAttribute.ConstructorArguments[0].Value is string message) {
				var ruca = new RequiresUnreferencedCodeAttribute (message);
				if (customAttribute.HasProperties) {
					foreach (var prop in customAttribute.Properties) {
						if (prop.Name == "Url") {
							ruca.Url = prop.Argument.Value as string;
							break;
						}
					}
				}

				return ruca;
			}

			context.LogWarning (
				$"Attribute '{typeof (RequiresUnreferencedCodeAttribute).FullName}' doesn't have the required number of parameters specified.",
				2028, (IMemberDefinition) provider);
			return null;
		}

		static RemoveAttributeInstancesAttribute BuildRemoveAttributeInstancesAttribute (LinkContext context, TypeDefinition attributeContext, CustomAttribute ca)
		{
			switch (ca.ConstructorArguments.Count) {
			case 0:
				return new RemoveAttributeInstancesAttribute ();
			case 1:
				// Argument is always boxed
				return new RemoveAttributeInstancesAttribute ((CustomAttributeArgument) ca.ConstructorArguments[0].Value);
			default:
				context.LogWarning (
					$"Attribute '{ca.AttributeType.GetDisplayName ()}' doesn't have the required number of arguments specified.",
					2028, attributeContext);
				return null;
			};
		}
	}
}