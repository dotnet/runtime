using System;
using System.Reflection;
using System.Reflection.Emit;

public class Tests {
	// This set of tests are for checking the impact of custom modifiers on vtable layout and
	// method overrides. CustomModifiers are used for one thing and one thing alone, and that's making
	// two method signatures that would otherwise be equal into unequal signatures. This is used by C++/CLI
	// to tag primitive types with the C++-side type. For instance, this allows a single primitive type for 
	// various integer types, but prevents a function taking an int from overriding a function taking a long.
	//
	// We have to use SRE or il to interact with it, as C# compilers don't do much with modifiers beyond
	// reflection. There's no way to generate the below hierarchy and methods by compiling C#.

	Object childNoModObj;
	Object childOptModObj;
	Object childSameModObj;
	Object childForwardObj;
	Object childBackwardObj;
	MethodInfo invokeParentSig;
	MethodInfo invokeParentForwardSig;

	public static void DefineMethodM (TypeBuilder tb, string className, Type [] required_modifiers, Type [] optional_modifiers)
	{
		// Example il (no equivalent C# for modreqs)
		// Note that the modifiers will change for each class
		//
		//    // method line 4
		// .method public virtual hidebysig 
		//        instance default string M (int32* modreq ([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute)  A_1)  cil managed 
		// {
		//     // Method begins at RVA 0x2104
		// Code size 6 (0x6)
		// 	.maxstack 8
		// 	IL_0000:  ldstr "ChildSameMod"
		// 	IL_0005:  ret 
		//   } // end of method ChildSameMod::M
		//

		Type[][] req_mods = required_modifiers != null ? new Type [][] {required_modifiers} : null;
		Type[][] opt_mods = optional_modifiers != null ? new Type [][] {optional_modifiers} : null;
		MethodBuilder mbIM = tb.DefineMethod("M", MethodAttributes.Public | MethodAttributes.HideBySig |
		        MethodAttributes.Virtual,
		    CallingConventions.Standard,
		    typeof (string), null, null,
		    new Type[] {typeof (int *)}, req_mods, opt_mods);
		ILGenerator il = mbIM.GetILGenerator();
		il.Emit (OpCodes.Ldstr, className);
		//il.Emit (OpCodes.Ldstr, className);
		//il.Emit (OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
		il.Emit (OpCodes.Ret);
	}

	public static void DefineMethodInvoke (TypeBuilder tb, string invokeName, MethodInfo target, Type argument)
	{
		// Example il (no equivalent C# for modreqs)
		// Note that the modifiers will change as per the callee
		//
		//    .method public static
		//           default string InvokeParentSig (class Parent A_0)  cil managed
		//    {
		//	// Code size 14 (0xe)
		//	.maxstack 3
		//	.locals init (
		//		int32	V_0)
		//	IL_0000:  ldarg.0
		//	IL_0001:  ldc.i4.0
		//	IL_0002:  stloc.0
		//	IL_0003:  ldloca.s 0
		//	IL_0005:  nop
		//	IL_0006:  nop
		//	IL_0007:  nop
		//	IL_0008:  callvirt instance string class Parent::M(int32* modreq ([mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute) )
		//	IL_000d:  ret
		//} // end of method Invoker::InvokeParentSig
		// 
		//

		MethodBuilder mbIM = tb.DefineMethod(invokeName, MethodAttributes.Public | MethodAttributes.Static,
		    CallingConventions.Standard,
		    typeof (string), null, null,
		    new Type [] {argument}, null, null);
		ILGenerator il = mbIM.GetILGenerator();
		var local = il.DeclareLocal (typeof (int));

		il.Emit (OpCodes.Ldarg_0);
		il.Emit (OpCodes.Ldc_I4_0);
		il.Emit (OpCodes.Stloc, local);
		il.Emit (OpCodes.Ldloca_S, 0);
		il.Emit (OpCodes.Callvirt, target);

		il.Emit (OpCodes.Ret);
	}

	public Tests ()
	{
		string name = "CustomModifiersOverride";
		AssemblyName asmName = new AssemblyName(name);
		AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);
		ModuleBuilder mb = ab.DefineDynamicModule(name, name + ".dll");

		// Create class hierarchy:
		// Parent > ChildSameMod
		// Parent > ChildNoMod
		// Parent > ChildOptMod
		//
		// The Same, No, and Opt labels describe whether it's the same modifiers,
		// optional modifiers rather than required, or with no modifiers.
		//
		// ParentThreeForward > ChildThreeForward
		// ParentThreeForward > ChildThreeBackward
		//
		// These have to do with ordering. There are 3 mods, and forward/backwards is
		// relative and has to do with the order the modifiers appear in the IL.
		//
		// Each of these classes has a function called "M" with an int pointer argument which
		// contains custom modifiers. Based on whether the child's method overrides the parent's
		// method, we can tell whether the method signatures have been found equal by the underlying
		// runtime, and whether the impact on VTable layout is correct.

		var mods = new Type [] { typeof (System.Runtime.CompilerServices.IsReadOnlyAttribute) };
	
		var parent = mb.DefineType("Parent", TypeAttributes.Public);
		DefineMethodM (parent, "Parent", mods, null);
		var parentType = parent.CreateType ();
	
		var childSameMod = mb.DefineType("ChildSameMod", TypeAttributes.Public, parent);
		DefineMethodM (childSameMod, "ChildSameMod", mods, null);
		var childSameModType = childSameMod.CreateType ();

