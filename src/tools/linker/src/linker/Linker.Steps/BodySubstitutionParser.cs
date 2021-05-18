using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class BodySubstitutionParser : ProcessLinkerXmlBase
	{
		SubstitutionInfo _substitutionInfo;

		public BodySubstitutionParser (LinkContext context, Stream documentStream, string xmlDocumentLocation)
			: base (context, documentStream, xmlDocumentLocation)
		{
		}

		public BodySubstitutionParser (LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "")
			: base (context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Parse (SubstitutionInfo xmlInfo)
		{
			_substitutionInfo = xmlInfo;
			bool stripSubstitutions = _context.IsOptimizationEnabled (CodeOptimizations.RemoveSubstitutions, _resourceAssembly);
			ProcessXml (stripSubstitutions, _context.IgnoreSubstitutions);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
			ProcessResources (assembly, nav.SelectChildren ("resource", ""));
		}

		protected override TypeDefinition ProcessExportedType (ExportedType exported, AssemblyDefinition assembly) => null;

		protected override bool ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav) => false;

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			if (!nav.HasChildren)
				return;

			XPathNodeIterator methods = nav.SelectChildren ("method", "");
			if (methods.Count > 0)
				ProcessMethods (type, methods);

			var fields = nav.SelectChildren ("field", "");
			if (fields.Count > 0) {
				while (fields.MoveNext ()) {
					if (!ShouldProcessElement (fields.Current))
						continue;

					ProcessField (type, fields);
				}
			}
		}

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					continue;

				ProcessMethod (type, iterator);
			}
		}

		void ProcessMethod (TypeDefinition type, XPathNodeIterator iterator)
		{
			string signature = GetAttribute (iterator.Current, "signature");
			if (string.IsNullOrEmpty (signature))
				return;

			MethodDefinition method = FindMethod (type, signature);
			if (method == null) {
				LogWarning ($"Could not find method '{signature}' on type '{type.GetDisplayName ()}'", 2009, iterator.Current);
				return;
			}

			string action = GetAttribute (iterator.Current, "body");
			switch (action) {
			case "remove":
				_substitutionInfo.SetMethodAction (method, MethodAction.ConvertToThrow);
				return;
			case "stub":
				string value = GetAttribute (iterator.Current, "value");
				if (!string.IsNullOrEmpty (value)) {
					if (!TryConvertValue (value, method.ReturnType, out object res)) {
						LogWarning ($"Invalid value for '{method.GetDisplayName ()}' stub", 2010, iterator.Current);
						return;
					}

					_substitutionInfo.SetMethodStubValue (method, res);
				}

				_substitutionInfo.SetMethodAction (method, MethodAction.ConvertToStub);
				return;
			default:
				LogWarning ($"Unknown body modification '{action}' for '{method.GetDisplayName ()}'", 2011, iterator.Current);
				return;
			}
		}

		void ProcessField (TypeDefinition type, XPathNodeIterator iterator)
		{
			string name = GetAttribute (iterator.Current, "name");
			if (string.IsNullOrEmpty (name))
				return;

			var field = type.Fields.FirstOrDefault (f => f.Name == name);
			if (field == null) {
				LogWarning ($"Could not find field '{name}' on type '{type.GetDisplayName ()}'", 2012, iterator.Current);
				return;
			}

			if (!field.IsStatic || field.IsLiteral) {
				LogWarning ($"Substituted field '{field.GetDisplayName ()}' needs to be static field.", 2013, iterator.Current);
				return;
			}

			string value = GetAttribute (iterator.Current, "value");
			if (string.IsNullOrEmpty (value)) {
				LogWarning ($"Missing 'value' attribute for field '{field.GetDisplayName ()}'.", 2014, iterator.Current);
				return;
			}
			if (!TryConvertValue (value, field.FieldType, out object res)) {
				LogWarning ($"Invalid value '{value}' for '{field.GetDisplayName ()}'.", 2015, iterator.Current);
				return;
			}

			_substitutionInfo.SetFieldValue (field, res);

			string init = GetAttribute (iterator.Current, "initialize");
			if (init?.ToLowerInvariant () == "true") {
				_substitutionInfo.SetFieldInit (field);
			}
		}

		void ProcessResources (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				if (!ShouldProcessElement (nav))
					continue;

				string name = GetAttribute (nav, "name");
				if (String.IsNullOrEmpty (name)) {
					LogWarning ($"Missing 'name' attribute for resource.", 2038, iterator.Current);
					continue;
				}

				string action = GetAttribute (nav, "action");
				if (action != "remove") {
					LogWarning ($"Invalid value '{action}' for attribute 'action' for resource '{name}'.", 2039, iterator.Current);
					continue;
				}

				EmbeddedResource resource = assembly.FindEmbeddedResource (name);
				if (resource == null) {
					LogWarning ($"Could not find embedded resource '{name}' to remove in assembly '{assembly.Name.Name}'.", 2040, iterator.Current);
					continue;
				}

				_context.Annotations.AddResourceToRemove (assembly, resource);
			}
		}

		static MethodDefinition FindMethod (TypeDefinition type, string signature)
		{
			if (!type.HasMethods)
				return null;

			foreach (MethodDefinition meth in type.Methods)
				if (signature == DescriptorMarker.GetMethodSignature (meth, includeGenericParameters: true))
					return meth;

			return null;
		}
	}
}
