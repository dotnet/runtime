using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	class VarianceBasic
	{
		[Kept]
		static void Main ()
		{
			TestEntrypoint.Test ();
		}
		[Kept]
		public interface InterfaceScenario1<
			[KeptGenericParamAttributes (GenericParameterAttributes.Contravariant)]
			in T
		>
		{
			[Kept]
			static abstract int Method ();
		}
		[Kept]
		public interface InterfaceScenario2<
			[KeptGenericParamAttributes (GenericParameterAttributes.Contravariant)]
			in T
		>
		{
			[Kept]
			static abstract int Method ();

			[Kept]
			static abstract int Method2 ();
		}
		[Kept]
		[KeptInterface (typeof (InterfaceScenario1<object>))]
		public class BaseScenario1 : InterfaceScenario1<object>
		{
			[Kept]
			public static int Method ()
			{
				return 1;
			}
		}
		[Kept]
		[KeptBaseType (typeof (BaseScenario1))]
		public class DerivedScenario1 : BaseScenario1
		{
		}
		[Kept]
		[KeptInterface (typeof (InterfaceScenario2<string>))]
		[KeptInterface (typeof (InterfaceScenario2<object>))]
		public class BaseScenario2 : InterfaceScenario2<string>, InterfaceScenario2<object>
		{
			[Kept]
			static int InterfaceScenario2<string>.Method ()
			{
				return 2;
			}

			[Kept]
			static int InterfaceScenario2<object>.Method ()
			{
				return 3;
			}

			[Kept]
			static int InterfaceScenario2<string>.Method2 ()
			{
				return 4;
			}

			[Kept]
			static int InterfaceScenario2<object>.Method2 ()
			{
				return 5;
			}
		}
		[Kept]
		[KeptBaseType (typeof (BaseScenario2))]
		[KeptInterface (typeof (InterfaceScenario2<object>))]
		public class DerivedScenario2 : BaseScenario2, InterfaceScenario2<object>
		{
			[Kept]
			static int InterfaceScenario2<object>.Method ()
			{
				return 6;
			}

			[Kept]
			static int InterfaceScenario2<object>.Method2 ()
			{
				return 7;
			}
		}
		[Kept]
		public class TestEntrypoint
		{
			[Kept]
			public static string Test_Scenario<T, ImplType> () where ImplType : InterfaceScenario1<T>
			{
				int x = ImplType.Method ();
				return x.ToString ();
			}

			[Kept]
			public static string Test_Scenario2_1<T, ImplType> () where ImplType : InterfaceScenario2<T>
			{
				int x = ImplType.Method ();
				return x.ToString ();
			}

			[Kept]
			public static string Test_Scenario2_2<T, ImplType> () where ImplType : InterfaceScenario2<T>
			{
				int x = ImplType.Method2 ();
				return x.ToString ();
			}

			[Kept]
			public static int Test ()
			{
				try {
					CheckForFailure ("VariantDispatchToBaseTypeMethodVariantly", "1", TestEntrypoint.Test_Scenario<string, BaseScenario1> ());
					CheckForFailure ("VariantDispatchToBaseTypeMethodFromDerivedTypeVariantly", "1", TestEntrypoint.Test_Scenario<string, DerivedScenario1> ());
				} catch (Exception ex) {
					CheckForFailure ("VariantDispatchToBaseTypeMethod", "No Exception", ex.GetType ().Name);
				}
				try {
					CheckForFailure ("NonVariantDispatchToMethodTakesPriorityOverVariantMatch", "2", TestEntrypoint.Test_Scenario2_1<string, BaseScenario2> ());
					CheckForFailure ("NonVariantDispatchToMethodTakesPriorityOverVariantMatch", "3", TestEntrypoint.Test_Scenario2_1<object, BaseScenario2> ());
					CheckForFailure ("NonVariantDispatchToMethodTakesPriorityOverVariantMatch", "4", TestEntrypoint.Test_Scenario2_2<string, BaseScenario2> ());
					CheckForFailure ("NonVariantDispatchToMethodTakesPriorityOverVariantMatch", "5", TestEntrypoint.Test_Scenario2_2<object, BaseScenario2> ());
				} catch (Exception ex) {
					CheckForFailure ("NonVariantDispatchToMethodTakesPriorityOverVariantMatch", "No Exception", ex.GetType ().Name);
				}
				try {

					CheckForFailure ("VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType", "6", TestEntrypoint.Test_Scenario2_1<string, DerivedScenario2> ());
					CheckForFailure ("VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType", "6", TestEntrypoint.Test_Scenario2_1<object, DerivedScenario2> ());
					CheckForFailure ("VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType", "7", TestEntrypoint.Test_Scenario2_2<string, DerivedScenario2> ());
					CheckForFailure ("VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType", "7", TestEntrypoint.Test_Scenario2_2<object, DerivedScenario2> ());
				} catch (Exception ex) {
					CheckForFailure ("VariantDispatchToBaseTypeMethodFromDerivedTypeVariantly", "No Exception", ex.GetType ().Name);
				}
				return 0;
			}

			[Kept]
			public static void CheckForFailure (string a, string b, string c)
			{
			}
		}

	}
}
