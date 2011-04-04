//
// InjectSecurityAttributes.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 Novell, Inc.
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
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Tuner {

	public class InjectSecurityAttributes : BaseStep {

		enum TargetKind {
			Type,
			Method,
		}

		protected enum AttributeType {
			Critical,
			SafeCritical,
		}

		const string _safe_critical = "System.Security.SecuritySafeCriticalAttribute";
		const string _critical = "System.Security.SecurityCriticalAttribute";
		const string _system_void = "System.Void";

		const string sec_attr_folder = "secattrs";

		protected AssemblyDefinition _assembly;

		MethodDefinition _safe_critical_ctor;
		MethodDefinition _critical_ctor;
		TypeDefinition _void_type;

		string data_folder;

		protected override bool ConditionToProcess ()
		{
			if (!Context.HasParameter (sec_attr_folder)) {
				Console.Error.WriteLine ("Warning: no secattrs folder specified.");
				return false;
			}

			data_folder = Context.GetParameter (sec_attr_folder);
			return true;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			string secattr_file = Path.Combine (
				data_folder,
				assembly.Name.Name + ".secattr");

			if (!File.Exists (secattr_file)) {
				Console.Error.WriteLine ("Warning: file '{0}' not found, skipping.", secattr_file);
				return;
			}

			_assembly = assembly;

			// remove existing [SecurityCritical] and [SecuritySafeCritical]
			RemoveSecurityAttributes ();

			// add [SecurityCritical] and [SecuritySafeCritical] from the data file
			ProcessSecurityAttributeFile (secattr_file);
		}

		protected void RemoveSecurityAttributes ()
		{
			foreach (TypeDefinition type in _assembly.MainModule.Types) {
				if (RemoveSecurityAttributes (type))
					type.HasSecurity = false;

				if (type.HasMethods) {
					foreach (MethodDefinition method in type.Methods) {
						if (RemoveSecurityAttributes (method))
							method.HasSecurity = false;
					}
				}
			}
		}

		static bool RemoveSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			// also remove already existing CAS security declarations

			if (provider == null)
				return false;

			if (!provider.HasSecurityDeclarations)
				return false;

			provider.SecurityDeclarations.Clear ();
			return true;
		}

		static bool RemoveSecurityAttributes (ICustomAttributeProvider provider)
		{
			bool result = RemoveSecurityDeclarations (provider as ISecurityDeclarationProvider);

			if (!provider.HasCustomAttributes)
				return result;

			var attributes = provider.CustomAttributes;
			for (int i = 0; i < attributes.Count; i++) {
				CustomAttribute attribute = attributes [i];
				switch (attribute.Constructor.DeclaringType.FullName) {
				case _safe_critical:
				case _critical:
					attributes.RemoveAt (i--);
					break;
				}
			}
			return result;
		}

		void ProcessSecurityAttributeFile (string file)
		{
			using (StreamReader reader = File.OpenText (file)) {
				string line;
				while ((line = reader.ReadLine ()) != null)
					ProcessLine (line);
			}
		}

		void ProcessLine (string line)
		{
			if (line == null || line.Length < 6 || line [0] == '#')
				return;

			int sep = line.IndexOf (": ");
			if (sep == -1)
				return;

			string marker = line.Substring (0, sep);
			string target = line.Substring (sep + 2);

			ProcessSecurityAttributeEntry (
				DecomposeAttributeType (marker),
				DecomposeTargetKind (marker),
				target);
		}

		static AttributeType DecomposeAttributeType (string marker)
		{
			if (marker.StartsWith ("SC"))
				return AttributeType.Critical;
			else if (marker.StartsWith ("SSC"))
				return AttributeType.SafeCritical;
			else
				throw new ArgumentException ();
		}

		static TargetKind DecomposeTargetKind (string marker)
		{
			switch (marker [marker.Length - 1]) {
			case 'T':
				return TargetKind.Type;
			case 'M':
				return TargetKind.Method;
			default:
				throw new ArgumentException ();
			}
		}

		public static bool NeedsDefaultConstructor (TypeDefinition type)
		{
			if (type.IsInterface)
				return false;

			TypeReference base_type = type.BaseType;
			if ((base_type == null) || (base_type.Namespace != "System"))
				return true;

			return ((base_type.Name != "Delegate") && (base_type.Name != "MulticastDelegate"));
		}

		void ProcessSecurityAttributeEntry (AttributeType type, TargetKind kind, string target)
		{
			ICustomAttributeProvider provider = GetTarget (kind, target);
			if (provider == null) {
				Console.Error.WriteLine ("Warning: entry '{0}' could not be found", target);
				return;
			}

			// we need to be smarter when applying the attributes (mostly SC) to types
			if (kind == TargetKind.Type) {
				TypeDefinition td = (provider as TypeDefinition);
				// ensure [SecurityCritical] types (well most) have a default constructor
				if ((type == AttributeType.Critical) && NeedsDefaultConstructor (td)) {
					if (GetDefaultConstructor (td) == null) {
						// Console.Error.WriteLine ("Info: adding default ctor for '{0}'", td);
						td.Methods.Add (CreateDefaultConstructor ());
					}
				}

				// it's easier for some tools (e.g. less false positives in fxcop) 
				// and also quicker for the runtime (one less lookup) if all methods gets decorated
				foreach (MethodDefinition method in td.Methods) {
					bool skip = false;

					AttributeType mtype = type;
					// there are cases where an SC cannot be applied to some methods
					switch (method.Name) {
					// e.g. everything we override from System.Object (which is transparent)
					case "Equals":
						skip = method.Parameters.Count == 1 && method.Parameters [0].ParameterType.FullName == "System.Object";
						break;
					case "Finalize":
					case "GetHashCode":
					case "ToString":
						skip = !method.HasParameters;
						break;
					// e.g. some transparent interfaces, like IDisposable (implicit or explicit)
					// downgrade some SC into SSC to respect the override/inheritance rules
					case "System.IDisposable.Dispose":
					case "Dispose":
						skip = !method.HasParameters;
						break;
					}

					if (skip)
						continue;

					switch (mtype) {
					case AttributeType.Critical:
						AddCriticalAttribute (method);
						break;
					case AttributeType.SafeCritical:
						AddSafeCriticalAttribute (method);
						break;
					}
				}
			}

			switch (type) {
			case AttributeType.Critical:
				AddCriticalAttribute (provider);
				break;
			case AttributeType.SafeCritical:
				AddSafeCriticalAttribute (provider);
				break;
			}
		}

		protected void AddCriticalAttribute (ICustomAttributeProvider provider)
		{
			// a [SecurityCritical] replaces a [SecuritySafeCritical]
			if (HasSecurityAttribute (provider, AttributeType.SafeCritical))
				RemoveSecurityAttributes (provider);

			AddSecurityAttribute (provider, AttributeType.Critical);
		}

		void AddSafeCriticalAttribute (ICustomAttributeProvider provider)
		{
			// a [SecuritySafeCritical] is ignored if a [SecurityCritical] is present
			if (HasSecurityAttribute (provider, AttributeType.Critical))
				return;

			AddSecurityAttribute (provider, AttributeType.SafeCritical);
		}

		void AddSecurityAttribute (ICustomAttributeProvider provider, AttributeType type)
		{
			if (HasSecurityAttribute (provider, type))
				return;

			var attributes = provider.CustomAttributes;
			switch (type) {
			case AttributeType.Critical:
				attributes.Add (CreateCriticalAttribute ());
				break;
			case AttributeType.SafeCritical:
				attributes.Add (CreateSafeCriticalAttribute ());
				break;
			}
		}

		protected static bool HasSecurityAttribute (ICustomAttributeProvider provider, AttributeType type)
		{
			if (!provider.HasCustomAttributes)
				return false;

			foreach (CustomAttribute attribute in provider.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.Name) {
				case _critical:
					if (type == AttributeType.Critical)
						return true;

					break;
				case _safe_critical:
					if (type == AttributeType.SafeCritical)
						return true;

					break;
				}
			}

			return false;
		}

		ICustomAttributeProvider GetTarget (TargetKind kind, string target)
		{
			switch (kind) {
			case TargetKind.Type:
				return GetType (target);
			case TargetKind.Method:
				return GetMethod (target);
			default:
				throw new ArgumentException ();
			}
		}

		TypeDefinition GetType (string fullname)
		{
			return _assembly.MainModule.GetType (fullname);
		}

		MethodDefinition GetMethod (string signature)
		{
			int pos = signature.IndexOf (" ");
			if (pos == -1)
				throw new ArgumentException ();

			string tmp = signature.Substring (pos + 1);

			pos = tmp.IndexOf ("::");
			if (pos == -1)
				throw new ArgumentException ();

			string type_name = tmp.Substring (0, pos);

			int parpos = tmp.IndexOf ("(");
			if (parpos == -1)
				throw new ArgumentException ();

			string method_name = tmp.Substring (pos + 2, parpos - pos - 2);

			TypeDefinition type = GetType (type_name);
			if (type == null)
				return null;

			return GetMethod (type.Methods, signature);
		}

		static MethodDefinition GetMethod (IEnumerable methods, string signature)
		{
			foreach (MethodDefinition method in methods)
				if (GetFullName (method) == signature)
					return method;

			return null;
		}

		static string GetFullName (MethodReference method)
		{
			var sentinel = method.Parameters.FirstOrDefault (p => p.ParameterType.IsSentinel);
			var sentinel_pos = -1;
			if (sentinel != null)
				sentinel_pos = method.Parameters.IndexOf (sentinel);

			StringBuilder sb = new StringBuilder ();
			sb.Append (method.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (method.DeclaringType.FullName);
			sb.Append ("::");
			sb.Append (method.Name);
			if (method.HasGenericParameters) {
				sb.Append ("<");
				for (int i = 0; i < method.GenericParameters.Count; i++ ) {
					if (i > 0)
						sb.Append (",");
					sb.Append (method.GenericParameters [i].Name);
				}
				sb.Append (">");
			}
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					if (i == sentinel_pos)
						sb.Append ("...,");

					sb.Append (method.Parameters [i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		static MethodDefinition GetDefaultConstructor (TypeDefinition type)
		{
			foreach (MethodDefinition ctor in type.Methods.Where (m => m.IsConstructor))
				if (!ctor.IsStatic && !ctor.HasParameters)
					return ctor;

			return null;
		}

		MethodDefinition GetSafeCriticalCtor ()
		{
			if (_safe_critical_ctor != null)
				return _safe_critical_ctor;

			TypeDefinition safe_critical_type = Context.GetType (_safe_critical);
			if (safe_critical_type == null)
				throw new InvalidOperationException (String.Format ("{0} type not found", _safe_critical));

			_safe_critical_ctor = GetDefaultConstructor (safe_critical_type);
			return _safe_critical_ctor;
		}

		MethodDefinition GetCriticalCtor ()
		{
			if (_critical_ctor != null)
				return _critical_ctor;

			TypeDefinition critical_type = Context.GetType (_critical);
			if (critical_type == null)
				throw new InvalidOperationException (String.Format ("{0} type not found", _critical));

			_critical_ctor = GetDefaultConstructor (critical_type);
			return _critical_ctor;
		}

		TypeDefinition GetSystemVoid ()
		{
			if (_void_type != null)
				return _void_type;

			_void_type = Context.GetType (_system_void);
			if (_void_type == null)
				throw new InvalidOperationException (String.Format ("{0} type not found", _system_void));

			return _void_type;
		}

		MethodReference Import (MethodDefinition method)
		{
			return _assembly.MainModule.Import (method);
		}

		CustomAttribute CreateSafeCriticalAttribute ()
		{
			return new CustomAttribute (Import (GetSafeCriticalCtor ()));
		}

		CustomAttribute CreateCriticalAttribute ()
		{
			return new CustomAttribute (Import (GetCriticalCtor ()));
		}

		MethodDefinition CreateDefaultConstructor ()
		{
			MethodDefinition method = new MethodDefinition (".ctor", 
				MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, 
				GetSystemVoid ());
			method.Body.Instructions.Add (Instruction.Create (OpCodes.Ret));
			return method;
		}
	}
}
