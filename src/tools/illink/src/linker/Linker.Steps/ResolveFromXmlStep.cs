//
// ResolveFromXmlStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
// (C) 2007 Novell, Inc.
// Copyright 2013 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class XmlResolutionException : Exception {
		public XmlResolutionException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}

	public class ResolveFromXmlStep : ResolveStep {

		static readonly string _signature = "signature";
		static readonly string _fullname = "fullname";
		static readonly string _required = "required";
		static readonly string _preserve = "preserve";
		static readonly string _accessors = "accessors";
		static readonly string _ns = string.Empty;

		static readonly string[] _accessorsAll = new string[] { "all" };
		static readonly char[] _accessorsSep = new char[] { ';' };

		readonly XPathDocument _document;
		readonly string _xmlDocumentLocation;
		readonly string _resourceName;
		readonly AssemblyDefinition _resourceAssembly;

		public ResolveFromXmlStep (XPathDocument document, string xmlDocumentLocation = "<unspecified>")
		{
			_document = document;
			_xmlDocumentLocation = xmlDocumentLocation;
		}

		public ResolveFromXmlStep (XPathDocument document, string resourceName, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: this (document, xmlDocumentLocation)
		{
			if (string.IsNullOrEmpty (resourceName))
				throw new ArgumentNullException (nameof (resourceName));

			_resourceName = resourceName;
			_resourceAssembly = resourceAssembly ?? throw new ArgumentNullException (nameof (resourceAssembly));
		}

		protected override void Process ()
		{
			XPathNavigator nav = _document.CreateNavigator ();

			// This step can be created with XML files that aren't necessarily
			// linker descriptor files. So bail if we don't have a <linker> element.
			if (!nav.MoveToChild("linker", _ns))
				return;

			try {
				ProcessAssemblies (Context, nav.SelectChildren ("assembly", _ns));

				if (!string.IsNullOrEmpty (_resourceName) && Context.StripResources)
					Context.Annotations.AddResourceToRemove (_resourceAssembly, _resourceName);
			} catch (Exception ex) when (!(ex is XmlResolutionException)) {
				throw new XmlResolutionException (string.Format ("Failed to process XML description: {0}", _xmlDocumentLocation), ex);
			}
		}

		protected virtual void ProcessAssemblies (LinkContext context, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				AssemblyDefinition assembly = GetAssembly (context, GetAssemblyName (iterator.Current));
				if (assembly != null)
					ProcessAssembly (assembly, iterator);
			}
		}

		protected virtual void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			if (IsExcluded (iterator.Current))
				return;

			if (GetTypePreserve (iterator.Current) == TypePreserve.All) {
				foreach (var type in assembly.MainModule.Types)
					MarkAndPreserveAll (type);
			} else {
				ProcessTypes (assembly, iterator.Current.SelectChildren ("type", _ns));
				ProcessNamespaces (assembly, iterator.Current.SelectChildren ("namespace", _ns));
			}
		}

		void ProcessNamespaces (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				string fullname = GetFullName (iterator.Current);
				foreach (TypeDefinition type in assembly.MainModule.Types) {
					if (type.Namespace != fullname)
						continue;

					MarkAndPreserveAll (type);
				}
			}
		}

		void MarkAndPreserveAll (TypeDefinition type)
		{
			Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			Annotations.SetPreserve (type, TypePreserve.All);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes)
				MarkAndPreserveAll (nested);
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
								MarkingHelpers.MarkExportedType (exported, assembly.MainModule, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
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
				MarkingHelpers.MarkExportedType (exportedType, module, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
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
			if (IsExcluded (nav))
				return;
			
			TypePreserve preserve = GetTypePreserve (nav);
			if (preserve != TypePreserve.Nothing)
				Annotations.SetPreserve (type, preserve);

			bool required = IsRequired (nav);
			MarkChildren (type, nav, required);

			if (!required)
				return;

			if (Annotations.IsMarked (type)) { 
				var existingLevel = Annotations.TryGetPreserve (type, out TypePreserve existingPreserve) ? existingPreserve : TypePreserve.Nothing; 
				var duplicateLevel = preserve != TypePreserve.Nothing ? preserve : nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All; 
				Context.LogMessage ($"Duplicate preserve in {_xmlDocumentLocation} of {type.FullName} ({existingLevel}).  Duplicate uses ({duplicateLevel})"); 
			} 

			Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			if (type.IsNested) {
				var currentType = type;
				while (currentType.IsNested) {
					var parent = currentType.DeclaringType;
					Context.Annotations.Mark (parent, new DependencyInfo (DependencyKind.DeclaringType, currentType));
					currentType = parent;
				}
			}
		}

		void MarkSelectedFields (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator fields = nav.SelectChildren ("field", _ns);
			if (fields.Count == 0)
				return;

			ProcessFields (type, fields);
		}

		void MarkChildren (TypeDefinition type, XPathNavigator nav, bool required)
		{
			if (nav.HasChildren) {
				MarkSelectedFields (nav, type);
				MarkSelectedMethods (nav, type, required);
				MarkSelectedEvents (nav, type, required);
				MarkSelectedProperties (nav, type, required);
			}
		}

		void MarkSelectedMethods (XPathNavigator nav, TypeDefinition type, bool required)
		{
			XPathNodeIterator methods = nav.SelectChildren ("method", _ns);
			if (methods.Count == 0)
				return;

			ProcessMethods (type, methods, required);
		}

		void MarkSelectedEvents (XPathNavigator nav, TypeDefinition type, bool required)
		{
			XPathNodeIterator events = nav.SelectChildren ("event", _ns);
			if (events.Count == 0)
				return;

			ProcessEvents (type, events, required);
		}

		void MarkSelectedProperties (XPathNavigator nav, TypeDefinition type, bool required)
		{
			XPathNodeIterator properties = nav.SelectChildren ("property", _ns);
			if (properties.Count == 0)
				return;

			ProcessProperties (type, properties, required);
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
			if (IsExcluded (iterator.Current))
				return;
			
			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessFieldSignature (type, value);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessFieldName (type, value);
		}

		void ProcessFieldSignature (TypeDefinition type, string signature)
		{
			FieldDefinition field = GetField (type, signature);
			if (field == null) {
				AddUnresolveMarker (string.Format ("T: {0}; F: {1}", type, signature));
				return;
			}

			MarkField (type, field);
		}

		void MarkField (TypeDefinition type, FieldDefinition field)
		{
			if (Annotations.IsMarked (field))
				Context.LogMessage ($"Duplicate preserve in {_xmlDocumentLocation} of {field.FullName}");
				
			Context.Annotations.Mark (field, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
		}

		void ProcessFieldName (TypeDefinition type, string name)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields)
				if (field.Name == name)
					MarkField (type, field);
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

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			while (iterator.MoveNext ())
				ProcessMethod (type, iterator, required);
		}

		protected virtual void ProcessMethod (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			if (IsExcluded (iterator.Current))
				return;
			
			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessMethodSignature (type, value, required);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessMethodName (type, value, required);
		}

		void ProcessMethodSignature (TypeDefinition type, string signature, bool required)
		{
			MethodDefinition meth = GetMethod (type, signature);
			if (meth == null) {
				AddUnresolveMarker (string.Format ("T: {0}; M: {1}", type, signature));
				return;
			}

			MarkMethod (type, meth, required);
		}

		void MarkMethod (TypeDefinition type, MethodDefinition method, bool required)
		{
			if (Annotations.IsMarked (method)) 
				Context.LogMessage ($"Duplicate preserve in {_xmlDocumentLocation} of {method.FullName}");

			Annotations.Mark (method, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			Annotations.MarkIndirectlyCalledMethod (method);
			Annotations.SetAction (method, MethodAction.Parse);

			if (!required)
				Annotations.AddPreservedMethod (type, method);
		}

		void MarkMethodIfNotNull (TypeDefinition type, MethodDefinition method, bool required)
		{
			if (method == null)
				return;

			MarkMethod (type, method, required);
		}

		void ProcessMethodName (TypeDefinition type, string name, bool required)
		{
			if (!type.HasMethods)
				return;

			foreach (MethodDefinition method in type.Methods)
				if (name == method.Name)
					MarkMethod (type, method, required);
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

					sb.Append (meth.Parameters [i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		void ProcessEvents (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			while (iterator.MoveNext ())
				ProcessEvent (type, iterator, required);
		}

		protected virtual void ProcessEvent (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			if (IsExcluded (iterator.Current))
				return;
			
			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessEventSignature (type, value, required);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessEventName (type, value, required);
		}

		void ProcessEventSignature (TypeDefinition type, string signature, bool required)
		{
			EventDefinition @event = GetEvent (type, signature);
			if (@event == null) {
				AddUnresolveMarker (string.Format ("T: {0}; E: {1}", type, signature));
				return;
			}

			MarkEvent (type, @event, required);
		}

		void MarkEvent (TypeDefinition type, EventDefinition @event, bool required)
		{
			if (Annotations.IsMarked (@event))
				Context.LogMessage ($"Duplicate preserve in {_xmlDocumentLocation} of {@event.FullName}");

			Annotations.Mark (@event, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			MarkMethod (type, @event.AddMethod, required);
			MarkMethod (type, @event.RemoveMethod, required);
			MarkMethodIfNotNull (type, @event.InvokeMethod, required);
		}

		void ProcessEventName (TypeDefinition type, string name, bool required)
		{
			if (!type.HasEvents)
				return;

			foreach (EventDefinition @event in type.Events)
				if (@event.Name == name)
					MarkEvent (type, @event, required);
		}

		protected static EventDefinition GetEvent (TypeDefinition type, string signature)
		{
			if (!type.HasEvents)
				return null;

			foreach (EventDefinition @event in type.Events)
				if (signature == GetEventSignature (@event))
					return @event;

			return null;
		}

		static string GetEventSignature (EventDefinition @event)
		{
			return @event.EventType.FullName + " " + @event.Name;
		}

		void ProcessProperties (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			while (iterator.MoveNext ())
				ProcessProperty (type, iterator, required);
		}

		protected virtual void ProcessProperty (TypeDefinition type, XPathNodeIterator iterator, bool required)
		{
			if (IsExcluded (iterator.Current))
				return;
			
			string value = GetSignature (iterator.Current);
			if (!String.IsNullOrEmpty (value))
				ProcessPropertySignature (type, value, GetAccessors (iterator.Current), required);

			value = GetAttribute (iterator.Current, "name");
			if (!String.IsNullOrEmpty (value))
				ProcessPropertyName (type, value, _accessorsAll, required);
		}

		void ProcessPropertySignature (TypeDefinition type, string signature, string[] accessors, bool required)
		{
			PropertyDefinition property = GetProperty (type, signature);
			if (property == null) {
				AddUnresolveMarker (string.Format ("T: {0}; P: {1}", type, signature));
				return;
			}

			MarkProperty (type, property, accessors, required);
		}

		void MarkProperty (TypeDefinition type, PropertyDefinition property, string[] accessors, bool required)
		{
			if (Annotations.IsMarked (property))
				Context.LogMessage ($"Duplicate preserve in {_xmlDocumentLocation} of {property.FullName}");
				
			Annotations.Mark (property, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			MarkPropertyAccessors (type, property, accessors, required);
		}

		void MarkPropertyAccessors (TypeDefinition type, PropertyDefinition property, string[] accessors, bool required)
		{
			if (Array.IndexOf (accessors, "all") >= 0) {
				MarkMethodIfNotNull (type, property.GetMethod, required);
				MarkMethodIfNotNull (type, property.SetMethod, required);
				return;
			}

			if (property.GetMethod != null && Array.IndexOf (accessors, "get") >= 0)
				MarkMethod (type, property.GetMethod, required);
			else if (property.GetMethod == null)
				AddUnresolveMarker (string.Format ("T: {0}' M: {1} get_{2}", type, property.PropertyType, property.Name));
			
			if (property.SetMethod != null && Array.IndexOf (accessors, "set") >= 0)
				MarkMethod (type, property.SetMethod, required);
			else if (property.SetMethod == null)
				AddUnresolveMarker (string.Format ("T: {0}' M: System.Void set_{2} ({1})", type, property.PropertyType, property.Name));
		}

		void ProcessPropertyName (TypeDefinition type, string name, string[] accessors, bool required)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties)
				if (property.Name == name)
					MarkProperty (type, property, accessors, required);
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

		static bool IsRequired (XPathNavigator nav)
		{
			string attribute = GetAttribute (nav, _required);
			if (attribute == null || attribute.Length == 0)
				return true;

			return bool.TryParse (attribute, out bool result) && result;
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

			if (accessorsValue != null)	{
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
		
		protected virtual bool IsExcluded (XPathNavigator nav)
		{
			var value = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (value))
				return false;

			return Context.IsFeatureExcluded (value);
		}


		public override string ToString ()
		{
			return "ResolveFromXmlStep: " + _xmlDocumentLocation;
		}
	}
}
