using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Tuner;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MonoMac.Tuner {

	public class MethodMapInjection : BaseStep {

		struct ExportedMethod {
			public readonly CustomAttribute attribute;
			public readonly MethodDefinition method;

			public ExportedMethod (CustomAttribute attribute, MethodDefinition method)
			{
				this.attribute = attribute;
				this.method = method;
			}
		}

		ModuleDefinition module;

		bool imported;
		TypeReference void_type;
		TypeReference dictionary_intptr_methoddesc;
		MethodReference dictionary_intptr_methoddesc_ctor;
		MethodReference dictionary_intptr_methoddesc_set_item;
		MethodReference methoddesc_ctor;
		MethodReference selector_get_handle;
		MethodReference methodbase_get_method_from_handle;
		MethodReference class_register_methods;
		MethodReference type_get_type_from_handle;
		FieldReference selector_init;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			module = assembly.MainModule;

			foreach (TypeDefinition type in module.GetAllTypes ()) {
				if (!type.IsNSObject ())
					continue;

				ProcessNSObject (type);
			}

			imported = false;
		}

		void PrepareImports ()
		{
			if (imported)
				return;

			var corlib = Context.GetAssembly ("mscorlib");

			void_type = Import (corlib, "System.Void");

			var monomac = Context.GetAssembly ("MonoMac");

			var dictionary = Import (corlib, "System.Collections.Generic.Dictionary`2");

			var intptr = Import (corlib, "System.IntPtr");
			var method_desc = Import (monomac, "MonoMac.ObjCRuntime.MethodDescription");

			var dic_i_md = new GenericInstanceType (dictionary);
			dic_i_md.GenericArguments.Add (intptr);
			dic_i_md.GenericArguments.Add (method_desc);
			dictionary_intptr_methoddesc = dic_i_md;

			dictionary_intptr_methoddesc_ctor = Import (".ctor", dic_i_md, false, void_type, Import (corlib, "System.Int32"));

			dictionary_intptr_methoddesc_set_item = Import ("set_Item", dic_i_md, false, void_type,
				dictionary.GenericParameters [0],
				dictionary.GenericParameters [1]);

			methoddesc_ctor = Import (".ctor", method_desc, false, void_type,
				Import (corlib, "System.Reflection.MethodBase"),
				Import (monomac, "MonoMac.ObjCRuntime.ArgumentSemantic"));

			var selector = Import (monomac, "MonoMac.ObjCRuntime.Selector");

			selector_get_handle = Import ("GetHandle", selector, true, intptr, Import (corlib, "System.String"));
			selector_init = new FieldReference ("Init", selector, intptr);

			var methodbase = Import (corlib, "System.Reflection.MethodBase");

			methodbase_get_method_from_handle = Import ("GetMethodFromHandle", methodbase, true, methodbase, Import (corlib, "System.RuntimeMethodHandle"));

			var type = Import (corlib, "System.Type");

			type_get_type_from_handle = Import ("GetTypeFromHandle", type, true, type, Import (corlib, "System.RuntimeTypeHandle"));

			var @class = Import (monomac, "MonoMac.ObjCRuntime.Class");

			class_register_methods = Import ("RegisterMethods", @class, true, void_type, type, dic_i_md);

			imported = true;
		}

		MethodReference Import (string name, TypeReference declaring_type, bool @static, TypeReference return_type, params TypeReference [] parameters_type)
		{
			var reference = new MethodReference (name, return_type, declaring_type) {
				HasThis = !@static,
				ExplicitThis = false,
				CallingConvention = MethodCallingConvention.Default,
			};

			foreach (var parameter_type in parameters_type)
				reference.Parameters.Add (new ParameterDefinition (parameter_type));

			return reference;
		}

		TypeReference Import (TypeReference type)
		{
			return module.Import (type);
		}

		TypeReference Import (AssemblyDefinition assembly, string type_name)
		{
			return Import (assembly.MainModule.GetType (type_name));
		}

		void ProcessNSObject (TypeDefinition type)
		{
			var exported = new List<ExportedMethod> ();

			if (type.HasMethods) {
				ProcessMethods (type, exported);
				ProcessConstructors (type, exported);
			}

			if (exported.Count == 0)
				return;

			InjectMethodMap (type, exported);
		}

		void InjectMethodMap (TypeDefinition type, List<ExportedMethod> exported_methods)
		{
			PrepareImports ();

			var cctor = GetTypeConstructor (type);

			var selectors = MapSelectors (cctor);

			var map = new VariableDefinition (dictionary_intptr_methoddesc);
			map.Name = "$method_map";
			cctor.Body.Variables.Add (map);
			cctor.Body.SimplifyMacros ();

			var il = cctor.Body.GetILProcessor ();

			var instructions = new List<Instruction> {
				il.Create (OpCodes.Ldc_I4, exported_methods.Count),
				il.Create (OpCodes.Newobj, dictionary_intptr_methoddesc_ctor),
				il.Create (OpCodes.Stloc, map),
			};

			foreach (var exported in exported_methods) {

				instructions.Add (il.Create (OpCodes.Ldloc, map));

				if (!IsDefaultConstructor (exported)) {
					var selector_name = GetSelectorName (exported);
					FieldReference selector;

					if (selectors != null && selectors.TryGetValue (selector_name, out selector)) {
						instructions.Add (il.Create (OpCodes.Ldsfld, selector));
					} else {
						instructions.AddRange (new [] {
							il.Create (OpCodes.Ldstr, selector_name),
							il.Create (OpCodes.Call, selector_get_handle),
						});
					}
				} else
					instructions.Add (il.Create (OpCodes.Ldsfld, selector_init));

				instructions.AddRange (new [] {
					il.Create (OpCodes.Ldtoken, exported.method),
					il.Create (OpCodes.Call, methodbase_get_method_from_handle),
					il.Create (OpCodes.Ldc_I4, GetArgumentSemantic (exported)),
					il.Create (OpCodes.Newobj, methoddesc_ctor),
					il.Create (OpCodes.Callvirt, dictionary_intptr_methoddesc_set_item),
				});
			}

			instructions.AddRange (new [] {
				il.Create (OpCodes.Ldtoken, type),
				il.Create (OpCodes.Call, type_get_type_from_handle),
				il.Create (OpCodes.Ldloc, map),
				il.Create (OpCodes.Call, class_register_methods),
			});

			Append (il, instructions);

			cctor.Body.OptimizeMacros ();
		}

		static Dictionary<string, FieldReference> MapSelectors (MethodDefinition cctor)
		{
			var instructions = cctor.Body.Instructions;
			Dictionary<string, FieldReference> selectors = null;

			for (int i = 0; i < instructions.Count; i++) {
				var instruction = instructions [i];

				FieldReference field;
				if (!IsCreateSelector (instruction, out field))
					continue;

				if (selectors == null)
					selectors = new Dictionary<string, FieldReference> ();

				selectors.Add ((string) instruction.Operand, field);
			}

			return selectors;
		}

		static bool IsCreateSelector (Instruction instruction, out FieldReference field)
		{
			field = null;

			if (instruction.OpCode != OpCodes.Ldstr)
				return false;

			if (instruction.Next == null)
				return false;

			instruction = instruction.Next;

			if (instruction.OpCode != OpCodes.Call)
				return false;

			var method = (MethodReference) instruction.Operand;
			if (method.DeclaringType.Name != "Selector")
				return false;

			if (method.Name != "GetHandle" && method.Name != "sel_registerName")
				return false;

			if (instruction.Next == null)
				return false;

			instruction = instruction.Next;

			if (instruction.OpCode != OpCodes.Stsfld)
				return false;

			field = instruction.Operand as FieldReference;
			return true;
		}

		static bool IsDefaultConstructor (ExportedMethod exported)
		{
			return exported.attribute == null && exported.method.IsConstructor && !exported.method.IsStatic;
		}

		static void Append (ILProcessor il, IEnumerable<Instruction> instructions)
		{
			var method_instructions = il.Body.Instructions;
			var last = method_instructions [method_instructions.Count - 1];

			foreach (var instruction in instructions)
				il.InsertBefore (last, instruction);
		}

		static int GetArgumentSemantic (ExportedMethod exported)
		{
			if (exported.attribute == null)
				return 0; // Assign

			var arguments = exported.attribute.ConstructorArguments;

			if (arguments.Count == 2)
				return (int) arguments [1].Value;

			if (arguments.Count == 1)
				return -1; // None

			return 0; // Assign
		}

		static string GetSelectorName (ExportedMethod exported)
		{
			var arguments = exported.attribute.ConstructorArguments;

			if (arguments.Count == 0)
				return exported.method.Name;

			return (string) arguments [0].Value;
		}

		MethodDefinition GetTypeConstructor (TypeDefinition type)
		{
			return type.GetTypeConstructor () ?? CreateTypeConstructor (type);
		}

		MethodDefinition CreateTypeConstructor (TypeDefinition type)
		{
			var cctor = new MethodDefinition (".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, void_type);
			cctor.Body.GetILProcessor ().Emit (OpCodes.Ret);

			type.Methods.Add (cctor);

			return cctor;
		}

		void ProcessConstructors (TypeDefinition type, List<ExportedMethod> exported)
		{
			foreach (MethodDefinition ctor in type.GetConstructors ()) {
				if (!ctor.HasParameters && !ctor.IsStatic) {
					exported.Add (new ExportedMethod (null, ctor));
					continue;
				}

				CustomAttribute export;
				if (!TryGetExportAttribute (ctor, out export))
					continue;

				exported.Add (new ExportedMethod (export, ctor));
			}
		}

		static bool TryGetExportAttribute (MethodDefinition method, out CustomAttribute export)
		{
			export = null;

			if (!method.HasCustomAttributes)
				return false;

			foreach (CustomAttribute attribute in method.CustomAttributes) {
				if (attribute.AttributeType.FullName != "MonoMac.Foundation.ExportAttribute")
					continue;

				export = attribute;
				return true;
			}

			return false;
		}

		void ProcessMethods (TypeDefinition type, List<ExportedMethod> exported)
		{
			foreach (MethodDefinition method in type.GetMethods ()) {
				CustomAttribute attribute;
				if (TryGetExportAttribute (method, out attribute)) {
					exported.Add (new ExportedMethod (attribute, method));
					continue;
				}

				if (!method.IsVirtual)
					continue;

				var bases = Annotations.GetBaseMethods (method);
				if (bases == null)
					continue;

				foreach (MethodDefinition @base in bases) {
					if (@base.DeclaringType.IsInterface)
						continue;

					if (TryGetExportAttribute (@base, out attribute)) {
						exported.Add (new ExportedMethod (attribute, method));
						break;
					}
				}
			}
		}
	}
}
