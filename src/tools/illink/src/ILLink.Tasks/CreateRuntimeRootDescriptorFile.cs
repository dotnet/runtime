using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Microsoft.Build.Utilities; // Task
using Microsoft.Build.Framework; // ITaskItem

namespace ILLink.Tasks
{
	public class CreateRuntimeRootILLinkDescriptorFile : Task
	{
		/// <summary>
		///   The path to namespace.h.
		/// </summary>
		[Required]
		public ITaskItem NamespaceFilePath { get; set; }

		/// <summary>
		///   The path to mscorlib.h.
		/// </summary>
		[Required]
		public ITaskItem MscorlibFilePath { get; set; }

		/// <summary>
		///   The path to cortypeinfo.h.
		/// </summary>
		[Required]
		public ITaskItem CortypeFilePath { get; set; }

		/// <summary>
		///   The path to rexcep.h.
		/// </summary>
		[Required]
		public ITaskItem RexcepFilePath { get; set; }

		/// <summary>
		///   The path to ILLinkTrim.xml.
		/// </summary>
		[Required]
		public ITaskItem ILLinkTrimXmlFilePath { get; set; }

		/// <summary>
		///   The path to the file to generate.
		/// </summary>
		[Required]
		public ITaskItem RuntimeRootDescriptorFilePath { get; set; }

		class ClassMembers
		{
			public bool keepAllFields;
			public HashSet<string> methods;
			public HashSet<string> fields;
		}

		Dictionary<string, string> namespaceDictionary = new Dictionary<string, string> ();
		Dictionary<string, string> classIdsToClassNames = new Dictionary<string, string> ();
		Dictionary<string, ClassMembers> classNamesToClassMembers = new Dictionary<string, ClassMembers> ();

		public override bool Execute ()
		{
			var namespaceFilePath = NamespaceFilePath.ItemSpec;
			if (!File.Exists (namespaceFilePath)) {
				Log.LogError ("File " + namespaceFilePath + " doesn't exist.");
				return false;
			}

			var mscorlibFilePath = MscorlibFilePath.ItemSpec;
			if (!File.Exists (mscorlibFilePath)) {
				Log.LogError ("File " + mscorlibFilePath + " doesn't exist.");
				return false;
			}

			var cortypeFilePath = CortypeFilePath.ItemSpec;
			if (!File.Exists (cortypeFilePath)) {
				Log.LogError ("File " + cortypeFilePath + " doesn't exist.");
				return false;
			}

			var rexcepFilePath = RexcepFilePath.ItemSpec;
			if (!File.Exists (rexcepFilePath)) {
				Log.LogError ("File " + rexcepFilePath + " doesn't exist.");
				return false;
			}

			var iLLinkTrimXmlFilePath = ILLinkTrimXmlFilePath.ItemSpec;
			if (!File.Exists (iLLinkTrimXmlFilePath)) {
				Log.LogError ("File " + iLLinkTrimXmlFilePath + " doesn't exist.");
				return false;
			}

			ProcessNamespaces (namespaceFilePath);

			ProcessMscorlib (mscorlibFilePath);

			ProcessCoreTypes (cortypeFilePath);

			ProcessExceptionTypes (rexcepFilePath);

			OutputXml (iLLinkTrimXmlFilePath, RuntimeRootDescriptorFilePath.ItemSpec);

			return true;
		}

		void ProcessNamespaces (string namespaceFile)
		{
			string [] namespaces = File.ReadAllLines (namespaceFile);

			// Process definitions of the form
			// #define g_SystemNS          "System"
			// from namespace.h
			foreach (string namespaceDef in namespaces) {
				if (namespaceDef.StartsWith ("#define")) {
					char [] separators = { '"', ' ', '\t' };
					string [] namespaceDefElements = namespaceDef.Split (separators, StringSplitOptions.RemoveEmptyEntries);
					int startIndex = "g_".Length;
					// E.g., if namespaceDefElements [1] is "g_RuntimeNS", lhs is "Runtime".
					string lhs = namespaceDefElements [1].Substring (startIndex, namespaceDefElements [1].LastIndexOf ('N') - startIndex);
					if (namespaceDefElements.Length == 3) {
						// E.G., #define g_SystemNS          "System"
						// "System" --> "System"
						namespaceDictionary [lhs] = namespaceDefElements [2];
					}
					else {
						// E.g., #define g_RuntimeNS         g_SystemNS ".Runtime"
						// "Runtime" --> "System.Runtime"
						string prefix = namespaceDefElements [2].Substring (startIndex, namespaceDefElements [2].LastIndexOf ('N') - startIndex);
						namespaceDictionary [lhs] = namespaceDictionary [prefix] + namespaceDefElements [3];
					}
				}
			}
		}

