// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LinkAttributesParser : ProcessLinkerXmlBase
	{
		AttributeInfo _attributeInfo;

		public LinkAttributesParser (LinkContext context, XPathDocument document, string xmlDocumentLocation)
			: base (context, document, xmlDocumentLocation)
		{
		}

		public LinkAttributesParser (LinkContext context, XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (context, document, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Parse (AttributeInfo xmlInfo)
		{
			_attributeInfo = xmlInfo;
			bool stripLinkAttributes = _context.IsOptimizationEnabled (CodeOptimizations.RemoveLinkAttributes, _resourceAssembly);
			ProcessXml (stripLinkAttributes, _context.IgnoreLinkAttributes);
		}

		IEnumerable<CustomAttribute> ProcessAttributes (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			XPathNodeIterator iterator = nav.SelectChildren ("attribute", string.Empty);
			var attributes = new List<CustomAttribute> ();
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					continue;

				string internalAttribute = GetAttribute (iterator.Current, "internal");
				if (!string.IsNullOrEmpty (internalAttribute)) {
					ProcessInternalAttribute (provider, internalAttribute);
					continue;
				}

				string attributeFullName = GetFullName (iterator.Current);
				if (string.IsNullOrEmpty (attributeFullName)) {
					_context.LogWarning ($"'attribute' element does not contain attribute 'fullname' or it's empty", 2029, _xmlDocumentLocation);
					continue;
				}

				if (!GetAttributeType (iterator, attributeFullName, out TypeDefinition attributeType))
					continue;

				CustomAttribute customAttribute = CreateCustomAttribute (iterator, attributeType);
				if (customAttribute != null)
					attributes.Add (customAttribute);
			}

			return attributes;
		}

		CustomAttribute CreateCustomAttribute (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			string[] attributeArguments = GetAttributeChildren (iterator.Current.SelectChildren ("argument", string.Empty)).ToArray ();
			var attributeArgumentCount = attributeArguments == null ? 0 : attributeArguments.Length;
			MethodDefinition constructor = attributeType.Methods.Where (method => method.IsInstanceConstructor ()).FirstOrDefault (c => c.Parameters.Count == attributeArgumentCount);
			if (constructor == null) {
				_context.LogWarning (
					$"Could not find a constructor for type '{attributeType}' that has '{attributeArgumentCount}' arguments",
					2022,
					_xmlDocumentLocation);
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			var arguments = ProcessAttributeArguments (constructor, attributeArguments);
			if (arguments != null)
				foreach (var argument in arguments)
					customAttribute.ConstructorArguments.Add (argument);

			var properties = ProcessAttributeProperties (iterator.Current.SelectChildren ("property", string.Empty), attributeType);
			if (properties != null)
				foreach (var property in properties)
					customAttribute.Properties.Add (property);

			return customAttribute;
		}

		List<CustomAttributeNamedArgument> ProcessAttributeProperties (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			List<CustomAttributeNamedArgument> attributeProperties = new List<CustomAttributeNamedArgument> ();
			while (iterator.MoveNext ()) {
				string propertyName = GetName (iterator.Current);
				if (string.IsNullOrEmpty (propertyName)) {
					_context.LogWarning ($"Property element does not contain attribute 'name'", 2051, _xmlDocumentLocation);
					continue;
				}

				PropertyDefinition property = attributeType.Properties.Where (prop => prop.Name == propertyName).FirstOrDefault ();
				if (property == null) {
					_context.LogWarning ($"Property '{propertyName}' could not be found", 2052, _xmlDocumentLocation);
					continue;
				}

				var propertyValue = iterator.Current.Value;
				if (!TryConvertValue (propertyValue, property.PropertyType, out object value)) {
					_context.LogWarning ($"Invalid value '{propertyValue}' for property '{propertyName}'", 2053, _xmlDocumentLocation);
					continue;
				}

				attributeProperties.Add (new CustomAttributeNamedArgument (property.Name,
					new CustomAttributeArgument (property.PropertyType, value)));
			}

			return attributeProperties;
		}

		List<CustomAttributeArgument> ProcessAttributeArguments (MethodDefinition attributeConstructor, string[] arguments)
		{
			if (arguments == null)
				return null;

			List<CustomAttributeArgument> attributeArguments = new List<CustomAttributeArgument> ();
			for (int i = 0; i < arguments.Length; i++) {
				object argValue;
				TypeDefinition parameterType = attributeConstructor.Parameters[i].ParameterType.Resolve ();
				if (!TryConvertValue (arguments[i], parameterType, out argValue)) {
					_context.LogWarning (
						$"Invalid argument value '{arguments[i]}' for parameter type '{parameterType.GetDisplayName ()}' of attribute '{attributeConstructor.DeclaringType.GetDisplayName ()}'",
						2054,
						_xmlDocumentLocation);
					return null;
				}

				attributeArguments.Add (new CustomAttributeArgument (parameterType, argValue));
			}

			return attributeArguments;
		}

		void ProcessInternalAttribute (ICustomAttributeProvider provider, string internalAttribute)
		{
			if (internalAttribute != "RemoveAttributeInstances") {
				_context.LogWarning ($"Unrecognized internal attribute '{internalAttribute}'", 2049, _xmlDocumentLocation);
				return;
			}

			if (provider.MetadataToken.TokenType != TokenType.TypeDef) {
				_context.LogWarning ($"Internal attribute 'RemoveAttributeInstances' can only be used on a type, but is being used on '{provider}'", 2048, _xmlDocumentLocation);
				return;
			}

			if (!_context.Annotations.IsMarked (provider)) {
				IEnumerable<Attribute> removeAttributeInstance = new List<Attribute> { new RemoveAttributeInstancesAttribute () };
				_attributeInfo.AddInternalAttributes (provider, removeAttributeInstance);
			}
		}

		bool GetAttributeType (XPathNodeIterator iterator, string attributeFullName, out TypeDefinition attributeType)
		{
			string assemblyName = GetAttribute (iterator.Current, "assembly");
			if (string.IsNullOrEmpty (assemblyName)) {
				attributeType = _context.GetType (attributeFullName);
			} else {
				AssemblyDefinition assembly;
				try {
					assembly = _context.TryResolve (AssemblyNameReference.Parse (assemblyName));
					if (assembly == null) {
						_context.LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, _xmlDocumentLocation);
						attributeType = default;
						return false;
					}
				} catch (Exception) {
					_context.LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, _xmlDocumentLocation);
					attributeType = default;
					return false;
				}

				attributeType = _context.TypeNameResolver.ResolveTypeName (assembly, attributeFullName)?.Resolve ();
			}

			if (attributeType == null) {
				_context.LogWarning ($"Attribute type '{attributeFullName}' could not be found", 2031, _xmlDocumentLocation);
				return false;
			}

			return true;
		}

		static ArrayBuilder<string> GetAttributeChildren (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> children = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				children.Add (iterator.Current.Value);
			}
			return children;
		}

		protected override AllowedAssemblies AllowedAssemblySelector {
			get {
				if (_resourceAssembly == null)
					return AllowedAssemblies.AllAssemblies;

				// Corelib XML may contain assembly wildcard to support compiler-injected attribute types
				if (_resourceAssembly.Name.Name == PlatformAssemblies.CoreLib)
					return AllowedAssemblies.AllAssemblies;

				return AllowedAssemblies.ContainingAssembly;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, assembly);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (assembly, attributes);
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, type);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (type, attributes);
			ProcessTypeChildren (type, nav);

			if (!type.HasNestedTypes)
				return;

			var iterator = nav.SelectChildren ("type", string.Empty);
			while (iterator.MoveNext ()) {
				foreach (TypeDefinition nested in type.NestedTypes) {
					if (nested.Name == GetAttribute (iterator.Current, "name") && ShouldProcessElement (iterator.Current))
						ProcessType (nested, iterator.Current);
				}
			}
		}

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, field);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (field, attributes);
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, method);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (method, attributes);
			ProcessReturnParameters (method, nav);
			ProcessParameters (method, nav);
		}

		void ProcessParameters (MethodDefinition method, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren ("parameter", string.Empty);
			while (iterator.MoveNext ()) {
				IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current, method);
				if (attributes.Any ()) {
					string paramName = GetAttribute (iterator.Current, "name");
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (parameter.HasCustomAttributes || _attributeInfo.CustomAttributes.ContainsKey (parameter))
								_context.LogWarning (
									$"More than one value specified for parameter '{paramName}' of method '{method.GetDisplayName ()}'",
									2024, _xmlDocumentLocation);
							_attributeInfo.AddCustomAttributes (parameter, attributes);
							break;
						}
					}
				}
			}
		}

		void ProcessReturnParameters (MethodDefinition method, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren ("return", string.Empty);
			bool firstAppearance = true;
			while (iterator.MoveNext ()) {
				if (firstAppearance) {
					firstAppearance = false;
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current, method.MethodReturnType);
					if (attributes.Any ())
						_attributeInfo.AddCustomAttributes (method.MethodReturnType, attributes);
				} else {
					_context.LogWarning (
						$"There is more than one 'return' child element specified for method '{method.GetDisplayName ()}'",
						2023, _xmlDocumentLocation);
				}
			}
		}

		protected override MethodDefinition GetMethod (TypeDefinition type, string signature)
		{
			if (type.HasMethods)
				foreach (MethodDefinition method in type.Methods)
					if (signature.Replace (" ", "") == GetMethodSignature (method) || signature.Replace (" ", "") == GetMethodSignature (method, true))
						return method;

			return null;
		}

		static string GetMethodSignature (MethodDefinition method, bool includeReturnType = false)
		{
			StringBuilder sb = new StringBuilder ();
			if (includeReturnType) {
				sb.Append (method.ReturnType.FullName);
			}
			sb.Append (method.Name);
			if (method.HasGenericParameters) {
				sb.Append ("<");
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (method.GenericParameters[i].Name);
				}
				sb.Append (">");
			}
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (method.Parameters[i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object customData, bool fromSignature)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, property);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (property, attributes);
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, @event);
			if (attributes.Any ())
				_attributeInfo.AddCustomAttributes (@event, attributes);
		}
	}
}