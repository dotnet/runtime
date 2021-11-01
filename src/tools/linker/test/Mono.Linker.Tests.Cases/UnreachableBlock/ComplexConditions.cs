using System;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[Reference ("System.Reflection.Emit.dll")]
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
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldarg.0",
			"isinst System.Type",
			"brtrue.s il_19",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditions::get_IsDynamicCodeSupported()",
			"pop",
			"ldarg.0",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"br.s il_17",
			"br.s il_1a",
			"ldc.i4.1",
			"stloc.0",
			"ldloc.0",
			"brfalse.s il_24",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditions::Reached_1()",
			"nop",
			"ret",
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
		[ExpectedInstructionSequence (new[] {
			"nop",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditions::get_IsDynamicCodeSupported()",
			"stloc.1",
			"ldloc.1",
			"pop",
			"ldc.i4.0",
			"stloc.0",
			"ldarg.0",
			"ldc.i4.2",
			"beq.s il_15",
			"ldarg.0",
			"ldc.i4.3",
			"ceq",
			"br.s il_16",
			"ldc.i4.1",
			"stloc.2",
			"ldloc.2",
			"brfalse.s il_20",
			"newobj System.Void System.ArgumentException::.ctor()",
			"throw",
			"newobj System.Void System.ApplicationException::.ctor()",
			"throw",
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