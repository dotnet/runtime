using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class MakeGenericDataflowIntrinsics
	{
		public static void Main ()
		{
			MakeGenericType.Test ();
			MakeGenericMethod.Test ();
		}

		class MakeGenericType
		{
			class Gen<T> { }
			class GenConstrained<T> where T : class { }

			static Type GrabUnknownType () => null;

			public static void Test ()
			{
				TestRecognizedIntrinsic ();
				TestRecognizedGenericIntrinsic<object> ();
				TestRecognizedConstraint ();
				TestUnknownOwningType ();
				TestUnknownArgument ();
			}

			public static void TestRecognizedIntrinsic () => typeof (Gen<>).MakeGenericType (typeof (object));

			public static void TestRecognizedGenericIntrinsic<T> () => typeof (Gen<>).MakeGenericType (typeof (T));

			public static void TestRecognizedConstraint () => typeof (GenConstrained<>).MakeGenericType (GrabUnknownType ());

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			[ExpectedWarning ("IL3050", nameof (Type.MakeGenericType), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownOwningType () => GrabUnknownType ().MakeGenericType (typeof (object));

			[ExpectedWarning ("IL3050", nameof (Type.MakeGenericType), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownArgument () => typeof (Gen<>).MakeGenericType (GrabUnknownType ());
		}

		class MakeGenericMethod
		{
			public static void Gen<T> () { }
			public static void GenConstrained<T> () where T : class { }

			static MethodInfo GrabUnknownMethod () => null;

			static Type GrabUnknownType () => null;

			public static void Test ()
			{
				TestRecognizedIntrinsic ();
				TestRecognizedGenericIntrinsic<object> ();
				TestRecognizedConstraint ();
				TestUnknownOwningMethod ();
				TestUnknownArgument ();
			}

			public static void TestRecognizedIntrinsic () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (typeof (object));

			public static void TestRecognizedGenericIntrinsic<T> () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (typeof (T));

			public static void TestRecognizedConstraint () => typeof (MakeGenericMethod).GetMethod (nameof (GenConstrained)).MakeGenericMethod (GrabUnknownType ());

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			[ExpectedWarning ("IL3050", nameof (MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownOwningMethod () => GrabUnknownMethod ().MakeGenericMethod (typeof (object));

			[ExpectedWarning ("IL3050", nameof (MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownArgument () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (GrabUnknownType());
		}
	}
}