		void ProcessMscorlib (string typeFile)
		{
			string [] types = File.ReadAllLines (typeFile);
			string classId = null;

			foreach (string def in types) {
				string [] defElements = null;
				if (def.StartsWith ("DEFINE_") || def.StartsWith ("// DEFINE_")) {
					char [] separators = { ',', '(', ')', ' ', '\t', '/' };
					defElements = def.Split (separators, StringSplitOptions.RemoveEmptyEntries);
				}

				if (def.StartsWith ("DEFINE_CLASS(") || def.StartsWith ("// DEFINE_CLASS(")) {
					// E.g., DEFINE_CLASS(APP_DOMAIN,            System,                 AppDomain)
					classId = defElements [1];               // APP_DOMAIN
					string classNamespace = defElements [2]; // System
					string className = defElements [3];      // AppDomain
					AddClass (classNamespace, className, classId);
				}
				else if (def.StartsWith ("DEFINE_CLASS_U(")) {
					// E.g., DEFINE_CLASS_U(System,                 AppDomain,      AppDomainBaseObject)
					string classNamespace = defElements [1]; // System
					string className = defElements [2];      // AppDomain
					classId = defElements [3];               // AppDomainBaseObject
															 // For these classes the sizes of managed and unmanaged classes and field offsets
															 // are compared so we need to preserve all fields.
					const bool keepAllFields = true;
					AddClass (classNamespace, className, classId, keepAllFields);
				}
				else if (def.StartsWith ("DEFINE_FIELD(")) {
					// E.g., DEFINE_FIELD(ACCESS_VIOLATION_EXCEPTION, IP,                _ip)
					classId = defElements [1];          // ACCESS_VIOLATION_EXCEPTION
					string fieldName = defElements [3]; // _ip
					AddField (fieldName, classId);
				}
				else if (def.StartsWith ("DEFINE_METHOD(")) {
					// E.g., DEFINE_METHOD(APP_DOMAIN,           ON_ASSEMBLY_LOAD,       OnAssemblyLoadEvent,        IM_Assembly_RetVoid)
					string methodName = defElements [3]; // OnAssemblyLoadEvent
					classId = defElements [1];           // APP_DOMAIN
					AddMethod (methodName, classId);
				}
				else if (def.StartsWith ("DEFINE_PROPERTY(") || def.StartsWith ("DEFINE_STATIC_PROPERTY(")) {
					// E.g., DEFINE_PROPERTY(ARRAY,              LENGTH,                 Length,                     Int)
					// or    DEFINE_STATIC_PROPERTY(THREAD,      CURRENT_THREAD,         CurrentThread,              Thread)
					string propertyName = defElements [3];          // Length or CurrentThread
					classId = defElements [1];                      // ARRAY or THREAD
					AddMethod ("get_" + propertyName, classId);
				}
				else if (def.StartsWith ("DEFINE_SET_PROPERTY(")) {
					// E.g., DEFINE_SET_PROPERTY(THREAD,         UI_CULTURE,             CurrentUICulture,           CultureInfo)
					string propertyName = defElements [3]; // CurrentUICulture
					classId = defElements [1];             // THREAD
					AddMethod ("get_" + propertyName, classId);
					AddMethod ("set_" + propertyName, classId);
				}
			}
		}

		public void ProcessCoreTypes (string corTypeFile)
		{
			string [] corTypes = File.ReadAllLines (corTypeFile);

			foreach (string def in corTypes) {
				// E.g., TYPEINFO(ELEMENT_TYPE_VOID,         "System", "Void",          0,              TYPE_GC_NONE,   false,  true,   false,  false,  false) // 0x01
				if (def.StartsWith ("TYPEINFO(")) {
					char [] separators = { ',', '(', ')', '"', ' ', '\t' };
					string [] defElements = def.Split (separators, StringSplitOptions.RemoveEmptyEntries);
					string classId = null;
					string classNamespace = defElements [2]; // System
					string className = defElements [3];      // Void
					AddClass (classNamespace, className, classId);
				}
			}
		}

		public void ProcessExceptionTypes (string excTypeFile)
		{
			string [] excTypes = File.ReadAllLines (excTypeFile);

			foreach (string def in excTypes) {
				// E.g., DEFINE_EXCEPTION(g_InteropNS,          MarshalDirectiveException,      false,  COR_E_MARSHALDIRECTIVE)
				if (def.StartsWith ("DEFINE_EXCEPTION(")) {
					char [] separators = { ',', '(', ')', ' ', '\t' };
					string [] defElements = def.Split (separators, StringSplitOptions.RemoveEmptyEntries);
					string classId = null;
					string classNamespace = defElements [1]; // g_InteropNS
					string className = defElements [2];      // MarshalDirectiveException
					AddClass (classNamespace, className, classId);
					AddMethod (".ctor", classId, classNamespace, className);
				}
			}
		}

