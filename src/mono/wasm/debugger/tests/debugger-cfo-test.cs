using System;

namespace DebuggerTests
{
	public class CallFunctionOnTest {
		public static void LocalsTest (int len)
		{
			var big = new int[len];
			for (int i = 0; i < len; i ++)
				big [i] = i + 1000;

			var simple_struct = new Math.SimpleStruct () { dt = new DateTime (2020, 1, 2, 3, 4, 5), gs = new Math.GenericStruct<DateTime> { StringField = $"simple_struct # gs # StringField" } };

			var ss_arr = new Math.SimpleStruct [len];
			for (int i = 0; i < len; i ++)
				ss_arr [i] = new Math.SimpleStruct () { dt = new DateTime (2020+i, 1, 2, 3, 4, 5), gs = new Math.GenericStruct<DateTime> { StringField = $"ss_arr # {i} # gs # StringField" } };

			var nim = new Math.NestedInMath { SimpleStructProperty = new Math.SimpleStruct () { dt = new DateTime (2010, 6, 7, 8, 9, 10) } };
			Action<Math.GenericStruct<int[]>> action = Math.DelegateTargetWithVoidReturn;
			Console.WriteLine("foo");
		}

		public static void PropertyGettersTest ()
		{
			var ptd = new ClassWithProperties { DTAutoProperty = new DateTime (4, 5, 6, 7, 8, 9) };
			var swp = new StructWithProperties ();
			System.Console.WriteLine("break here");
		}

		public static async System.Threading.Tasks.Task PropertyGettersTestAsync ()
		{
			var ptd = new ClassWithProperties { DTAutoProperty = new DateTime (4, 5, 6, 7, 8, 9) };
			var swp = new StructWithProperties ();
			System.Console.WriteLine("break here");
			await System.Threading.Tasks.Task.CompletedTask;
		}
	}

	class ClassWithProperties
	{
		public int Int { get { return 5; } }
		public string String { get { return "foobar"; } }
		public DateTime DT { get { return new DateTime (3, 4, 5, 6, 7, 8); } }

		public int[] IntArray { get { return new int[] { 10, 20 }; } }
		public DateTime[] DTArray { get { return new DateTime[] { new DateTime (6, 7, 8, 9, 10, 11), new DateTime (1, 2, 3, 4, 5, 6) }; }}
		public DateTime DTAutoProperty { get; set; }
		public string StringField;
	}

	struct StructWithProperties
	{
		public int Int { get { return 5; } }
		public string String { get { return "foobar"; } }
		public DateTime DT { get { return new DateTime (3, 4, 5, 6, 7, 8); } }

		public int[] IntArray { get { return new int[] { 10, 20 }; } }
		public DateTime[] DTArray { get { return new DateTime[] { new DateTime (6, 7, 8, 9, 10, 11), new DateTime (1, 2, 3, 4, 5, 6) }; }}
	}
}