		var childNoMod = mb.DefineType("ChildNoMod", TypeAttributes.Public, parent);
		DefineMethodM (childNoMod, "ChildNoMod", null, null);
		var childNoModType = childNoMod.CreateType ();

		var childOptMod = mb.DefineType("ChildOptMod", TypeAttributes.Public, parent);
		DefineMethodM (childOptMod, "ChildOptMod", null, mods);
		var childOptModType = childOptMod.CreateType ();

		var first_mod = typeof (System.Runtime.CompilerServices.IsReadOnlyAttribute);
		var second_mod = typeof (System.Runtime.CompilerServices.SpecialNameAttribute);
		var third_mod = typeof (System.Runtime.CompilerServices.IsConst);

		var forwardMods = new Type [] {first_mod, second_mod, third_mod};
		var backwardMods = new Type [] {third_mod, second_mod, first_mod};

		var parentThreeForward = mb.DefineType("ParentThreeForward", TypeAttributes.Public);
		DefineMethodM (parentThreeForward, "ParentThreeForward", forwardMods, null);
		var parentThreeForwardType = parentThreeForward.CreateType ();

		var childThreeForward = mb.DefineType("ChildThreeForward", TypeAttributes.Public, parentThreeForward);
		DefineMethodM (childThreeForward, "ChildThreeForward", forwardMods, null);
		var childThreeForwardType = childThreeForward.CreateType ();

		var childThreeBackward = mb.DefineType("ChildThreeBackward", TypeAttributes.Public, parentThreeForward);
		DefineMethodM (childThreeBackward, "ChildThreeBackward", backwardMods, null);
		var childThreeBackwardType = childThreeBackward.CreateType ();

		var invoker = mb.DefineType("Invoker", TypeAttributes.Public);
		// Since we want to generate callvirt calls which contain references to the custom modifiers
		// for the parent types, to actually probe overloading or not, we need to generate the classes the make those
		// calls

		var methodBaseReference = parentType.GetMethod ("M");
		DefineMethodInvoke (invoker, "InvokeParentSig", methodBaseReference, parentType);

		var methodBaseReferenceMultiple = parentThreeForwardType.GetMethod ("M");
		DefineMethodInvoke (invoker, "InvokeParentThreeForwardSig", methodBaseReferenceMultiple, parentThreeForwardType);

		var invokerType = invoker.CreateType ();

		ab.Save(name + ".dll");

		childNoModObj = Activator.CreateInstance (childNoModType);
		childOptModObj = Activator.CreateInstance (childOptModType);
		childSameModObj = Activator.CreateInstance (childSameModType);
		childForwardObj = Activator.CreateInstance (childThreeForwardType);
		childBackwardObj = Activator.CreateInstance (childThreeBackwardType);
		invokeParentSig = invokerType.GetMethod ("InvokeParentSig");
		invokeParentForwardSig = invokerType.GetMethod ("InvokeParentThreeForwardSig");
	}

	public static int Main () {
		// The expected behavior can be near impossible to reproduce using this .exe on
		// another CLR because many SRE implementations do not correctly use modifiers. To
		// compare results with another runtime, get the dll saved by the above ab.Save()
		// line, manually copy it over, and run it with the other CLR. 
		var tester = new Tests ();
		tester.TestModSufficient ();
		tester.TestModNecessary ();
		tester.TestModReqSameAsOpt ();
		tester.TestModReqMulti ();
		tester.TestModReqMultiNoOrder ();

		return 0;
	}

	public void TestModSufficient()
	{
		var result = (string)invokeParentSig.Invoke(null, new Object[] { childSameModObj });
		if (result != "ChildSameMod")
			throw new Exception("TestModSufficient: Unexpected Class Override");
		else
			Console.WriteLine("Success: {0} {1}", childSameModObj.GetType().Name, result);
	}
	public void TestModNecessary()
	{
		var result = (string)invokeParentSig.Invoke(null, new Object[] { childNoModObj });
		if (result != "Parent")
			throw new Exception(String.Format("Unexpected Class Override: {0}", result));
		else
			Console.WriteLine("Success: {0} {1}", childNoModObj.GetType().Name, result);
	}
	public void TestModReqSameAsOpt()
	{
		var result = (string)invokeParentSig.Invoke(null, new Object[] { childOptModObj });
		if (result != "Parent")
			throw new Exception("Unexpected Class Override");
		else
			Console.WriteLine("Success: {0} {1}", childOptModObj.GetType().Name, result);
	}
	public void TestModReqMulti()
	{
		var result = (string)invokeParentForwardSig.Invoke(null, new Object[] { childForwardObj });
		if (result != "ChildThreeForward")
			throw new Exception(String.Format("Unexpected Class Override {0}, MULTIPLE MODS BROKEN", result));
		else
			Console.WriteLine("Success MULTI MODS: {0} {1}", childForwardObj.GetType().Name, result);
	}
	public void TestModReqMultiNoOrder()
	{
		var result = (string)invokeParentForwardSig.Invoke(null, new Object[] { childBackwardObj });
		if (result != "ParentThreeForward")
			throw new Exception(String.Format("Unexpected Class Override: {0}, ORDER MATTERS", result));
		else
			Console.WriteLine("Success ORDER MATTERS: {0} {1}", childBackwardObj.GetType().Name, result);
	}
}
