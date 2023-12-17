// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.InlineArrays
{
	[ExpectedNoWarnings]
	public class InlineArray
	{
		public static void Main()
		{
			InlineArrayUsage.Test ();
			CollectionLiteralsOfArrays.Test ();
		}

		[Kept]
		class InlineArrayUsage
		{
			// NativeAOT will remove most of the struct type information as it's not needed
			// in the generated native code. Eventually we might come up with a better test infra to validate this.
			[Kept (By = Tool.Trimmer)]
			public struct StructWithFixedBuffer
			{
				[Kept (By = Tool.Trimmer)]
				public FixedBuffer Buffer;

				[Kept (By = Tool.Trimmer)]
				[KeptAttributeAttribute (typeof(InlineArrayAttribute), By = Tool.Trimmer)]
				[InlineArray (8)]
				public partial struct FixedBuffer
				{
					[Kept (By = Tool.Trimmer)]
					public int e0;
				}
			}

			[Kept (By = Tool.Trimmer)]
			public struct StructWithAutoLayoutBuffer
			{
				[Kept (By = Tool.Trimmer)]
				public AutoLayoutBuffer Buffer;

				[Kept (By = Tool.Trimmer)]
				[KeptAttributeAttribute (typeof (InlineArrayAttribute), By = Tool.Trimmer)]
				[InlineArray (8)]
				[StructLayout (LayoutKind.Auto)]
				public struct AutoLayoutBuffer
				{
					[Kept (By = Tool.Trimmer)]
					public int e0;
				}
			}

			[Kept]
			public static void Test ()
			{
				var s = new StructWithFixedBuffer ();
				s.Buffer[0] = 5;

				var sa = new StructWithAutoLayoutBuffer ();
				_ = sa.Buffer[1];
			}
		}

		[Kept]
		[KeptMember (".cctor()")]
		class CollectionLiteralsOfArrays
		{
			[Kept]
			public static readonly ImmutableArray<string> ImmutableValues = ["one", "two"];
			[Kept]
			public static readonly string[] ArrayValues = ["one", "two"];

			[Kept]
			public static void Test()
			{
				_ = CollectionLiteralsOfArrays.ImmutableValues[0];
				_ = CollectionLiteralsOfArrays.ArrayValues[1];
			}
		}
	}
}
