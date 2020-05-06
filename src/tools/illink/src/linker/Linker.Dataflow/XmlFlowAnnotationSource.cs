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

		static readonly string _signature = "signature";
		static readonly string _fullname = "fullname";
		static readonly string _preserve = "preserve";
		static readonly string _accessors = "accessors";
		static readonly string _ns = string.Empty;

		static readonly string[] _accessorsAll = new string[] { "all" };
		static readonly char[] _accessorsSep = new char[] { ';' };

		readonly XPathDocument _document;
		readonly string _xmlDocumentLocation;
		readonly LinkContext _context;

		public class XmlResolutionException : Exception
		{
			public XmlResolutionException (string message, Exception innerException)
				: base (message, innerException)
			{
			}
		}

		public XmlFlowAnnotationSource (LinkContext context, string document)
		{
			_xmlDocumentLocation = document;
			_document = new XPathDocument (_xmlDocumentLocation);
			_context = context;
			Initialize ();
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			return _fields.TryGetValue (field, out var ann) ? ann : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index)
		{
			if (_methods.TryGetValue (method, out var ann) && ann.ParameterAnnotations != null) {
				string paramName = method.Parameters[index].Name;

				foreach (var (ParamName, Annotation) in ann.ParameterAnnotations)
					if (ParamName == paramName)
						return Annotation;
			}

			return DynamicallyAccessedMemberTypes.None;
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

		static DynamicallyAccessedMemberTypes ParseKinds (ArrayBuilder<Attribute> attributes)
		{
			foreach (var attribute in attributes.ToArray ()) {
				if (attribute.attributeName == "System.Runtime.CompilerServices.DynamicallyAccessedMembers" && attribute.arguments.Count == 1) {
					foreach (var argument in attribute.arguments.ToArray ()) {
						if (argument == string.Empty)
							break;
						return (DynamicallyAccessedMemberTypes) Enum.Parse (typeof (DynamicallyAccessedMemberTypes), argument);
					}
				}
			}
			return DynamicallyAccessedMemberTypes.None;
		}

		void Initialize ()
		{
			XPathNavigator nav = _document.CreateNavigator ();

			// This step can be created with XML files that aren't necessarily
			// annotations descriptor files. So bail if we don't have a <annotations> element.
			if (!nav.MoveToChild ("annotations", _ns))
				return;

			try {
				ProcessAssemblies (_context, nav.SelectChildren ("assembly", _ns));
			} catch (Exception ex) when (!(ex is XmlResolutionException)) {
				throw new XmlResolutionException (string.Format ("Failed to process XML description: {0}", _document), ex);
			}
		}

		protected virtual void ProcessAssemblies (LinkContext context, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				AssemblyDefinition assembly = GetAssembly (context, GetAssemblyName (iterator.Current));

				if (assembly == null) {
					context.LogMessage ($"Assembly {GetAssemblyName (iterator.Current).Name} couldn't be resolved");
					continue;
				}

				ProcessAssembly (assembly, iterator);
			}
		}

		protected virtual void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (iterator.Current))
				return;
#endif
			ProcessTypes (assembly, iterator.Current.SelectChildren ("type", _ns));
		}

		ArrayBuilder<Attribute> ProcessAttributes (XPathNodeIterator iterator)
		{
			var attributes = new ArrayBuilder<Attribute> ();
			while (iterator.MoveNext ()) {
				string attributeName = GetFullName (iterator.Current);
				ArrayBuilder<string> arguments = GetAttributeArguments (iterator.Current.SelectChildren ("argument", _ns));
				ArrayBuilder<string> fields = GetAttributeFields (iterator.Current.SelectChildren ("field", _ns));
				ArrayBuilder<string> properties = GetAttributeProperties (iterator.Current.SelectChildren ("property", _ns));

				attributes.Add (new Attribute (attributeName, arguments, fields, properties));
			}
			return attributes;
		}

		ArrayBuilder<string> GetAttributeArguments (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> arguments = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				arguments.Add (iterator.Current.Value);
			}
			return arguments;
		}

		ArrayBuilder<string> GetAttributeFields (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> fields = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				fields.Add (iterator.Current.Value);
			}
			return fields;
		}

		ArrayBuilder<string> GetAttributeProperties (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> properties = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				properties.Add (iterator.Current.Value);
			}
			return properties;
		}

		void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				string fullname = GetFullName (nav);

				if (IsTypePattern (fullname)) {
					ProcessTypePattern (fullname, assembly, nav);
					continue;
				}

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null) {
					if (assembly.MainModule.HasExportedTypes) {
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
				}

				if (type == null)
					continue;

				ProcessType (type, nav);
			}
		}

		static bool IsTypePattern (string fullname)
		{
			return fullname.IndexOf ("*") != -1;
		}

		static Regex CreateRegexFromPattern (string pattern)
		{
			return new Regex (pattern.Replace (".", @"\.").Replace ("*", "(.*)"));
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

		void MatchExportedType (ExportedType exportedType, ModuleDefinition module, Regex regex, XPathNavigator nav)
		{
			if (regex.Match (exportedType.FullName).Success) {
				TypeDefinition type = exportedType.Resolve ();
				if (type != null) {
					ProcessType (type, nav);
				}
			}
		}


		void ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav)
		{
			Regex regex = CreateRegexFromPattern (fullname);

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				MatchType (type, regex, nav);
			}

			if (assembly.MainModule.HasExportedTypes) {
				foreach (var exported in assembly.MainModule.ExportedTypes) {
					MatchExportedType (exported, assembly.MainModule, regex, nav);
				}
			}
		}

		protected virtual void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (nav))
				return;
