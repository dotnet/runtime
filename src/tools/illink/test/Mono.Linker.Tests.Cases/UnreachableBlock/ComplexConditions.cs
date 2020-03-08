using System;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCompileArgument ("/optimize-")] // Relying on debug csc behaviour
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ComplexConditions
	{
		public static void Main ()
		{
			Test_1 (null);
			Test_2 (9);
		}

		[Kept]
#if !NETCOREAPP
		[ExpectBodyModified]
#else
		[ExpectedInstructionSequence (new [] {
			"nop",
			"ldarg.0",
			"isinst",
			"brtrue.s",
			"call",
			"pop",
			"ldarg.0",
			"isinst",
			"ldnull",
			"cgt.un",
			"br.s",
			"nop",
			"br.s",
			"ldc.i4.1",
			"stloc.0",
			"ldloc.0",
			"brfalse.s",
			"call",
			"nop",
			"ret"
			})]
#endif
		static void Test_1 (object type)
		{
			if (type is Type || (IsDynamicCodeSupported && type is TypeBuilder))
				Reached_1 ();
		}

		[Kept]
#if !NETCOREAPP
		[ExpectBodyModified]
#else
		[ExpectedInstructionSequence (new [] {
			"nop",
			"call",
			"stloc.1",
			"ldloc.1",
			"pop",
			"ldc.i4.0",
			"stloc.0",
			"ldarg.0",
			"ldc.i4.2",
			"beq.s",
			"ldarg.0",
			"ldc.i4.3",
			"ceq",
			"br.s",
			"ldc.i4.1",
			"stloc.2",
			"ldloc.2",
			"brfalse.s",
			"newobj",
			"throw",
			"newobj",
			"throw"
			})]
#endif
		static void Test_2 (int a)
		{
			int zero;
			if (IsDynamicCodeSupported)
				zero = 0;

			if (a == 2 || a == 3)
				throw new ArgumentException ();

			throw new ApplicationException ();
		}

		[Kept]
		static bool IsDynamicCodeSupported {
			[Kept]
			get {
				return true;
			}
		}

		[Kept]
		static void Reached_1 ()
		{
		}
	}
}