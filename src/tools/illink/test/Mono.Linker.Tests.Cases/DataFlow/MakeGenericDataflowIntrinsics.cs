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

			static Type GrabUnknownType () => null;

			public static void Test ()
			{
				TestRecognizedIntrinsic ();
				TestRecognizedGenericIntrinsic<object> ();
				TestUnknownOwningType ();
				TestUnknownArgument ();
			}

			public static void TestRecognizedIntrinsic () => typeof (Gen<>).MakeGenericType (typeof (object));

			public static void TestRecognizedGenericIntrinsic<T> () => typeof (Gen<>).MakeGenericType (typeof (T));

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			[ExpectedWarning ("IL3050", nameof (Type.MakeGenericType), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownOwningType () => GrabUnknownType ().MakeGenericType (typeof (object));

			[ExpectedWarning ("IL3050", nameof (Type.MakeGenericType), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownArgument () => typeof (Gen<>).MakeGenericType (GrabUnknownType ());
		}

		class MakeGenericMethod
		{
			public static void Gen<T> () { }

			static MethodInfo GrabUnknownMethod () => null;

			static Type GrabUnknownType () => null;

			public static void Test ()
			{
				TestRecognizedIntrinsic ();
				TestRecognizedGenericIntrinsic<object> ();
				TestUnknownOwningMethod ();
				TestUnknownArgument ();
			}

			public static void TestRecognizedIntrinsic () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (typeof (object));

			public static void TestRecognizedGenericIntrinsic<T> () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (typeof (T));

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			[ExpectedWarning ("IL3050", nameof (MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownOwningMethod () => GrabUnknownMethod ().MakeGenericMethod (typeof (object));

			[ExpectedWarning ("IL3050", nameof (MethodInfo.MakeGenericMethod), Tool.Analyzer | Tool.NativeAot, "")]
			public static void TestUnknownArgument () => typeof (MakeGenericMethod).GetMethod (nameof (Gen)).MakeGenericMethod (GrabUnknownType());
		}
	}
}
