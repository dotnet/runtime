using System;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class ProcessLinkerXmlStepBase : LoadReferencesStep
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
		readonly XPathDocument _document;
		readonly EmbeddedResource _resource;
		readonly AssemblyDefinition _resourceAssembly;

		protected ProcessLinkerXmlStepBase (XPathDocument document, string xmlDocumentLocation)
		{
			_document = document;
			_xmlDocumentLocation = xmlDocumentLocation;
		}

		protected ProcessLinkerXmlStepBase (XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation)
			: this (document, xmlDocumentLocation)
		{
			_resource = resource ?? throw new ArgumentNullException (nameof (resource));
			_resourceAssembly = resourceAssembly ?? throw new ArgumentNullException (nameof (resourceAssembly));
		}

		protected virtual bool ShouldProcessElement (XPathNavigator nav) => FeatureSettings.ShouldProcessElement (nav, Context, _xmlDocumentLocation);

		protected virtual void ProcessXml (bool stripResource, bool ignoreResource)
		{
			try {
				XPathNavigator nav = _document.CreateNavigator ();

				// Initial structure check - ignore XML document which don't look like linker XML format
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace))
					return;

				if (_resource != null) {
					if (stripResource)
						Context.Annotations.AddResourceToRemove (_resourceAssembly, _resource);
					if (ignoreResource)
						return;
				}

				if (!ShouldProcessElement (nav))
					return;

				ProcessAssemblies (nav.SelectChildren ("assembly", ""));

			} catch (Exception ex) when (!(ex is LinkerFatalErrorException)) {
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Error processing '{_xmlDocumentLocation}'", 1013), ex);
			}
		}

		protected virtual bool AllowAllAssembliesSelector { get => false; }

		protected virtual void ProcessAssemblies (XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				bool processAllAssemblies = AllowAllAssembliesSelector && GetFullName (iterator.Current) == AllAssembliesFullName;

				// Errors for invalid assembly names should show up even if this element will be
				// skipped due to feature conditions.
				var name = processAllAssemblies ? null : GetAssemblyName (iterator.Current);

				if (!ShouldProcessElement (iterator.Current))
					continue;

				if (processAllAssemblies) {
					foreach (AssemblyDefinition assembly in Context.GetAssemblies ())
						ProcessAssembly (assembly, iterator, warnOnUnresolvedTypes: false);
				} else {
					AssemblyDefinition assembly = GetAssembly (Context, name);

					if (assembly == null) {
						Context.LogWarning ($"Could not resolve assembly '{GetAssemblyName (iterator.Current).Name}' specified in {_xmlDocumentLocation}", 2007, _xmlDocumentLocation);
						continue;
					}

					ProcessAssembly (assembly, iterator, warnOnUnresolvedTypes: true);
				}
			}
		}

		protected abstract void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator, bool warnOnUnresolvedTypes);

		protected virtual void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator, bool warnOnUnresolvedTypes)
		{
			iterator = iterator.Current.SelectChildren (TypeElementName, XmlNamespace);
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

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
						Context.LogWarning ($"Could not resolve type '{fullname}' specified in {_xmlDocumentLocation}", 2008, _xmlDocumentLocation);
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
					Context.LogWarning ($"Could not find field '{signature}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2012, _xmlDocumentLocation);
					return;
				}

				ProcessField (type, field, nav);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				if (!type.HasFields)
					return;

				bool foundMatch = false;
				foreach (FieldDefinition field in type.Fields) {
					if (field.Name == name) {
						foundMatch = true;
						ProcessField (type, field, nav);
					}
				}

				if (!foundMatch) {
					Context.LogWarning ($"Could not find field '{name}' in type '{type.GetDisplayName ()}' specified in { _xmlDocumentLocation}", 2012, _xmlDocumentLocation);
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
					Context.LogWarning ($"Could not find method '{signature}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2009, _xmlDocumentLocation);
					return;
				}

				ProcessMethod (type, method, nav, customData);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				if (!type.HasMethods)
					return;

				bool foundMatch = false;
				foreach (MethodDefinition method in type.Methods) {
					if (name == method.Name) {
						foundMatch = true;
						ProcessMethod (type, method, nav, customData);
					}
				}

				if (!foundMatch) {
					Context.LogWarning ($"Could not find method '{name}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2009, _xmlDocumentLocation);
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
					Context.LogWarning ($"Could not find event '{signature}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2016, _xmlDocumentLocation);
					return;
				}

				ProcessEvent (type, @event, nav, customData);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				if (!type.HasEvents)
					return;

				bool foundMatch = false;
				foreach (EventDefinition @event in type.Events) {
					if (@event.Name == name) {
						foundMatch = true;
						ProcessEvent (type, @event, nav, customData);
					}
				}

				if (!foundMatch) {
					Context.LogWarning ($"Could not find event '{name}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2016, _xmlDocumentLocation);
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
					Context.LogWarning ($"Could not find property '{signature}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2017, _xmlDocumentLocation);
					return;
				}

				ProcessProperty (type, property, nav, customData, true);
			}

			string name = GetAttribute (nav, NameAttributeName);
			if (!String.IsNullOrEmpty (name)) {
				if (!type.HasProperties)
					return;

				bool foundMatch = false;
				foreach (PropertyDefinition property in type.Properties) {
					if (property.Name == name) {
						foundMatch = true;
						ProcessProperty (type, property, nav, customData, false);
					}
				}

				if (!foundMatch) {
					Context.LogWarning ($"Could not find property '{name}' in type '{type.GetDisplayName ()}' specified in {_xmlDocumentLocation}", 2017, _xmlDocumentLocation);
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

		protected abstract AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName);

		protected virtual AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetFullName (nav));
		}

		protected static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, FullNameAttributeName);
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, SignatureAttributeName);
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, XmlNamespace);
		}

		public override string ToString () => GetType ().Name + ": " + _xmlDocumentLocation;
	}
}
