// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker
{
	class XmlCustomAttributeSource
	{
		readonly Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> _attributes = new Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> ();

		readonly LinkContext _context;
		XPathDocument _document;
		string _xmlDocumentLocation;

		public XmlCustomAttributeSource (LinkContext context)
		{
			_context = context;
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			return _attributes.TryGetValue (provider, out var ann) ? ann : null;
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			return _attributes.ContainsKey (provider);
		}

		IEnumerable<CustomAttribute> ProcessAttributes (XPathNavigator nav)
		{
			XPathNodeIterator iterator = nav.SelectChildren ("attribute", string.Empty);
			var attributes = new List<CustomAttribute> ();
			while (iterator.MoveNext ()) {
				AssemblyDefinition assembly;
				TypeDefinition attributeType;

				string attributeFullName = GetFullName (iterator.Current);
				if (attributeFullName == String.Empty) {
					_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Attribute element does not contain attribute 'fullname'", 2029, _xmlDocumentLocation));
					continue;
				}
				string assemblyName = GetAttribute (iterator.Current, "assembly");
				if (assemblyName == String.Empty)
					attributeType = _context.GetType (attributeFullName);
				else {
					try {
						assembly = GetAssembly (_context, AssemblyNameReference.Parse (assemblyName));
					} catch (Exception) {
						_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not resolve assembly '{assemblyName}' in attribute '{attributeFullName}' specified in the '{_xmlDocumentLocation}'", 2030, _xmlDocumentLocation));
						continue;
					}
					attributeType = assembly.FindType (attributeFullName);
				}
				if (attributeType == null) {
					_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Attribute type '{attributeFullName}' could not be found", 2031, _xmlDocumentLocation));
					continue;
				}

				ArrayBuilder<string> arguments = GetAttributeChildren (iterator.Current.SelectChildren ("argument", string.Empty));
				MethodDefinition constructor = attributeType.Methods.Where (method => method.IsInstanceConstructor ()).FirstOrDefault (c => c.Parameters.Count == arguments.Count);
				if (constructor == null) {
					_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not find a constructor for type '{attributeType}' that receives '{arguments.Count}' arguments as parameter", 2022, _xmlDocumentLocation));
					continue;
				}
				string[] xmlArguments = arguments.ToArray ();
				bool recognizedArgument = true;

				CustomAttribute attribute = new CustomAttribute (constructor);
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
							_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not parse argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' as a {constructor.Parameters[i].ParameterType.FullName}", 2021, _xmlDocumentLocation));
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
								_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' could not be transformed to the constructor parameter type", 2032, _xmlDocumentLocation));
							}
							break;
						default:
							_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Argument '{xmlArguments[i]}' specified in '{_xmlDocumentLocation}' is of unsupported type '{constructor.Parameters[i].ParameterType}'", 2020, _xmlDocumentLocation));
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

		public void ParseXml (string document)
		{
			_xmlDocumentLocation = document;
			_document = new XPathDocument (_xmlDocumentLocation);

			XPathNavigator nav = _document.CreateNavigator ();

			if (!nav.MoveToChild ("linker", string.Empty))
				return;

			try {
				ProcessAssemblies (_context, nav.SelectChildren ("assembly", string.Empty));
			} catch (Exception ex) when (!(ex is LinkerFatalErrorException)) {
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Error processing '{_xmlDocumentLocation}'", 1013), ex);
			}
		}

		void ProcessAssemblies (LinkContext context, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					return;

				if (GetFullName (iterator.Current) == "*") {
					foreach (AssemblyDefinition assemblyIterator in context.GetAssemblies ()) {
						ProcessTypes (assemblyIterator, iterator, true);
					}
				} else {
					AssemblyDefinition assembly = GetAssembly (context, GetAssemblyName (iterator.Current));

					if (assembly == null) {
						_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not resolve assembly {GetAssemblyName (iterator.Current).Name} specified in {_xmlDocumentLocation}", 2007, _xmlDocumentLocation));
						continue;
					}
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
					if (attributes.Count () > 0)
						_attributes[assembly] = attributes;
					ProcessTypes (assembly, iterator);
				}
			}
		}

		void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator, bool searchOnAllAssemblies = false)
		{
			iterator = iterator.Current.SelectChildren ("type", string.Empty);
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				if (!ShouldProcessElement (nav))
					continue;

				string fullname = GetFullName (nav);

				if (fullname.IndexOf ("*") != -1) {
					ProcessTypePattern (fullname, assembly, nav);
					continue;
				}

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null && assembly.MainModule.HasExportedTypes) {
					foreach (var exported in assembly.MainModule.ExportedTypes) {
						if (fullname == exported.FullName) {
							var resolvedExternal = exported.Resolve ();
							if (resolvedExternal != null) {
								type = resolvedExternal;
								break;
							}
						}
					}
				}

				if (type == null) {
					if (!searchOnAllAssemblies)
						_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not resolve type '{fullname}' specified in {_xmlDocumentLocation}", 2008, _xmlDocumentLocation));
					continue;
				}

				ProcessType (type, nav);
			}
		}

		void MatchType (TypeDefinition type, Regex regex, XPathNavigator nav)
		{
			if (regex.Match (type.FullName).Success)
				ProcessType (type, nav);

			if (!type.HasNestedTypes)
				return;

			foreach (var nt in type.NestedTypes)
				MatchType (nt, regex, nav);
		}

		void ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav)
		{
			Regex regex = new Regex (fullname.Replace (".", @"\.").Replace ("*", "(.*)"));

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				MatchType (type, regex, nav);
			}

			if (assembly.MainModule.HasExportedTypes) {
				foreach (var exported in assembly.MainModule.ExportedTypes) {
					if (regex.Match (exported.FullName).Success) {
						TypeDefinition type = exported.Resolve ();
						if (type != null) {
							ProcessType (type, nav);
						}
					}
				}
			}
		}

		void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (nav);
			if (attributes.Count () > 0)
				_attributes[type] = attributes;
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

		void ProcessTypeChildren (TypeDefinition type, XPathNavigator nav)
		{
			if (nav.HasChildren) {
				ProcessSelectedFields (nav, type);
				ProcessSelectedMethods (nav, type);
				ProcessSelectedProperties (nav, type);
				ProcessSelectedEvents (nav, type);
			}
		}

		void ProcessSelectedFields (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator fields = nav.SelectChildren ("field", string.Empty);
			if (fields.Count == 0)
				return;

			while (fields.MoveNext ()) {
				if (!ShouldProcessElement (fields.Current)) {
					return;
				}
				string value = GetSignature (fields.Current);
				if (!String.IsNullOrEmpty (value))
					ProcessFieldSignature (type, value, fields);

				value = GetAttribute (fields.Current, "name");
				if (!String.IsNullOrEmpty (value))
					ProcessFieldName (type, value, fields);
			}
		}

		void ProcessSelectedMethods (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator methods = nav.SelectChildren ("method", string.Empty);
			if (methods.Count == 0)
				return;

			while (methods.MoveNext ()) {
				if (!ShouldProcessElement (methods.Current)) {
					return;
				}

				string value = GetSignature (methods.Current);
				if (!String.IsNullOrEmpty (value))
					ProcessMethodSignature (type, value, methods);

				value = GetAttribute (methods.Current, "name");
				if (!String.IsNullOrEmpty (value))
					ProcessMethodName (type, value, methods);
			}
		}

		void ProcessSelectedProperties (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator properties = nav.SelectChildren ("property", string.Empty);
			if (properties.Count == 0)
				return;
			while (properties.MoveNext ()) {
				if (!ShouldProcessElement (properties.Current)) {
					return;
				}

				string value = GetSignature (properties.Current);
				if (!String.IsNullOrEmpty (value))
					ProcessPropertySignature (type, value, properties);

				value = GetAttribute (properties.Current, "name");
				if (!String.IsNullOrEmpty (value))
					ProcessPropertyName (type, value, properties);
			}
		}

		void ProcessSelectedEvents (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator events = nav.SelectChildren ("event", string.Empty);
			if (events.Count == 0)
				return;
			while (events.MoveNext ()) {
				if (!ShouldProcessElement (events.Current)) {
					return;
				}

				string value = GetSignature (events.Current);
				if (!String.IsNullOrEmpty (value))
					ProcessEventSignature (type, value, events);

				value = GetAttribute (events.Current, "name");
				if (!String.IsNullOrEmpty (value))
					ProcessEventName (type, value, events);
			}
		}

		void ProcessFieldSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			FieldDefinition field = GetField (type, signature);
			if (field == null) {
				_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not find field '{signature}' in type '{type.FullName}' specified in { _xmlDocumentLocation}", 2016, _xmlDocumentLocation));
				return;
			}
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
			if (attributes.Count () > 0)
				_attributes[field] = attributes;
		}

		void ProcessFieldName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name == name) {
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
					if (attributes.Count () > 0)
						_attributes[field] = attributes;
				}
			}
		}

		static FieldDefinition GetField (TypeDefinition type, string signature)
		{
			if (!type.HasFields)
				return null;

			foreach (FieldDefinition field in type.Fields)
				if (signature == field.FieldType.FullName + " " + field.Name)
					return field;

			return null;
		}

		void ProcessMethodSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			MethodDefinition method = GetMethod (type, signature);
			if (method == null) {
				_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not find method '{signature}' in type '{type.FullName}' specified in '{_xmlDocumentLocation}'", 2009, _xmlDocumentLocation));
				return;
			}
			ProcessMethod (method, iterator);
		}

		void ProcessMethod (MethodDefinition method, XPathNodeIterator iterator)
		{
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
			if (attributes.Count () > 0)
				_attributes[method] = attributes;
			ProcessReturnParameters (method, iterator);
			ProcessParameters (method, iterator);
		}

		void ProcessParameters (MethodDefinition method, XPathNodeIterator iterator)
		{
			iterator = iterator.Current.SelectChildren ("parameter", string.Empty);
			while (iterator.MoveNext ()) {
				IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
				if (attributes.Count () > 0) {
					string paramName = GetAttribute (iterator.Current, "name");
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (_attributes.ContainsKey (parameter))
								_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"There are duplicate parameter names for '{paramName}' inside '{method.Name}' in '{_xmlDocumentLocation}'", 2024, _xmlDocumentLocation));
							_attributes[parameter] = attributes;
							break;
						}
					}
				}
			}
		}

		void ProcessReturnParameters (MethodDefinition method, XPathNodeIterator iterator)
		{
			iterator = iterator.Current.SelectChildren ("return", string.Empty);
			bool firstAppearance = true;
			while (iterator.MoveNext ()) {
				if (firstAppearance) {
					firstAppearance = false;
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
					if (attributes.Count () > 0)
						_attributes[method.MethodReturnType] = attributes;
				} else {
					_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"There is more than one return parameter specified for '{method.Name}' in '{_xmlDocumentLocation}'", 2023, _xmlDocumentLocation));
				}
			}
		}

		void ProcessMethodName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods)
				if (name == method.Name)
					ProcessMethod (method, iterator);
		}

		static MethodDefinition GetMethod (TypeDefinition type, string signature)
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

		void ProcessPropertySignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			PropertyDefinition property = GetProperty (type, signature);
			if (property != null) {
				IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
				if (attributes.Count () > 0)
					_attributes[property] = attributes;
			}
		}

		void ProcessPropertyName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name == name) {
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
					if (attributes.Count () > 0)
						_attributes[property] = attributes;
				}
			}
		}

		void ProcessEventSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			EventDefinition @event = GetEvent (type, signature);
			if (@event == null) {
				_context.LogMessage (MessageContainer.CreateWarningMessage (_context, $"Could not find event '{signature}' in type '{type.FullName}' specified in {_xmlDocumentLocation}", 2016, _xmlDocumentLocation));
				return;
			}
			IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
			if (attributes.Count () > 0)
				_attributes[@event] = attributes;
		}

		void ProcessEventName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasEvents)
				return;

			foreach (EventDefinition @event in type.Events) {
				if (@event.Name == name) {
					IEnumerable<CustomAttribute> attributes = ProcessAttributes (iterator.Current);
					if (attributes.Count () > 0)
						_attributes[@event] = attributes;
				}
			}
		}

		AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName)
		{
			var assembly = context.Resolve (assemblyName);
			context.ResolveReferences (assembly);
			return assembly;
		}

		AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetFullName (nav));
		}

		static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, "signature");
		}

		static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, "fullname");
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, string.Empty);
		}

		protected static EventDefinition GetEvent (TypeDefinition type, string signature)
		{
			if (!type.HasEvents)
				return null;

			foreach (EventDefinition @event in type.Events)
				if (signature == @event.EventType.FullName + " " + @event.Name)
					return @event;

			return null;
		}

		static PropertyDefinition GetProperty (TypeDefinition type, string signature)
		{
			if (!type.HasProperties)
				return null;

			foreach (PropertyDefinition property in type.Properties)
				if (signature == property.PropertyType.FullName + " " + property.Name)
					return property;

			return null;
		}

		bool ShouldProcessElement (XPathNavigator nav)
		{
			var feature = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (feature))
				return true;

			var value = GetAttribute (nav, "featurevalue");
			if (string.IsNullOrEmpty (value)) {
				_context.LogMessage (MessageContainer.CreateErrorMessage ($"Feature {feature} does not specify a \"featurevalue\" attribute", 1001));
				return false;
			}

			if (!bool.TryParse (value, out bool bValue)) {
				_context.LogMessage (MessageContainer.CreateErrorMessage ($"Unsupported non-boolean feature definition {feature}", 1002));
				return false;
			}

			if (_context.FeatureSettings == null || !_context.FeatureSettings.TryGetValue (feature, out bool featureSetting))
				return false;

			return bValue == featureSetting;
		}
	}
}