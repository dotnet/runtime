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
	class LinkAttributesStep : ProcessLinkerXmlStepBase
	{
		public LinkAttributesStep (XPathDocument document, string xmlDocumentLocation)
			: base (document, xmlDocumentLocation)
		{
		}

		public LinkAttributesStep (XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (document, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		IEnumerable<CustomAttribute> ProcessAttributes (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			XPathNodeIterator iterator = nav.SelectChildren ("attribute", string.Empty);
			var attributes = new List<CustomAttribute> ();
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					continue;

				AssemblyDefinition assembly;
				TypeDefinition attributeType;

				string internalAttribute = GetAttribute (iterator.Current, "internal");
				if (internalAttribute != String.Empty) {
					if (internalAttribute == "RemoveAttributeInstances") {
						if (provider.MetadataToken.TokenType == TokenType.TypeDef) {
							if (!Annotations.IsMarked (provider)) {
								IEnumerable<Attribute> removeAttributeInstance = new List<Attribute> { new RemoveAttributeInstancesAttribute () };
								Context.CustomAttributes.AddInternalAttributes (provider, removeAttributeInstance);
							}
							continue;
						} else {
							Context.LogWarning ($"Internal attribute 'RemoveAttributeInstances' can only be used on a type, but is being used on '{nav.Name}' '{provider}'", 2048, _xmlDocumentLocation);
							continue;
						}
					} else {
						Context.LogWarning ($"Unrecognized internal attribute '{internalAttribute}'", 2049, _xmlDocumentLocation);
						continue;
					}
				}

				string attributeFullName = GetFullName (iterator.Current);
				if (attributeFullName == String.Empty) {
					Context.LogWarning ($"Attribute element does not contain attribute 'fullname'", 2029, _xmlDocumentLocation);
					continue;
				}
				string assemblyName = GetAttribute (iterator.Current, "assembly");
				if (assemblyName == String.Empty)
					attributeType = Context.GetType (attributeFullName);
				else {
					try {
						assembly = GetAssembly (Context, AssemblyNameReference.Parse (assemblyName));
					} catch (Exception) {
						Context.LogWarning ($"Could not resolve assembly '{assemblyName}' in attribute '{attributeFullName}' specified in the '{_xmlDocumentLocation}'", 2030, _xmlDocumentLocation);
						continue;
					}
					attributeType = assembly.FindType (attributeFullName);
				}
				if (attributeType == null) {
					Context.LogWarning ($"Attribute type '{attributeFullName}' could not be found", 2031, _xmlDocumentLocation);
					continue;
				}

				ArrayBuilder<string> arguments = GetAttributeChildren (iterator.Current.SelectChildren ("argument", string.Empty));
				MethodDefinition constructor = attributeType.Methods.Where (method => method.IsInstanceConstructor ()).FirstOrDefault (c => c.Parameters.Count == arguments.Count);
				if (constructor == null) {
					Context.LogWarning ($"Could not find a constructor for type '{attributeType}' that receives '{arguments.Count}' arguments as parameter", 2022, _xmlDocumentLocation);
					continue;
				}
				string[] xmlArguments = arguments.ToArray ();
				bool recognizedArgument = true;

				CustomAttribute attribute = new CustomAttribute (constructor);
				if (xmlArguments == null) {
					attributes.Add (attribute);
					continue;
				}
				for (int i = 0; i < xmlArguments.Length; i++) {
					object argumentValue = null;

					if (constructor.Parameters[i].ParameterType.Resolve ().IsEnum) {
						foreach (var field in constructor.Parameters[i].ParameterType.Resolve ().Fields) {
							if (field.IsStatic && field.Name == xmlArguments[i]) {
								argumentValue = Convert.ToInt32 (field.Constant);
								break;
							}
						}
						if (argumentValue == null) {
							Context.LogWarning ($"Could not parse argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' as a {constructor.Parameters[i].ParameterType.FullName}", 2021, _xmlDocumentLocation);
							recognizedArgument = false;
						}
					} else {
						switch (constructor.Parameters[i].ParameterType.MetadataType) {
						case MetadataType.String:
							argumentValue = xmlArguments[i];
							break;
						case MetadataType.Int32:
							int result;
							if (int.TryParse (xmlArguments[i], out result))
								argumentValue = result;
							else {
								Context.LogWarning ($"Argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' could not be transformed to the constructor parameter type", 2032, _xmlDocumentLocation);
							}
							break;
						default:
							Context.LogWarning ($"Argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' is of unsupported type '{constructor.Parameters[i].ParameterType}'", 2020, _xmlDocumentLocation);
							recognizedArgument = false;
							break;
						}
					}
					attribute.ConstructorArguments.Add (new CustomAttributeArgument (constructor.Parameters[i].ParameterType, argumentValue));
				}
				if (recognizedArgument)
					attributes.Add (attribute);
			}
			return attributes;
		}

		ArrayBuilder<string> GetAttributeChildren (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> children = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				children.Add (iterator.Current.Value);
			}
			return children;
		}

		protected override void Process ()
		{
			ProcessXml (Context.StripLinkAttributes, Context.IgnoreLinkAttributes);
		}

		protected override bool AllowAllAssembliesSelector { get => true; }

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator, bool warnOnUnresolvedTypes)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current, assembly);
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (assembly, attributes);
			ProcessTypes (assembly, iterator, warnOnUnresolvedTypes);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, type);
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (type, attributes);
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
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (field, attributes);
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, method);
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (method, attributes);
			ProcessReturnParameters (method, nav);
			ProcessParameters (method, nav);
		}

		void ProcessParameters (MethodDefinition method, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren ("parameter", string.Empty);
			while (iterator.MoveNext ()) {
				IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current, method);
				if (attributes.Count () > 0) {
					string paramName = GetAttribute (iterator.Current, "name");
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (Context.CustomAttributes.HasCustomAttributes (parameter))
								Context.LogWarning ($"There are duplicate parameter names for '{paramName}' inside '{method.GetDisplayName ()}' in '{_xmlDocumentLocation}'", 2024, _xmlDocumentLocation);
							Context.CustomAttributes.AddCustomAttributes (parameter, attributes);
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
					if (attributes.Count () > 0)
						Context.CustomAttributes.AddCustomAttributes (method.MethodReturnType, attributes);
				} else {
					Context.LogWarning ($"There is more than one return parameter specified for '{method.GetDisplayName ()}' in '{_xmlDocumentLocation}'", 2023, _xmlDocumentLocation);
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
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (property, attributes);
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav, @event);
			if (attributes.Count () > 0)
				Context.CustomAttributes.AddCustomAttributes (@event, attributes);
		}

		protected override AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName)
		{
			var assembly = context.Resolve (assemblyName);
			ProcessReferences (assembly);
			return assembly;
		}
	}
}