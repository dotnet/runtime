// Because
// a. MCS never generate JMP instructions (good thing);
// b. Presently Mono's ILASM doesn't support declarative security attributes
//    (bad thing ;-) http://bugzilla.ximian.com/show_bug.cgi?id=66033
//
// we cannot test the JMP instruction without outside help.
//
// Instructions:
// 1. Compile this source file with MCS (or CSC)
// 2. Decompile this using monodis (or ildasm)
// 3. Change the "call" from Test to InnerTest by a "jmp"
//
//	from something like:
//		.method private static hidebysig default int32 Test (int32 rc) cil managed
//		{
//			.maxstack 8
//			IL_0000:  ldarg.0
//			IL_0001:  call int32 class Program::InnerTest(int32)
//			IL_0006:  ret
//		}
//	to:
//		.method private static hidebysig default int32 Test (int32 rc) cil managed 
//		{
//			.maxstack 8
//			jmp int32 class Program::InnerTest(int32)
//		}
//
// 4. Re-assemble with *MS* ilasm (until 660033 is fixed)
// 5. Execute the re-assembled assembly

using System;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	static public int InnerTest (int rc)
	{
		// so the caller is in *this* assembly (so RequestRefuse applies)
		Console.WriteLine ("*1* Library call expected to fail!");
		return rc;
	}

	static int Test (int rc)
	{
		return InnerTest (rc);
	}

	static int Main ()
	{
		try {
			return Test (1);
		}
		catch (SecurityException se) {
			Console.WriteLine ("*0* Expected SecurityException\n{0}", se);
			return 0;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected Exception\n{0}", e);
			return 2;
		}
	}
}
