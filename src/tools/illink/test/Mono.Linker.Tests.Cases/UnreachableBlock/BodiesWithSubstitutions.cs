using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerSubstitutionFile ("BodiesWithSubstitutions.xml")]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SetupCompileBefore (
		"LibReturningConstant.dll",
		new[] { "Dependencies/LibReturningConstant.cs" })]
	[SetupCompileBefore (
		"LibWithConstantSubstitution.dll",
		new[] { "Dependencies/LibWithConstantSubstitution.cs" },
		resources: new object[] { new string[] { "Dependencies/LibWithConstantSubstitution.xml", "ILLink.Substitutions.xml" } })]
	[IgnoreSubstitutions (false)]
	[KeptModuleReference ("unknown")]
	public class BodiesWithSubstitutions
	{
		static class ClassWithField
		{
			[Kept]
			public static int SField;
		}

		static int field;

		public static void Main ()
		{
			TestProperty_int_1 ();
			TestField_int_1 ();
			NoInlining ();
			TestPropagation ();

			LoopWithoutConstants.Test ();
			LoopWithConstants.Test ();
			LoopWithConstantsComplex.Test ();
			MultiLoopWithConstantsComplex.Test ();
			DeepConstant.Test ();

			ConstantFromNewAssembly.Test ();
			ConstantSubstitutionsFromNewAssembly.Test ();
			TestSubstitutionCollision ();
			TestSubstitutionOnNoInlining ();
			TestSubstitutionOnIntrinsic ();
			TestMethodWithoutBody ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_int_1 ()
		{
			if (Property != 3)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestField_int_1 ()
		{
			if (ClassWithField.SField != 9)
				NeverReached_1 ();
		}

		[Kept]
		static int Property {
			[Kept]
			[ExpectBodyModified]
			get {
				return field;
			}
		}

		static void NeverReached_1 ()
		{
		}

		[Kept]
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		static int NoInliningInner ()
		{
			return 1;
		}

		// Methods with NoInlining won't be evaluated by the linker
		[Kept]
		static void NoInlining ()
		{
			if (NoInliningInner () != 1)
				Reached_1 ();
		}

		[Kept]
		static void Reached_1 ()
		{
		}

		[Kept]
		static int PropagateProperty {
			[Kept]
			get {
				return Property;
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestPropagation ()
		{
			// We don't propagate return values across method calls
			if (PropagateProperty != 3)
				Propagation_NeverReached ();
			else
				Propagation_Reached ();
		}

		static void Propagation_NeverReached ()
		{
		}

		[Kept]
		static void Propagation_Reached ()
		{
		}

		static class LoopWithoutConstants
		{
			[Kept]
			static bool LoopMethod1 (int depth)
			{
				if (depth > 100)
					return false;

				return LoopMethod2 (depth + 1);
			}

			[Kept]
			static bool LoopMethod2 (int depth)
			{
				return LoopMethod1 (depth + 1);
			}

			[Kept] static void Reached () { }
			[Kept] static void Reached2 () { }

			[Kept]
			public static void Test ()
			{
				if (LoopMethod1 (1))
					Reached ();
				else
					Reached2 ();
			}
		}

		static class LoopWithConstants
		{
			[Kept]
			static int depth = 0;

			[Kept]
			static bool LoopMethod1 ()
			{
				depth++;
				return LoopMethod2 ();
			}

			[Kept]
			static bool LoopMethod2 ()
			{
				if (depth < 100)
					LoopMethod1 ();

				return false;
			}

			[Kept] static void Reached () { }
			[Kept] static void Reached2 () { }

			[Kept]
			public static void Test ()
			{
				// Currently we don't recognize this pattern as constant
				// Technically LoopMethod1 will always return false
				if (LoopMethod1 ())
					Reached ();
				else
					Reached2 ();
			}
		}

		static class LoopWithConstantsComplex
		{
			[Kept]
			static int depth = 0;

			[Kept]
			static bool IsTrue ()
			{
				return true;
			}

			static void ShouldNotBeReached () { }

			[Kept]
			[ExpectBodyModified]
			static bool LoopMethod1 ()
			{
				depth++;
				if (!IsTrue ())
					ShouldNotBeReached ();

				return LoopMethod2 ();
			}

			[Kept]
			[ExpectBodyModified]
			static bool LoopMethod2 ()
			{
				if (!IsTrue ())
					ShouldNotBeReached ();

				if (depth < 100)
					LoopMethod1 ();

				return false;
			}

			[Kept] static void Reached () { }
			[Kept] static void Reached2 () { }

			[Kept]
			public static void Test ()
			{
				// Currently we don't recognize this pattern as constant
				// Technically LoopMethod1 will always return false
				if (LoopMethod1 ())
					Reached ();
				else
					Reached2 ();
			}
		}

		static class MultiLoopWithConstantsComplex
		{
			[Kept]
			static int depth = 0;

			[Kept]
			static bool IsTrue ()
			{
				return true;
			}

			static void ShouldNotBeReached () { }

			[Kept]
			[ExpectBodyModified]
			static bool InnerLoopMethod1 ()
			{
				depth++;
				if (!IsTrue ())
					ShouldNotBeReached ();

				return InnerLoopMethod2 ();
			}

			[Kept]
			[ExpectBodyModified]
			static bool InnerLoopMethod2 ()
			{
				if (!IsTrue ())
					ShouldNotBeReached ();

				if (depth < 100)
					InnerLoopMethod1 ();

				return false;
			}

			[Kept]
			static void InnerReached () { }

			[Kept]
			[ExpectBodyModified]
			static bool OuterLoopMethod1 ()
			{
				if (!IsTrue ())
					ShouldNotBeReached ();

				// Currently we don't recognize this pattern as constant
				if (InnerLoopMethod1 ())
					InnerReached ();

				return OuterLoopMethod2 ();
			}

			[Kept]
			[ExpectBodyModified]
			static bool OuterLoopMethod2 ()
			{
				if (!IsTrue ())
					ShouldNotBeReached ();

				return OuterLoopMethod1 ();
			}

			[Kept] static void Reached () { }
			[Kept] static void Reached2 () { }

			[Kept]
			public static void Test ()
			{
				// Currently we don't recognize this pattern as constant
				if (OuterLoopMethod1 ())
					Reached ();
				else
					Reached2 ();
			}
		}

		static class DeepConstant
		{
			[Kept] static bool Method1 () => Method2 ();
			[Kept] static bool Method2 () => Method3 ();
			[Kept] static bool Method3 () => Method4 ();
			[Kept] static bool Method4 () => Method5 ();
			[Kept] static bool Method5 () => Method6 ();
			[Kept] static bool Method6 () => Method7 ();
			[Kept] static bool Method7 () => Method8 ();
			[Kept] static bool Method8 () => Method9 ();
			[Kept] static bool Method9 () => Method10 ();
			[Kept] static bool Method10 () => false;

			static void NotReached () { }
			[Kept] static void Reached () { }

			[Kept]
			[ExpectBodyModified]
			public static void Test ()
			{
				if (Method1 ())
					NotReached ();
				else
					Reached ();
			}
		}

		static class ConstantFromNewAssembly
		{
			static void NotReached () { }
			[Kept] static void Reached () { }

			[Kept]
			[ExpectBodyModified]
			public static void Test ()
			{
				if (LibReturningConstant.ReturnFalse ())
					NotReached ();
				else
					Reached ();
			}
		}

		static class ConstantSubstitutionsFromNewAssembly
		{
			static void NotReached () { }
			[Kept] static void Reached () { }

			[Kept]
			[ExpectBodyModified]
			public static void Test ()
			{
				if (LibWithConstantSubstitution.ReturnFalse ())
					NotReached ();
				else
					Reached ();
			}
		}

		[Kept]
		static bool CollisionProperty {
			[Kept]
			[ExpectBodyModified]
			get {
				// Need to call something with constant value to force processing of this method
				_ = Property;
				return true;
			} // Substitution will set this to false
		}

		// This tests that if there's a method (get_CollisionProperty) which itself is constant
		// and substitution changes its return value, the branch removal reacts to the substituted value
		// and not the value from the method's body.
		// This should ideally never happen, but still.
		// In the original code this test would be order dependent. Depending if TestSubstitutionsCollision
		// was processed before CollisionProperty, it would either propagate true or false.
		[Kept]
		[ExpectBodyModified]
		static void TestSubstitutionCollision ()
		{
			if (CollisionProperty)
				Collision_NeverReached ();
			else
				Collision_Reached ();
		}

		[Kept]
		static void Collision_Reached () { }
		static void Collision_NeverReached () { }

		[Kept]
		static bool NoInliningProperty {
			[Kept]
			[ExpectBodyModified]
			[MethodImpl (MethodImplOptions.NoInlining)]
			get { return true; }
		}

		[Kept]
		[ExpectBodyModified]
		static void TestSubstitutionOnNoInlining ()
		{
			if (NoInliningProperty)
				NoInlining_NeverReached ();
			else
				NoInlining_Reached ();
		}

		[Kept]
		static void NoInlining_Reached () { }
		static void NoInlining_NeverReached () { }

		[Kept]
		static bool IntrinsicProperty {
			[Kept]
			[ExpectBodyModified]
			[Intrinsic]
			[KeptAttributeAttribute (typeof (IntrinsicAttribute))]
			get { return true; }
		}

		[Kept]
		[ExpectBodyModified]
		static void TestSubstitutionOnIntrinsic ()
		{
			if (IntrinsicProperty)
				Intrinsic_NeverReached ();
			else
				Intrinsic_Reached ();
		}

		[Kept]
		static void Intrinsic_Reached () { }
		static void Intrinsic_NeverReached () { }

		[Kept]
		[System.Runtime.InteropServices.DllImport ("unknown")]
		static extern int PInvokeMethod ();

		[Kept]
		static void TestMethodWithoutBody ()
		{
			if (PInvokeMethod () == 0)
				MethodWithoutBody_Reached ();
		}

		[Kept]
		static void MethodWithoutBody_Reached () { }
	}
}