		void OutputXml (string iLLinkTrimXmlFilePath, string outputFileName)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (iLLinkTrimXmlFilePath);
			XmlNode linkerNode = doc ["linker"];
			XmlNode assemblyNode = linkerNode ["assembly"];

			foreach (string typeName in classNamesToClassMembers.Keys) {
				XmlNode typeNode = doc.CreateElement ("type");
				XmlAttribute typeFullName = doc.CreateAttribute ("fullname");
				typeFullName.Value = typeName;
				typeNode.Attributes.Append (typeFullName);

				ClassMembers members = classNamesToClassMembers [typeName];

				// We need to keep everyting in System.Runtime.InteropServices.WindowsRuntime and
				// System.Threading.Volatile.
				if (!typeName.StartsWith ("System.Runtime.InteropServices.WindowsRuntime") &&
					!typeName.StartsWith ("System.Threading.Volatile")) {
					if (members.keepAllFields) {
						XmlAttribute preserve = doc.CreateAttribute ("preserve");
						preserve.Value = "fields";
						typeNode.Attributes.Append (preserve);
					}
					else if ((members.fields == null) && (members.methods == null)) {
						XmlAttribute preserve = doc.CreateAttribute ("preserve");
						preserve.Value = "nothing";
						typeNode.Attributes.Append (preserve);
					}

					if (!members.keepAllFields && (members.fields != null)) {
						foreach (string field in members.fields) {
							XmlNode fieldNode = doc.CreateElement ("field");
							XmlAttribute fieldName = doc.CreateAttribute ("name");
							fieldName.Value = field;
							fieldNode.Attributes.Append (fieldName);
							typeNode.AppendChild (fieldNode);
						}
					}

					if (members.methods != null) {
						foreach (string method in members.methods) {
							XmlNode methodNode = doc.CreateElement ("method");
							XmlAttribute methodName = doc.CreateAttribute ("name");
							methodName.Value = method;
							methodNode.Attributes.Append (methodName);
							typeNode.AppendChild (methodNode);
						}
					}
				}
				assemblyNode.AppendChild (typeNode);
			}
			doc.Save (outputFileName);
		}

		void AddClass (string classNamespace, string className, string classId, bool keepAllFields = false)
		{
			string fullClassName = GetFullClassName (classNamespace, className);
			if (fullClassName != null) {
				if ((classId != null) && (classId != "NoClass")) {
					classIdsToClassNames [classId] = fullClassName;
				}
				ClassMembers members;
				if (!classNamesToClassMembers.TryGetValue (fullClassName, out members)) {
					members = new ClassMembers ();
					classNamesToClassMembers [fullClassName] = members;
				}
				members.keepAllFields |= keepAllFields;
			}
		}

		void AddField (string fieldName, string classId)
		{
			string className = classIdsToClassNames [classId];

			ClassMembers members = classNamesToClassMembers [className];

			if (members.fields == null) {
				members.fields = new HashSet<string> ();
			}
			members.fields.Add (fieldName);
		}

		void AddMethod (string methodName, string classId, string classNamespace = null, string className = null)
		{
			string fullClassName;
			if (classId != null) {
				fullClassName = classIdsToClassNames [classId];
			}
			else {
				fullClassName = GetFullClassName (classNamespace, className);
			}

			ClassMembers members = classNamesToClassMembers [fullClassName];

			if (members.methods == null) {
				members.methods = new HashSet<string> ();
			}
			members.methods.Add (methodName);
		}

		string GetFullClassName (string classNamespace, string className)
		{
			string prefixToRemove = "g_";
			if (classNamespace.StartsWith (prefixToRemove)) {
				classNamespace = classNamespace.Substring (prefixToRemove.Length);
			}
			string suffixToRemove = "NS";
			if (classNamespace.EndsWith (suffixToRemove)) {
				classNamespace = classNamespace.Substring (0, classNamespace.Length - suffixToRemove.Length);
			}

			if ((classNamespace == "NULL") && (className == "NULL")) {
				return null;
			}

			if (!namespaceDictionary.ContainsKey (classNamespace)) {
				Log.LogError ("Unknown namespace: " + classNamespace);
			}

			return namespaceDictionary [classNamespace] + "." + className;
		}
	}
}
