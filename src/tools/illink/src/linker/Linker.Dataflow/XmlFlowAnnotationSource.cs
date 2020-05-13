// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	class XmlFlowAnnotationSource : IFlowAnnotationSource
	{
		readonly Dictionary<MethodDefinition, AnnotatedMethod> _methods = new Dictionary<MethodDefinition, AnnotatedMethod> ();
		readonly Dictionary<PropertyDefinition, DynamicallyAccessedMemberTypes> _properties = new Dictionary<PropertyDefinition, DynamicallyAccessedMemberTypes> ();
		readonly Dictionary<FieldDefinition, DynamicallyAccessedMemberTypes> _fields = new Dictionary<FieldDefinition, DynamicallyAccessedMemberTypes> ();

		readonly XPathDocument _document;
		readonly string _xmlDocumentLocation;
		readonly LinkContext _context;

		public XmlFlowAnnotationSource (LinkContext context, string document)
		{
			_xmlDocumentLocation = document;
			_document = new XPathDocument (_xmlDocumentLocation);
			_context = context;
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			return _fields.TryGetValue (field, out var ann) ? ann : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index)
		{
			DynamicallyAccessedMemberTypes parameterAnnotation = DynamicallyAccessedMemberTypes.None;

			if (_methods.TryGetValue (method, out var ann) && ann.ParameterAnnotations != null) {
				string paramName = method.Parameters[index].Name;

				bool firstAppearance = true;
				foreach (var (ParamName, Annotation) in ann.ParameterAnnotations) {
					if (ParamName == paramName && firstAppearance) {
						firstAppearance = false;
						parameterAnnotation = Annotation;
					} else if (ParamName == paramName && !firstAppearance) {
						_context.LogMessage (MessageContainer.CreateWarningMessage ($"There are duplicate parameter names for '{paramName}' inside '{method.Name}' in '{_xmlDocumentLocation}'", 2024));
					}
				}
			}

			return parameterAnnotation;
		}

		public DynamicallyAccessedMemberTypes GetPropertyAnnotation (PropertyDefinition property)
		{
			return _properties.TryGetValue (property, out var ann) ? ann : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			return _methods.TryGetValue (method, out var ann) ? ann.ReturnAnnotation : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetThisParameterAnnotation (MethodDefinition method)
		{
			if (_methods.TryGetValue (method, out var ann) && ann.ParameterAnnotations != null) {
				foreach (var (ParamName, Annotation) in ann.ParameterAnnotations)
					if (ParamName == "this")
						return Annotation;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		static DynamicallyAccessedMemberTypes GetMemberTypesForDynamicallyAccessedMemberAttribute (ArrayBuilder<Attribute> attributes, LinkContext _context, string _xmlDocumentLocation)
		{
			foreach (var attribute in attributes.ToArray ()) {
				if (attribute.attributeName == "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers" && attribute.arguments.Count == 1) {
					if (attribute.arguments.Count == 0) {
						_context.LogMessage (MessageContainer.CreateWarningMessage ($"DynamicallyAccessedMembers attribute was specified but no argument was proportioned", 2020));
					} else if (attribute.arguments.Count == 1) {
						DynamicallyAccessedMemberTypes result;
						foreach (var argument in attribute.arguments.ToArray ()) {
							if (argument == string.Empty)
								break;
							if (Enum.TryParse (argument, false, out result)) {
								return result;
							} else {
								_context.LogMessage (MessageContainer.CreateWarningMessage ($"Could not parse argument '{argument}' specified in '{_xmlDocumentLocation}' as a DynamicallyAccessedMemberTypes", 2021));
							}
						}
					} else {
						_context.LogMessage (MessageContainer.CreateWarningMessage ($"DynamicallyAccessedMembers attribute was specified but there is more than one argument", 2022));
					}
				}
			}
			return DynamicallyAccessedMemberTypes.None;
		}

		public void ParseXml ()
		{
			XPathNavigator nav = _document.CreateNavigator ();

			if (!nav.MoveToChild ("linker", string.Empty))
				return;

			try {
				ProcessAssemblies (_context, nav.SelectChildren ("assembly", string.Empty));
			} catch (Exception ex) when (!(ex is LinkerFatalErrorException)) {
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Error processing '{_xmlDocumentLocation}'", 1013), ex);
			}
		}

		private void ProcessAssemblies (LinkContext context, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current)) {
					return;
				}

				AssemblyDefinition assembly = GetAssembly (context, GetAssemblyName (iterator.Current));

				if (assembly == null) {
					_context.LogMessage (MessageContainer.CreateWarningMessage ($"Could not resolve assembly {GetAssemblyName (iterator.Current).Name} specified in {_xmlDocumentLocation}", 2007));
					continue;
				}
				ProcessTypes (assembly, iterator.Current.SelectChildren ("type", string.Empty));
			}
		}

		ArrayBuilder<Attribute> ProcessAttributes (XPathNodeIterator iterator)
		{
			iterator = iterator.Current.SelectChildren ("attribute", string.Empty);
			var attributes = new ArrayBuilder<Attribute> ();
			while (iterator.MoveNext ()) {
				string attributeName = GetFullName (iterator.Current);
				ArrayBuilder<string> arguments = GetAttributeChildren (iterator.Current.SelectChildren ("argument", string.Empty));

				attributes.Add (new Attribute (attributeName, arguments));
			}
			return attributes;
		}

		ArrayBuilder<string> GetAttributeChildren (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> childs = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				childs.Add (iterator.Current.Value);
			}
			return childs;
		}

		void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
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
					_context.LogMessage (MessageContainer.CreateWarningMessage ($"Could not resolve type '{fullname}' specified in {_xmlDocumentLocation}", 2008));
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
			if (!ShouldProcessElement (nav)) {
				return;
			}

			ProcessTypeChildren (type, nav);

			if (!type.HasNestedTypes)
				return;

			var iterator = nav.SelectChildren ("type", string.Empty);
			while (iterator.MoveNext ()) {
				foreach (TypeDefinition nested in type.NestedTypes) {
					if (nested.Name == GetAttribute (iterator.Current, "name"))
						ProcessTypeChildren (nested, iterator.Current);
				}
			}
		}

		void ProcessTypeChildren (TypeDefinition type, XPathNavigator nav)
		{
			if (nav.HasChildren) {
				ProcessSelectedFields (nav, type);
				ProcessSelectedMethods (nav, type);
				ProcessSelectedProperties (nav, type);
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

		void ProcessFieldSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			FieldDefinition field = GetField (type, signature);
			if (field == null) {
				_context.LogMessage (MessageContainer.CreateWarningMessage ($"Could not find field '{signature}' in type '{type.FullName}' specified in { _xmlDocumentLocation}", 2016));
				return;
			}
			ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator);
			DynamicallyAccessedMemberTypes fieldAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (attributes, _context, _xmlDocumentLocation);
			if (fieldAnnotation != DynamicallyAccessedMemberTypes.None)
				_fields[field] = fieldAnnotation;
		}

		void ProcessFieldName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name == name) {
					ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator);
					DynamicallyAccessedMemberTypes fieldAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (attributes, _context, _xmlDocumentLocation);
					if (fieldAnnotation != DynamicallyAccessedMemberTypes.None)
						_fields[field] = fieldAnnotation;
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
				_context.LogMessage (MessageContainer.CreateWarningMessage ($"Could not find method '{signature}' in type '{type.FullName}' specified in '{_xmlDocumentLocation}'", 2009));
				return;
			}
			ProcessMethodChildren (type, method, iterator);
		}

		void ProcessMethodChildren (TypeDefinition type, MethodDefinition method, XPathNodeIterator iterator)
		{
			ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator);
			ArrayBuilder<(string, ArrayBuilder<Attribute>)> parameterAnnotations = ProcessParameters (type,
				method, iterator.Current.SelectChildren ("parameter", string.Empty));
			ArrayBuilder<ArrayBuilder<Attribute>> returnParameterAnnotations = ProcessReturnParameters (type,
				method, iterator.Current.SelectChildren ("return", string.Empty));

			var parameterAnnotation = new ArrayBuilder<(string ParamName, DynamicallyAccessedMemberTypes Annotation)> ();
			DynamicallyAccessedMemberTypes returnAnnotation = 0;

			if (parameterAnnotations.Count > 0) {
				foreach (var parameter in parameterAnnotations.ToArray ()) {
					DynamicallyAccessedMemberTypes paramAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (parameter.Item2, _context, _xmlDocumentLocation);
					if (paramAnnotation != 0)
						parameterAnnotation.Add ((parameter.Item1, paramAnnotation));
				}
			}

			if (returnParameterAnnotations.Count == 1) {
				foreach (var returnparameter in returnParameterAnnotations.ToArray ()) {
					DynamicallyAccessedMemberTypes returnparamAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (returnparameter, _context, _xmlDocumentLocation);
					if (returnparamAnnotation != 0)
						returnAnnotation = returnparamAnnotation;
				}
			} else {
				_context.LogMessage (MessageContainer.CreateWarningMessage ($"There is more than one return parameter specified for '{method.Name}' in '{_xmlDocumentLocation}'", 2023));
			}
			if (returnAnnotation != 0 || parameterAnnotation.Count > 0)
				_methods[method] = new AnnotatedMethod (returnAnnotation, parameterAnnotation.ToArray ());

		}

		ArrayBuilder<(string, ArrayBuilder<Attribute>)> ProcessParameters (TypeDefinition type,
			MethodDefinition method, XPathNodeIterator iterator)
		{
			var methodParameters = new ArrayBuilder<(string, ArrayBuilder<Attribute>)> ();
			while (iterator.MoveNext ()) {
				methodParameters.Add ((GetAttribute (iterator.Current, "name"), ProcessAttributes (iterator)));
			}
			return methodParameters;
		}

		ArrayBuilder<ArrayBuilder<Attribute>> ProcessReturnParameters (TypeDefinition type,
			MethodDefinition method, XPathNodeIterator iterator)
		{
			var methodParameters = new ArrayBuilder<ArrayBuilder<Attribute>> ();

			while (iterator.MoveNext ()) {
				methodParameters.Add (ProcessAttributes (iterator));
			}
			return methodParameters;
		}

		void ProcessMethodName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods)
				if (name == method.Name)
					ProcessMethodChildren (type, method, iterator);
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
				ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator);
				DynamicallyAccessedMemberTypes propertyAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (attributes, _context, _xmlDocumentLocation);
				if (propertyAnnotation != DynamicallyAccessedMemberTypes.None)
					_properties[property] = propertyAnnotation;
			}
		}

		void ProcessPropertyName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name == name) {
					ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator);
					DynamicallyAccessedMemberTypes propertyAnnotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (attributes, _context, _xmlDocumentLocation);
					if (propertyAnnotation != DynamicallyAccessedMemberTypes.None)
						_properties[property] = propertyAnnotation;
				}
			}
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

		private struct AnnotatedMethod
		{
			public readonly DynamicallyAccessedMemberTypes ReturnAnnotation;
			public readonly (string ParamName, DynamicallyAccessedMemberTypes Annotation)[] ParameterAnnotations;

			public AnnotatedMethod (DynamicallyAccessedMemberTypes returnAnnotation,
				(string ParamName, DynamicallyAccessedMemberTypes Annotation)[] paramAnnotations)
				=> (ReturnAnnotation, ParameterAnnotations) = (returnAnnotation, paramAnnotations);
		}

		private struct Attribute
		{
			public string attributeName { get; set; }
			public ArrayBuilder<string> arguments { get; set; }

			public Attribute (string _attributeName, ArrayBuilder<string> _arguments)
			{
				attributeName = _attributeName;
				arguments = _arguments;
			}
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
