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
				if (customAttribute != null) {
					_context.LogMessage ($"Assigning external custom attribute '{FormatCustomAttribute (customAttribute)}' instance to '{provider}'");
					attributes.Add (customAttribute);
				}
			}

			return attributes;

			static string FormatCustomAttribute (CustomAttribute ca)
			{
				StringBuilder sb = new StringBuilder ();
				sb.Append (ca.Constructor.GetDisplayName ());
				sb.Append (" { args: ");
				for (int i = 0; i < ca.ConstructorArguments.Count; ++i) {
					if (i > 0)
						sb.Append (", ");

					var caa = ca.ConstructorArguments[i];
					sb.Append ($"{caa.Type.GetDisplayName ()} {caa.Value}");
				}
				sb.Append (" }");

				return sb.ToString ();
			}
		}

		CustomAttribute CreateCustomAttribute (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			CustomAttributeArgument[] arguments = ReadCustomAttributeArguments (iterator);

			MethodDefinition constructor = FindBestMatchingConstructor (attributeType, arguments);
			if (constructor == null) {
				_context.LogWarning (
					$"Could not find matching constructor for custom attribute '{attributeType.GetDisplayName ()}' arguments",
					2022,
					_xmlDocumentLocation);
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			foreach (var argument in arguments)
				customAttribute.ConstructorArguments.Add (argument);

			ReadCustomAttributeProperties (iterator.Current.SelectChildren ("property", string.Empty), attributeType, customAttribute);

			return customAttribute;
		}

		static MethodDefinition FindBestMatchingConstructor (TypeDefinition attributeType, CustomAttributeArgument[] args)
		{
			var methods = attributeType.Methods;
			for (int i = 0; i < attributeType.Methods.Count; ++i) {
				var m = methods[i];
				if (!m.IsInstanceConstructor ())
					continue;

				var p = m.Parameters;
				if (args.Length != p.Count)
					continue;

				bool match = true;
				for (int ii = 0; match && ii != args.Length; ++ii) {
					//
					// No candidates betterness, only exact matches are supported
					//
					if (p[ii].ParameterType.Resolve () != args[ii].Type.Resolve ())
						match = false;
				}

				if (match)
					return m;
			}

			return null;
		}

		void ReadCustomAttributeProperties (XPathNodeIterator iterator, TypeDefinition attributeType, CustomAttribute customAttribute)
		{
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

				var caa = ReadCustomAttributeArgument (iterator);
				if (caa is null)
					continue;

				customAttribute.Properties.Add (new CustomAttributeNamedArgument (property.Name, caa.Value));
			}
		}

		CustomAttributeArgument[] ReadCustomAttributeArguments (XPathNodeIterator iterator)
		{
			var args = new ArrayBuilder<CustomAttributeArgument> ();

			iterator = iterator.Current.SelectChildren ("argument", string.Empty);
			while (iterator.MoveNext ()) {
				CustomAttributeArgument? caa = ReadCustomAttributeArgument (iterator);
				if (caa is not null)
					args.Add (caa.Value);
			}

			return args.ToArray () ?? Array.Empty<CustomAttributeArgument> ();
		}

		CustomAttributeArgument? ReadCustomAttributeArgument (XPathNodeIterator iterator)
		{
			TypeReference typeref = ResolveArgumentType (iterator);
			if (typeref is null)
				return null;

			string svalue = iterator.Current.Value;

			//
			// Builds CustomAttributeArgument in the same way as it would be
			// represented in the metadata if encoded there. This simplifies
			// any custom attributes handling in linker by using same attributes
			// value extraction or mathing logic.
			//
			switch (typeref.MetadataType) {
			case MetadataType.Object:
				iterator = iterator.Current.SelectChildren ("argument", string.Empty);
				if (iterator?.MoveNext () != true) {
					_context.LogError ($"Custom attribute argument for 'System.Object' requires nested 'argument' node", 1043);
					return null;
				}

				var boxedValue = ReadCustomAttributeArgument (iterator);
				if (boxedValue is null)
					return null;

				return new CustomAttributeArgument (typeref, boxedValue);

			case MetadataType.Char:
			case MetadataType.Byte:
			case MetadataType.SByte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.UInt64:
			case MetadataType.Int64:
			case MetadataType.String:
				return new CustomAttributeArgument (typeref, ConvertStringValue (svalue, typeref));

			case MetadataType.ValueType:
				var enumType = typeref.Resolve ();
				if (enumType?.IsEnum != true)
					goto default;

				var enumField = enumType.Fields.Where (f => f.IsStatic && f.Name == svalue).FirstOrDefault ();
				object evalue = enumField?.Constant ?? svalue;

				typeref = enumType.GetEnumUnderlyingType ();
				return new CustomAttributeArgument (enumType, ConvertStringValue (evalue, typeref));

			case MetadataType.Class:
				if (!typeref.IsTypeOf ("System", "Type"))
					goto default;

				TypeReference type = _context.TypeNameResolver.ResolveTypeName (svalue);
				if (type == null) {
					_context.LogError ($"Could not resolve custom attribute type value '{svalue}'", 1044, _xmlDocumentLocation);
					return null;
				}

				return new CustomAttributeArgument (typeref, type);
			default:
				// TODO: Add support for null values
				// TODO: Add suppport for arrays
				_context.LogError ($"Unexpected attribute argument type '{typeref.GetDisplayName ()}'", 1045);
				return null;
			}

			TypeReference ResolveArgumentType (XPathNodeIterator iterator)
			{
				string typeName = GetAttribute (iterator.Current, "type");
				if (string.IsNullOrEmpty (typeName))
					typeName = "System.String";

				TypeReference typeref = _context.TypeNameResolver.ResolveTypeName (typeName);
				if (typeref == null) {
					_context.LogError ($"The type '{typeName}' used with attribute value '{iterator.Current.Value}' could not be found", 1041, _xmlDocumentLocation);
					return null;
				}

				return typeref;
			}
		}

		object ConvertStringValue (object value, TypeReference targetType)
		{
			TypeCode typeCode;
			switch (targetType.MetadataType) {
			case MetadataType.String:
				typeCode = TypeCode.String;
				break;
			case MetadataType.Char:
				typeCode = TypeCode.Char;
				break;
			case MetadataType.Byte:
				typeCode = TypeCode.Byte;
				break;
			case MetadataType.SByte:
				typeCode = TypeCode.SByte;
				break;
			case MetadataType.Int16:
				typeCode = TypeCode.Int16;
				break;
			case MetadataType.UInt16:
				typeCode = TypeCode.UInt16;
				break;
			case MetadataType.Int32:
				typeCode = TypeCode.Int32;
				break;
			case MetadataType.UInt32:
				typeCode = TypeCode.UInt32;
				break;
			case MetadataType.UInt64:
				typeCode = TypeCode.UInt64;
				break;
			case MetadataType.Int64:
				typeCode = TypeCode.Int64;
				break;
			case MetadataType.Boolean:
				typeCode = TypeCode.Boolean;
				break;
			case MetadataType.Single:
				typeCode = TypeCode.Single;
				break;
			case MetadataType.Double:
				typeCode = TypeCode.Double;
				break;
			default:
				throw new NotSupportedException (targetType.ToString ());
			}

			try {
				return Convert.ChangeType (value, typeCode);
			} catch {
				_context.LogError ($"Cannot convert value '{value}' to type '{targetType.GetDisplayName ()}'", 1042);
				return null;
			}
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