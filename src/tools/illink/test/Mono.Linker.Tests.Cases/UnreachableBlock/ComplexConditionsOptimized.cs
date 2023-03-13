using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[Reference ("System.Reflection.Emit.dll")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ComplexConditionsOptimized
	{
		public static void Main ()
		{
			TestSwitch.Test ();
			TestSwitch.TestOffset ();
			TestSwitch2.TestFallThrough ();
			TestSwitchZero.Test ();
			TestSwitch3.TestFallThrough (true);
		}

		[Kept]
		class TestSwitch
		{
			static int KnownInteger {
				get => 2;
			}

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.2",
				"stloc.0",
				"ldloc.0",
				"brtrue il_8",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditionsOptimized/TestSwitch::Reached()",
				"ret",
			})]
			public static void Test ()
			{
				switch (KnownInteger) {
				case 0:
					Unreached ();
					break;
				case 1:
					Unreached ();
					break;
				case 2:
					Reached ();
					break;
				default:
					throw new ApplicationException ();
				}
			}

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.2",
				"stloc.0",
				"ldloc.0",
				"ldc.i4.1",
				"sub",
				"brtrue il_10",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditionsOptimized/TestSwitch::Reached()",
				"ret",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditionsOptimized/TestSwitch::Reached()",
				"br.s il_a",
			})]
			public static void TestOffset ()
			{
				switch (KnownInteger) {
				case 1:
					Reached ();
					break;
				case 2:
					Reached ();
					goto case 1;
				case 3:
					Unreached ();
					break;
				default:
					throw new ApplicationException ();
				}
			}

			static void Unreached () { }

			[Kept]
			static void Reached () { }
		}

		[Kept]
		class TestSwitch2
		{
			static int KnownInteger {
				get => 9;
			}

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.s 0x9",
				"stloc.0",
				"ldloc.0",
				"pop",
				"br.s il_7",
				"newobj System.Void System.ApplicationException::.ctor()",
				"throw",
			})]
			public static void TestFallThrough ()
			{
				switch (KnownInteger) {
				case 0:
					Unreached ();
					break;
				case 1:
					Unreached ();
					break;
				case 2:
					Unreached ();
					break;
				default:
					throw new ApplicationException ();
				}
			}

			static void Unreached () { }
		}

		[Kept]
		class TestSwitch3
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.4",
				"stloc.0",
				"ldloc.0",
				"pop",
				"br.s il_6",
				"newobj System.Void System.NotSupportedException::.ctor()",
				"throw",
			})]
			public static object TestFallThrough (bool createReader)
			{
				object instance;

				switch (ProcessArchitecture, createReader) {
				case (Architecture.X86, true):
					Unreached ();
					break;
				case (Architecture.X86, false):
					Unreached ();
					break;
				case (Architecture.X64, true):
					Unreached ();
					break;
				case (Architecture.X64, false):
					Unreached ();
					break;
				case (Architecture.Arm64, true):
					Unreached ();
					break;
				case (Architecture.Arm64, false):
					Unreached ();
					break;
				default:
					throw new NotSupportedException ();
				}

				return null;
			}

			static Architecture ProcessArchitecture => Architecture.Wasm;

			static void Unreached () { }
		}

		[Kept]
		class TestSwitchZero
		{
			static int KnownInteger {
				get => 0;
			}

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse il_8",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ComplexConditionsOptimized/TestSwitchZero::Reached()",
				"ret",
			})]
			public static void Test ()
			{
				switch (KnownInteger) {
				case 0:
					Reached ();
					break;
				case 1:
					Unreached ();
					break;
				case 2:
					Unreached ();
					break;
				default:
					throw new ApplicationException ();
				}
			}

			static void Unreached () { }

			[Kept]
			static void Reached () { }
		}
	}
}