// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class CompilerGeneratedTypes
	{
		public static void Main ()
		{
			// Iterators
			UseIterator ();
			IteratorTypeMismatch ();
			LocalIterator ();
			IteratorCapture ();
			NestedIterators ();
			IteratorInsideClosure ();
			IteratorInsideClosureMismatch ();

			// Async
			Async ();
			AsyncCapture ();
			AsyncTypeMismatch ();
			AsyncInsideClosure ();
			AsyncInsideClosureMismatch ();

			// Closures
			GlobalClosures ();
		}

		private static void UseIterator ()
		{
			foreach (var m in BasicIterator<string> ()) {
			}
		}

		private static IEnumerable<MethodInfo> BasicIterator<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			foreach (var m in typeof (T).GetMethods ()) {
				yield return m;
			}
		}

		private static void IteratorTypeMismatch ()
		{
			_ = Local<string> ();

			[ExpectedWarning ("IL2090", nameof (DynamicallyAccessedMemberTypes.PublicProperties), CompilerGeneratedCode = true)]
			static IEnumerable<object> Local<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				foreach (var m in typeof (T).GetMethods ()) {
					yield return m;
				}
				foreach (var p in typeof (T).GetProperties ()) {
					yield return p;
				}
			}
		}

		private static void LocalIterator ()
		{
			foreach (var m in Local<string, string> ()) { }

			static IEnumerable<object> Local<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
			{
				foreach (var m in typeof (T1).GetMethods ()) {
					yield return m;
				}
				foreach (var p in typeof (T2).GetProperties ()) {
					yield return p;
				}
			}
		}

		private static void IteratorCapture ()
		{
			Local1<string> ();
			void Local1<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1> ()
			{
				_ = Local2<string> ();
				IEnumerable<object> Local2<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
				{
					foreach (var m in typeof (T1).GetMethods ()) {
						yield return m;
					}
					foreach (var p in typeof (T2).GetProperties ()) {
						yield return p;
					}
				}
			}
		}

		private static void NestedIterators ()
		{
			Local1<string> ();
			IEnumerable<object> Local1<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1> ()
			{
				foreach (var o in Local2<string> ()) { yield return o; }
				IEnumerable<object> Local2<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
				{
					foreach (var m in typeof (T1).GetMethods ()) {
						yield return m;
					}
					foreach (var p in typeof (T2).GetProperties ()) {
						yield return p;
					}
				}
			}
		}

		private static void IteratorInsideClosure ()
		{
			Outer<string> ();
			IEnumerable<object> Outer<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1> ()
			{
				int x = 0;
				foreach (var o in Inner<string> ()) yield return o;
				IEnumerable<object> Inner<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
				{
					x++;
					foreach (var m in typeof (T1).GetMethods ()) yield return m;
					foreach (var p in typeof (T2).GetProperties ()) yield return p;
				}
			}
		}

		private static void IteratorInsideClosureMismatch ()
		{
			Outer<string> ();

			IEnumerable<object> Outer<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T1> ()
			{
				int x = 0;
				foreach (var o in Inner<string> ()) yield return o;

				[ExpectedWarning ("IL2090", "T1", "PublicMethods", CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL2090", "T2", "PublicProperties", CompilerGeneratedCode = true)]
				IEnumerable<object> Inner<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T2> ()
				{
					x++;
					foreach (var m in typeof (T1).GetMethods ()) yield return m;
					foreach (var p in typeof (T2).GetProperties ()) yield return p;
				}
			}

		}

		private static void Async ()
		{
			Local<string> ().Wait ();
			async Task Local<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				await Task.Delay (0);
				_ = typeof (T).GetMethods ();
			}
		}

		private static void AsyncCapture ()
		{
			Local1<string> ();
			void Local1<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1> ()
			{
				Local2<string> ().Wait ();
				async Task Local2<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
				{
					await Task.Delay (0);
					_ = typeof (T1).GetMethods ();
					await Task.Delay (0);
					_ = typeof (T2).GetProperties ();
				}
			}
		}

		[ExpectedWarning ("IL2090", nameof (DynamicallyAccessedMemberTypes.PublicProperties), CompilerGeneratedCode = true)]
		private static void AsyncTypeMismatch ()
		{
			_ = Local<string> ();

			static async Task Local<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				await Task.Delay (0);
				_ = typeof (T).GetMethods ();
				await Task.Delay (0);
				_ = typeof (T).GetProperties ();
			}
		}

		private static void AsyncInsideClosure ()
		{
			Outer<string> ();
			void Outer<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T1> ()
			{
				int x = 0;
				Inner<string> ().Wait ();
				async Task Inner<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T2> ()
				{
					await Task.Delay (0);
					x++;
					_ = typeof (T1).GetMethods ();
					_ = typeof (T2).GetProperties ();
				}
			}
		}

		private static void AsyncInsideClosureMismatch ()
		{
			Outer<string> ();

			void Outer<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T1> ()
			{
				int x = 0;
				Inner<string> ().Wait ();

				[ExpectedWarning ("IL2090", "T1", "PublicMethods", CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL2090", "T2", "PublicProperties", CompilerGeneratedCode = true)]
				async Task Inner<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T2> ()
				{
					await Task.Delay (0);
					x++;
					_ = typeof (T1).GetMethods ();
					_ = typeof (T2).GetProperties ();
				}
			}
		}

		private static void GlobalClosures ()
		{
			GlobalClosureClass<int>.M1<int> ();
			GlobalClosureClass<int>.M2<int> ();
		}

		private sealed class GlobalClosureClass<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
			public static void M1<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] U> ()
			{
				Func<string, Action> a = (string s) => () => Console.WriteLine (s + typeof (T).GetMethods ());
				Func<string, Action> b = (string s) =>
					// https://github.com/dotnet/linker/issues/2826
					[ExpectedWarning ("IL2090", "U", "PublicProperties", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				() => Console.WriteLine (s + typeof (U).GetProperties ());
				a ("");
				b ("");
			}
			public static void M2<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] U> ()
			{
				Func<string, Action> a = (string s) => () => Console.WriteLine (s + typeof (T).GetMethods ());
				Func<string, Action> b = (string s) =>
					// https://github.com/dotnet/linker/issues/2826
					[ExpectedWarning ("IL2090", "U", "PublicProperties", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				() => Console.WriteLine (s + typeof (U).GetProperties ());
				a ("");
				b ("");
			}
		}
	}
}
