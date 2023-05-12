using System;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class ParametersUsedViaReflection
	{
		public static void Main ()
		{
			TestMethodParameters ();
			TestClassParameters ();
		}

		[Kept]
		static void TestMethodParameters ()
		{
			var method = typeof (GetMethod_Name).GetMethod ("OnlyCalledViaReflection");
			var name = method.GetParameters ()[0].Name;

			GetMethod_Name.CalledDirectly (11);
			GetMethod_Name.CalledDirectly2<string> (1);

			Action<int> action = GetMethod_Name.OnlyUsedViaDelegate;
			name = action.Method.GetParameters ()[0].Name;

			GetMethod_Name instance = new GetMethod_Name ();
			action = instance.OnlyUseViaDelegateVirt;

			Expression<Action> ex = () => GetMethod_Name.OnlyUsedViaLdToken (42);

			TestLambdaUsage ((int firstName) => { firstName.ToString (); });
		}

		[Kept]
		static void TestClassParameters ()
		{
			var type = Type.GetType ("Mono.Linker.Tests.Cases.Reflection.ParametersUsedViaReflection+GenericClass1`1");

			var type2 = new GenericClass2<int> ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GetMethod_Name
		{
			[Kept]
			public static int OnlyCalledViaReflection (int firstName)
			{
				return 2;
			}

			[Kept]
			public static int OnlyCalledViaReflection (int arg1, int arg2)
			{
				return 3;
			}

			[Kept]
			public static void CalledDirectly ([RemovedNameValue] int firstArg)
			{
			}

			[Kept]
			public static void CalledDirectly2</*[RemovedNameValue]*/LongGenericName> ([RemovedNameValue] int firstArg)
			{
			}

			[Kept]
			public static void OnlyUsedViaDelegate (int firstName)
			{
			}

			[Kept]
			public virtual void OnlyUseViaDelegateVirt (int firstName)
			{
			}

			[Kept]
			public static void OnlyUsedViaLdToken (int firstName)
			{
			}
		}

		[Kept]
		public static void TestLambdaUsage ([RemovedNameValue] Delegate action)
		{
			var n = action.Method.GetParameters ()[0].Name;
		}

		[Kept]
		public class GenericClass1<TKey>
		{
		}

		[Kept]
		public class GenericClass2</*[RemovedNameValue]*/TRKey>
		{
			[Kept]
			public GenericClass2 ()
			{
			}
		}
	}

}
