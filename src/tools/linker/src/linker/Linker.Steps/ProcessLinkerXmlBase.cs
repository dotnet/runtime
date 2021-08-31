// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	[Flags]
	public enum AllowedAssemblies
	{
		ContainingAssembly = 0x1,
		AnyAssembly = 0x2 | ContainingAssembly,
		AllAssemblies = 0x4 | AnyAssembly
	}

	public abstract class ProcessLinkerXmlBase
	{
		const string FullNameAttributeName = "fullname";
		const string LinkerElementName = "linker";
		const string TypeElementName = "type";
		const string SignatureAttributeName = "signature";
		const string NameAttributeName = "name";
		const string FieldElementName = "field";
		const string MethodElementName = "method";
		const string EventElementName = "event";
		const string PropertyElementName = "property";
		const string AllAssembliesFullName = "*";
		protected const string XmlNamespace = "";

		protected readonly string _xmlDocumentLocation;
		readonly XPathNavigator _document;
		readonly EmbeddedResource _resource;
		protected readonly AssemblyDefinition _resourceAssembly;
		protected readonly LinkContext _context;

		protected ProcessLinkerXmlBase (LinkContext context, Stream documentStream, string xmlDocumentLocation)
		{
			_context = context;
			using (documentStream) {
				_document = XDocument.Load (documentStream, LoadOptions.SetLineInfo).CreateNavigator ();
			}
			_xmlDocumentLocation = xmlDocumentLocation;
		}

		protected ProcessLinkerXmlBase (LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation)
			: this (context, documentStream, xmlDocumentLocation)
		{
			_resource = resource ?? throw new ArgumentNullException (nameof (resource));
			_resourceAssembly = resourceAssembly ?? throw new ArgumentNullException (nameof (resourceAssembly));
		}

		protected virtual bool ShouldProcessElement (XPathNavigator nav) => FeatureSettings.ShouldProcessElement (nav, _context, _xmlDocumentLocation);

		protected virtual void ProcessXml (bool stripResource, bool ignoreResource)
		{
			if (!AllowedAssemblySelector.HasFlag (AllowedAssemblies.AnyAssembly) && _resourceAssembly == null)
				throw new InvalidOperationException ("The containing assembly must be specified for XML which is restricted to modifying that assembly only.");

			try {
				XPathNavigator nav = _document.CreateNavigator ();

				// Initial structure check - ignore XML document which don't look like linker XML format
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace))
					return;

				if (_resource != null) {
					if (stripResource)
						_context.Annotations.AddResourceToRemove (_resourceAssembly, _resource);
					if (ignoreResource)
						return;
				}

				if (!ShouldProcessElement (nav))
					return;

				ProcessAssemblies (nav.SelectChildren ("assembly", ""));

				// For embedded XML, allow not specifying the assembly explicitly in XML.
				if (_resourceAssembly != null)
					ProcessAssembly (_resourceAssembly, nav, warnOnUnresolvedTypes: true);

			} catch (Exception ex) when (!(ex is LinkerFatalErrorException)) {
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Error processing '{_xmlDocumentLocation}'", 1013), ex);
			}
		}

		protected virtual AllowedAssemblies AllowedAssemblySelector { get => _resourceAssembly != null ? AllowedAssemblies.ContainingAssembly : AllowedAssemblies.AnyAssembly; }

		protected virtual void ProcessAssemblies (XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				bool processAllAssemblies = GetFullName (iterator.Current) == AllAssembliesFullName;
				if (processAllAssemblies && AllowedAssemblySelector != AllowedAssemblies.AllAssemblies) {
					LogWarning ($"XML contains unsupported wildcard for assembly 'fullname' attribute.", 2100, iterator.Current);
					continue;
				}

				// Errors for invalid assembly names should show up even if this element will be
				// skipped due to feature conditions.
				var name = processAllAssemblies ? null : GetAssemblyName (iterator.Current);

				AssemblyDefinition assemblyToProcess = null;
				if (!AllowedAssemblySelector.HasFlag (AllowedAssemblies.AnyAssembly)) {
					if (_resourceAssembly.Name.Name != name.Name) {
						LogWarning ($"Embedded XML in assembly '{_resourceAssembly.Name.Name}' contains assembly 'fullname' attribute for another assembly '{name}'.", 2101, iterator.Current);
						continue;
					}
					assemblyToProcess = _resourceAssembly;
				}

				if (!ShouldProcessElement (iterator.Current))
					continue;

				if (processAllAssemblies) {
					// We could avoid loading all references in this case: https://github.com/mono/linker/issues/1708
					foreach (AssemblyDefinition assembly in _context.GetReferencedAssemblies ())
						ProcessAssembly (assembly, iterator.Current, warnOnUnresolvedTypes: false);
				} else {
					AssemblyDefinition assembly = assemblyToProcess ?? _context.TryResolve (name);

					if (assembly == null) {
						LogWarning ($"Could not resolve assembly '{name.Name}'.", 2007, iterator.Current);
						continue;
					}

					ProcessAssembly (assembly, iterator.Current, warnOnUnresolvedTypes: true);
				}
			}
		}

		protected abstract void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes);

		protected virtual void ProcessTypes (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			var iterator = nav.SelectChildren (TypeElementName, XmlNamespace);
			while (iterator.MoveNext ()) {
				nav = iterator.Current;

				if (!ShouldProcessElement (nav))
					continue;

				string fullname = GetFullName (nav);

				if (fullname.IndexOf ("*") != -1) {
					if (ProcessTypePattern (fullname, assembly, nav))
						continue;
				}

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null && assembly.MainModule.HasExportedTypes) {
					foreach (var exported in assembly.MainModule.ExportedTypes) {
						if (fullname == exported.FullName) {
							var resolvedExternal = ProcessExportedType (exported, assembly);
							if (resolvedExternal != null) {
								type = resolvedExternal;
								break;
							}
						}
					}
				}

				if (type == null) {
					if (warnOnUnresolvedTypes)
						LogWarning ($"Could not resolve type '{fullname}'.", 2008, nav);
					continue;
				}

				ProcessType (type, nav);
			}
		}

		protected virtual TypeDefinition ProcessExportedType (ExportedType exported, AssemblyDefinition assembly) => exported.Resolve ();

		void MatchType (TypeDefinition type, Regex regex, XPathNavigator nav)
		{
			if (regex.Match (type.FullName).Success)
				ProcessType (type, nav);

			if (!type.HasNestedTypes)
				return;

			foreach (var nt in type.NestedTypes)
				MatchType (nt, regex, nav);
		}

		protected virtual bool ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav)
		{
			Regex regex = new Regex (fullname.Replace (".", @"\.").Replace ("*", "(.*)"));

			foreach (TypeDefinition type in assembly.MainModule.Types) {
				MatchType (type, regex, nav);
			}

			if (assembly.MainModule.HasExportedTypes) {
				foreach (var exported in assembly.MainModule.ExportedTypes) {
					if (regex.Match (exported.FullName).Success) {
						var type = ProcessExportedType (exported, assembly);
						if (type != null) {
							ProcessType (type, nav);
						}
					}
				}
			}

			return true;
		}

		protected abstract void ProcessType (TypeDefinition type, XPathNavigator nav);

		protected void ProcessTypeChildren (TypeDefinition type, XPathNavigator nav, object customData = null)
		{
			if (nav.HasChildren) {
				ProcessSelectedFields (nav, type);
				ProcessSelectedMethods (nav, type, customData);
				ProcessSelectedEvents (nav, type, customData);
				ProcessSelectedProperties (nav, type, customData);
			}
		}

		void ProcessSelectedFields (XPathNavigator nav, TypeDefinition type)
		{
			XPathNodeIterator fields = nav.SelectChildren (FieldElementName, XmlNamespace);
			if (fields.Count == 0)
				return;

			while (fields.MoveNext ()) {
				if (!ShouldProcessElement (fields.Current))
					continue;
				ProcessField (type, fields.Current);
			}
		}

		protected virtual void ProcessField (TypeDefinition type, XPathNavigator nav)
		{
			string signature = GetSignature (nav);
			if (!String.IsNullOrEmpty (signature)) {
				FieldDefinition field = GetField (type, signature);
				if (field == null) {
					LogWarning ($"Could not find field '{signature}' on type '{type.GetDisplayName ()}'.", 2012, nav);
					return;
				}

				ProcessField (type, field, nav);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				bool foundMatch = false;
				if (type.HasFields) {
					foreach (FieldDefinition field in type.Fields) {
						if (field.Name == name) {
							foundMatch = true;
							ProcessField (type, field, nav);
						}
					}
				}

				if (!foundMatch) {
					LogWarning ($"Could not find field '{name}' on type '{type.GetDisplayName ()}'.", 2012, nav);
				}
			}
		}

		protected static FieldDefinition GetField (TypeDefinition type, string signature)
		{
			if (!type.HasFields)
				return null;

			foreach (FieldDefinition field in type.Fields)
				if (signature == field.FieldType.FullName + " " + field.Name)
					return field;

			return null;
		}

		protected virtual void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav) { }

		void ProcessSelectedMethods (XPathNavigator nav, TypeDefinition type, object customData)
		{
			XPathNodeIterator methods = nav.SelectChildren (MethodElementName, XmlNamespace);
			if (methods.Count == 0)
				return;

			while (methods.MoveNext ()) {
				if (!ShouldProcessElement (methods.Current))
					continue;
				ProcessMethod (type, methods.Current, customData);
			}
		}

		protected virtual void ProcessMethod (TypeDefinition type, XPathNavigator nav, object customData)
		{
			string signature = GetSignature (nav);
			if (!String.IsNullOrEmpty (signature)) {
				MethodDefinition method = GetMethod (type, signature);
				if (method == null) {
					LogWarning ($"Could not find method '{signature}' on type '{type.GetDisplayName ()}'.", 2009, nav);
					return;
				}

				ProcessMethod (type, method, nav, customData);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				bool foundMatch = false;
				if (type.HasMethods) {
					foreach (MethodDefinition method in type.Methods) {
						if (name == method.Name) {
							foundMatch = true;
							ProcessMethod (type, method, nav, customData);
						}
					}
				}

				if (!foundMatch) {
					LogWarning ($"Could not find method '{name}' on type '{type.GetDisplayName ()}'.", 2009, nav);
				}
			}
		}

		protected virtual MethodDefinition GetMethod (TypeDefinition type, string signature) => null;

		protected virtual void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData) { }

		void ProcessSelectedEvents (XPathNavigator nav, TypeDefinition type, object customData)
		{
			XPathNodeIterator events = nav.SelectChildren (EventElementName, XmlNamespace);
			if (events.Count == 0)
				return;

			while (events.MoveNext ()) {
				if (!ShouldProcessElement (events.Current))
					continue;
				ProcessEvent (type, events.Current, customData);
			}
		}

		protected virtual void ProcessEvent (TypeDefinition type, XPathNavigator nav, object customData)
		{
			string signature = GetSignature (nav);
			if (!String.IsNullOrEmpty (signature)) {
				EventDefinition @event = GetEvent (type, signature);
				if (@event == null) {
					LogWarning ($"Could not find event '{signature}' on type '{type.GetDisplayName ()}'.", 2016, nav);
					return;
				}

				ProcessEvent (type, @event, nav, customData);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				bool foundMatch = false;
				foreach (EventDefinition @event in type.Events) {
					if (@event.Name == name) {
						foundMatch = true;
						ProcessEvent (type, @event, nav, customData);
					}
				}

				if (!foundMatch) {
					LogWarning ($"Could not find event '{name}' on type '{type.GetDisplayName ()}'.", 2016, nav);
				}
			}
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

		protected virtual void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData) { }

		void ProcessSelectedProperties (XPathNavigator nav, TypeDefinition type, object customData)
		{
			XPathNodeIterator properties = nav.SelectChildren (PropertyElementName, XmlNamespace);
			if (properties.Count == 0)
				return;

			while (properties.MoveNext ()) {
				if (!ShouldProcessElement (properties.Current))
					continue;
				ProcessProperty (type, properties.Current, customData);
			}
		}

		protected virtual void ProcessProperty (TypeDefinition type, XPathNavigator nav, object customData)
		{
			string signature = GetSignature (nav);
			if (!String.IsNullOrEmpty (signature)) {
				PropertyDefinition property = GetProperty (type, signature);
				if (property == null) {
					LogWarning ($"Could not find property '{signature}' on type '{type.GetDisplayName ()}'.", 2017, nav);
					return;
				}

				ProcessProperty (type, property, nav, customData, true);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				bool foundMatch = false;
				foreach (PropertyDefinition property in type.Properties) {
					if (property.Name == name) {
						foundMatch = true;
						ProcessProperty (type, property, nav, customData, false);
					}
				}

				if (!foundMatch) {
					LogWarning ($"Could not find property '{name}' on type '{type.GetDisplayName ()}'.", 2017, nav);
				}
			}
		}

		protected static PropertyDefinition GetProperty (TypeDefinition type, string signature)
		{
			if (!type.HasProperties)
				return null;

			foreach (PropertyDefinition property in type.Properties)
				if (signature == property.PropertyType.FullName + " " + property.Name)
					return property;

			return null;
		}

		protected virtual void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object customData, bool fromSignature) { }

		protected virtual AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetFullName (nav));
		}

		protected static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, FullNameAttributeName);
		}

		protected static string GetName (XPathNavigator nav)
		{
			return GetAttribute (nav, NameAttributeName);
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, SignatureAttributeName);
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, XmlNamespace);
		}

		protected MessageOrigin GetMessageOriginForPosition (XPathNavigator position)
		{
			return (position is IXmlLineInfo lineInfo)
					? new MessageOrigin (_xmlDocumentLocation, lineInfo.LineNumber, lineInfo.LinePosition)
					: new MessageOrigin (_xmlDocumentLocation);
		}
		protected void LogWarning (string message, int warningCode, XPathNavigator position)
		{
			_context.LogWarning (message, warningCode, GetMessageOriginForPosition (position));
		}

		public override string ToString () => GetType ().Name + ": " + _xmlDocumentLocation;

		public bool TryConvertValue (string value, TypeReference target, out object result)
		{
			switch (target.MetadataType) {
			case MetadataType.Boolean:
				if (bool.TryParse (value, out bool bvalue)) {
					result = bvalue ? 1 : 0;
					return true;
				}

				goto case MetadataType.Int32;

			case MetadataType.Byte:
				if (!byte.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteresult))
					break;

				result = (int) byteresult;
				return true;

			case MetadataType.SByte:
				if (!sbyte.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteresult))
					break;

				result = (int) sbyteresult;
				return true;

			case MetadataType.Int16:
				if (!short.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortresult))
					break;

				result = (int) shortresult;
				return true;

			case MetadataType.UInt16:
				if (!ushort.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortresult))
					break;

				result = (int) ushortresult;
				return true;

			case MetadataType.Int32:
				if (!int.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iresult))
					break;

				result = iresult;
				return true;

			case MetadataType.UInt32:
				if (!uint.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uresult))
					break;

				result = (int) uresult;
				return true;

			case MetadataType.Double:
				if (!double.TryParse (value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dresult))
					break;

				result = dresult;
				return true;

			case MetadataType.Single:
				if (!float.TryParse (value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fresult))
					break;

				result = fresult;
				return true;

			case MetadataType.Int64:
				if (!long.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lresult))
					break;

				result = lresult;
				return true;

			case MetadataType.UInt64:
				if (!ulong.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulresult))
					break;

				result = (long) ulresult;
				return true;

			case MetadataType.Char:
				if (!char.TryParse (value, out char chresult))
					break;

				result = (int) chresult;
				return true;

			case MetadataType.String:
				if (value is string || value == null) {
					result = value;
					return true;
				}

				break;

			case MetadataType.ValueType:
				if (value is string &&
					_context.TryResolve (target) is TypeDefinition typeDefinition &&
					typeDefinition.IsEnum) {
					var enumField = typeDefinition.Fields.Where (f => f.IsStatic && f.Name == value).FirstOrDefault ();
					if (enumField != null) {
						result = Convert.ToInt32 (enumField.Constant);
						return true;
					}
				}

				break;
			}

			result = null;
			return false;
		}
	}
}