#endif
			ProcessTypeChildren (type, nav);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes) {
				var iterator = nav.SelectChildren ("type", _ns);
				while (iterator.MoveNext ()) {
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
			XPathNodeIterator fields = nav.SelectChildren ("field", _ns);
			if (fields.Count == 0)
				return;

			ProcessFields (type, fields);
		}

		void ProcessSelectedMethods (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator methods = nav.SelectChildren ("method", _ns);
			if (methods.Count == 0)
				return;

			ProcessMethods (type, methods);
		}

		void ProcessSelectedProperties (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator properties = nav.SelectChildren ("property", _ns);
			if (properties.Count == 0)
				return;

			ProcessProperties (type, properties);
		}

		static TypePreserve GetTypePreserve (XPathNavigator nav)
		{
			string attribute = GetAttribute (nav, _preserve);
			if (string.IsNullOrEmpty (attribute))
				return nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All;

			if (Enum.TryParse (attribute, true, out TypePreserve result))
				return result;
			return TypePreserve.Nothing;
		}

		void ProcessFields (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ())
				ProcessField (type, iterator);
		}

		protected virtual void ProcessField (TypeDefinition type, XPathNodeIterator iterator)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (iterator.Current))
				return;
#endif
			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessFieldSignature (type, value, iterator);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessFieldName (type, value, iterator);
		}

		void ProcessFieldSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			FieldDefinition field = GetField (type, signature);
			if (field != null) {
				ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns));
				DynamicallyAccessedMemberTypes fieldAnnotation = ParseKinds (attributes);
				if (fieldAnnotation != DynamicallyAccessedMemberTypes.None)
					_fields[field] = fieldAnnotation;
			}
		}

		void ProcessFieldName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name == name) {
					ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns));
					DynamicallyAccessedMemberTypes fieldAnnotation = ParseKinds (attributes);
					if (fieldAnnotation != DynamicallyAccessedMemberTypes.None)
						_fields[field] = fieldAnnotation;
				}
			}
		}

		protected static FieldDefinition GetField (TypeDefinition type, string signature)
		{
			if (!type.HasFields)
				return null;

			foreach (FieldDefinition field in type.Fields)
				if (signature == GetFieldSignature (field))
					return field;

			return null;
		}

		static string GetFieldSignature (FieldDefinition field)
		{
			return field.FieldType.FullName + " " + field.Name;
		}

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ())
				ProcessMethod (type, iterator);
		}

		protected virtual void ProcessMethod (TypeDefinition type, XPathNodeIterator iterator)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (iterator.Current))
				return;
