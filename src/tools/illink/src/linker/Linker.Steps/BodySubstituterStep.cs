using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : BaseStep
	{
		protected override void Process ()
		{
			var files = Context.Substitutions;
			if (files == null)
				return;

			foreach (var file in files) {
				try {
					ReadSubstitutionFile (GetSubstitutions (file));
				} catch (Exception ex) when (!(ex is XmlResolutionException)) {
					throw new XmlResolutionException ($"Failed to process XML substitution '{file}'", ex);
				}

			}
		}

		static XPathDocument GetSubstitutions (string substitutionsFile)
		{
			using (FileStream fs = File.OpenRead (substitutionsFile)) {
				return GetSubstitutions (fs);
			}
		}

		static XPathDocument GetSubstitutions (Stream substitutions)
		{
			using (StreamReader sr = new StreamReader (substitutions)) {
				return new XPathDocument (sr);
			}
		}

		void ReadSubstitutionFile (XPathDocument document)
		{
			XPathNavigator nav = document.CreateNavigator ();

			// Initial structure check
			if (!nav.MoveToChild ("linker", ""))
				return;

			// TODO: Add handling for feature

			ProcessAssemblies (nav.SelectChildren ("assembly", ""));
		}

		void ProcessAssemblies (XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				var name = GetAssemblyName (iterator.Current);

				var cache = Context.Resolver.AssemblyCache;

				if (!cache.TryGetValue (name.Name, out AssemblyDefinition assembly)) {
					Context.LogMessage (MessageImportance.Low, $"Could not match assembly '{name.FullName}' for substitution");
					continue;
				}

				ProcessAssembly (assembly, iterator);
			}
		}

		void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			ProcessTypes (assembly, iterator.Current.SelectChildren ("type", ""));
		}

		void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				string fullname = GetAttribute (nav, "fullname");

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null) {
					Context.LogMessage (MessageImportance.Low, $"Could not resolve type '{fullname}' for substitution");
					continue;
				}

				ProcessType (type, nav);
			}
		}

		void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			if (!nav.HasChildren)
				return;

			XPathNodeIterator methods = nav.SelectChildren ("method", "");
			if (methods.Count > 0)
				ProcessMethods (type, methods);

			var fields = nav.SelectChildren ("field", "");
			if (fields.Count > 0) {
				while (fields.MoveNext ())
					ProcessField (type, fields);
			}
		}

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ())
				ProcessMethod (type, iterator);
		}

		void ProcessMethod (TypeDefinition type, XPathNodeIterator iterator)
		{
			string signature = GetAttribute (iterator.Current, "signature");
			if (string.IsNullOrEmpty (signature))
				return;

			MethodDefinition method = FindMethod (type, signature);
			if (method == null) {
				Context.LogMessage (MessageImportance.Normal, $"Could not find method '{signature}' for substitution");
				return;
			}

			string action = GetAttribute (iterator.Current, "body");
			switch (action) {
			case "remove":
				Annotations.SetAction (method, MethodAction.ConvertToThrow);
				return;
			case "stub":
				string value = GetAttribute (iterator.Current, "value");
				if (value != "") {
					if (!TryConvertValue (value, method.ReturnType, out object res)) {
						Context.LogMessage (MessageImportance.High, $"Invalid value for '{signature}' stub");
						return;
					}

					Annotations.SetMethodStubValue (method, res);
				}

				Annotations.SetAction (method, MethodAction.ConvertToStub);
				return;
			default:
				Context.LogMessage (MessageImportance.High, $"Unknown body modification '{action}' for '{signature}'");
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
				Context.LogMessage (MessageImportance.Normal, $"Could not find field '{name}' for substitution.");
				return;
			}

			if (!field.IsStatic || field.IsLiteral) {
				Context.LogMessage (MessageImportance.Normal, $"Substituted field '{name}' needs to be static field.");
				return;
			}

			string value = GetAttribute (iterator.Current, "value");
			if (string.IsNullOrEmpty (value)) {
				Context.LogMessage (MessageImportance.High, $"Missing 'value' attribute for field '{field}'.");
				return;
			}
			if (!TryConvertValue (value, field.FieldType, out object res)) {
				Context.LogMessage (MessageImportance.High, $"Invalid value for '{field}': '{value}'.");
				return;
			}

			Annotations.SetFieldValue (field, res);

			string init = GetAttribute (iterator.Current, "initialize");
			if (init?.ToLowerInvariant () == "true") {
				Annotations.SetSubstitutedInit (field);
			}
		}

		static bool TryConvertValue (string value, TypeReference target, out object result)
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

				result = (int)uresult;
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

				result = (long)ulresult;
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
			}

			result = null;
			return false;
		}

		static MethodDefinition FindMethod (TypeDefinition type, string signature)
		{
			if (!type.HasMethods)
				return null;

			foreach (MethodDefinition meth in type.Methods)
				if (signature == ResolveFromXmlStep.GetMethodSignature (meth, includeGenericParameters: true))
					return meth;

			return null;
		}

		static AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetAttribute (nav, "fullname"));
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, "");
		}
	}
}