#endif

			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessMethodSignature (type, value, iterator);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessMethodName (type, value, iterator);
		}

		void ProcessMethodSignature (TypeDefinition type, string signature, XPathNodeIterator iterator)
		{
			MethodDefinition meth = GetMethod (type, signature);
			if (meth != null)
				ProcessMethodChildren (type, meth, iterator);
		}

		void ProcessMethodChildren (TypeDefinition type, MethodDefinition method, XPathNodeIterator iterator)
		{
			ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns));
			ArrayBuilder<(string, ArrayBuilder<Attribute>)> parameterAnnotations = ProcessParameters (type,
				method, iterator.Current.SelectChildren ("parameter", _ns));
			ArrayBuilder<ArrayBuilder<Attribute>> returnParameterAnnotations = ProcessReturnParameters (type,
				method, iterator.Current.SelectChildren ("returnparameter", _ns));

			var parameterAnnotation = new ArrayBuilder<(string ParamName, DynamicallyAccessedMemberTypes Annotation)> ();
			DynamicallyAccessedMemberTypes returnAnnotation = 0;

			if (parameterAnnotations.Count > 0) {
				foreach (var parameter in parameterAnnotations.ToArray ()) {
					DynamicallyAccessedMemberTypes paramAnnotation = ParseKinds (parameter.Item2);
					if (paramAnnotation != 0)
						parameterAnnotation.Add ((parameter.Item1, paramAnnotation));
				}
			}

			if (returnParameterAnnotations.Count == 1) {
				foreach (var returnparameter in returnParameterAnnotations.ToArray ()) {
					DynamicallyAccessedMemberTypes returnparamAnnotation = ParseKinds (returnparameter);
					if (returnparamAnnotation != 0)
						returnAnnotation = returnparamAnnotation;
				}
			}
			if (returnAnnotation != 0 || parameterAnnotation.Count > 0)
				_methods[method] = new AnnotatedMethod (returnAnnotation, parameterAnnotation.ToArray ());

		}

		ArrayBuilder<(string, ArrayBuilder<Attribute>)> ProcessParameters (TypeDefinition type,
			MethodDefinition method, XPathNodeIterator iterator)
		{
			var methodParameters = new ArrayBuilder<(string, ArrayBuilder<Attribute>)> ();
			while (iterator.MoveNext ()) {
				methodParameters.Add ((GetAttribute (iterator.Current, "name"), ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns))));
			}
			return methodParameters;
		}

		ArrayBuilder<ArrayBuilder<Attribute>> ProcessReturnParameters (TypeDefinition type,
			MethodDefinition method, XPathNodeIterator iterator)
		{
			var methodParameters = new ArrayBuilder<ArrayBuilder<Attribute>> ();

			while (iterator.MoveNext ()) {
				methodParameters.Add (ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns)));
			}
			return methodParameters;
		}


		void ProcessMethodIfNotNull (TypeDefinition type, MethodDefinition method, XPathNodeIterator iterator)
		{
			if (method == null)
				return;

			ProcessMethodChildren (type, method, iterator);
		}

		void ProcessMethodName (TypeDefinition type, string name, XPathNodeIterator iterator)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods)
				if (name == method.Name)
					ProcessMethodChildren (type, method, iterator);
		}

		protected static MethodDefinition GetMethod (TypeDefinition type, string signature)
		{
			if (type.HasMethods)
				foreach (MethodDefinition meth in type.Methods)
					if (signature == GetMethodSignature (meth, false))
						return meth;

			return null;
		}

		public static string GetMethodSignature (MethodDefinition meth, bool includeGenericParameters)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (meth.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (meth.Name);
			if (includeGenericParameters && meth.HasGenericParameters) {
				sb.Append ("`");
				sb.Append (meth.GenericParameters.Count);
			}

			sb.Append ("(");
			if (meth.HasParameters) {
				for (int i = 0; i < meth.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (meth.Parameters[i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		void ProcessProperties (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ())
				ProcessProperty (type, iterator);
		}

		protected virtual void ProcessProperty (TypeDefinition type, XPathNodeIterator iterator)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (iterator.Current))
				return;
#endif

			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessPropertySignature (type, value, GetAccessors (iterator.Current), iterator);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessPropertyName (type, value, _accessorsAll, iterator);
		}

		void ProcessPropertySignature (TypeDefinition type, string signature, string[] accessors, XPathNodeIterator iterator)
		{
			PropertyDefinition property = GetProperty (type, signature);
			if (property != null) {
				ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns));
				DynamicallyAccessedMemberTypes propertyAnnotation = ParseKinds (attributes);
				if (propertyAnnotation != DynamicallyAccessedMemberTypes.None)
					_properties[property] = propertyAnnotation;
			}
		}

		void ProcessPropertyName (TypeDefinition type, string name, string[] accessors, XPathNodeIterator iterator)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name == name) {
					ArrayBuilder<Attribute> attributes = ProcessAttributes (iterator.Current.SelectChildren ("attribute", _ns));
					DynamicallyAccessedMemberTypes propertyAnnotation = ParseKinds (attributes);
					if (propertyAnnotation != DynamicallyAccessedMemberTypes.None)
						_properties[property] = propertyAnnotation;
				}
			}
		}

		protected static PropertyDefinition GetProperty (TypeDefinition type, string signature)
		{
			if (!type.HasProperties)
				return null;

			foreach (PropertyDefinition property in type.Properties)
				if (signature == GetPropertySignature (property))
					return property;

			return null;
		}

		static string GetPropertySignature (PropertyDefinition property)
		{
			return property.PropertyType.FullName + " " + property.Name;
		}

		protected AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName)
		{
			var assembly = context.Resolve (assemblyName);
			ProcessReferences (assembly, context);
			return assembly;
		}

		protected virtual AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetFullName (nav));
		}

		static void ProcessReferences (AssemblyDefinition assembly, LinkContext context)
		{
			context.ResolveReferences (assembly);
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, _signature);
		}

		static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, _fullname);
		}

		protected static string[] GetAccessors (XPathNavigator nav)
		{
			string accessorsValue = GetAttribute (nav, _accessors);

			if (accessorsValue != null) {
				string[] accessors = accessorsValue.Split (
					_accessorsSep, StringSplitOptions.RemoveEmptyEntries);

				if (accessors.Length > 0) {
					for (int i = 0; i < accessors.Length; ++i)
						accessors[i] = accessors[i].ToLower ();

					return accessors;
				}
			}
			return _accessorsAll;
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, _ns);
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
			public ArrayBuilder<string> fields { get; set; }
			public ArrayBuilder<string> properties { get; set; }

			public Attribute (string _attributeName, ArrayBuilder<string> _arguments, ArrayBuilder<string> _fields, ArrayBuilder<string> _properties)
			{
				attributeName = _attributeName;
				arguments = _arguments;
				fields = _fields;
				properties = _properties;
			}
		}

#if !FEATURE_ILLINK
		protected virtual bool IsExcluded (XPathNavigator nav)
		{
			var value = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (value))
				return false;

			return Context.IsFeatureExcluded (value);
		}
#endif

		public override string ToString ()
		{
			return "XmlFlowAnnotationSource: " + _document;
		}
	}
}
