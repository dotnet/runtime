/*
 * The string portion of this test crashes on Boehm (and there might
 * not be much we can do about that - I haven't looked into it), and
 * the array portions are unbearably slow, so it's only run on SGen.
 */

using System;

class X
{
	public static void Test (Action<int> allocator)
	{
		for (int i = 0; i < 1000; ++i)
		{
			bool caught = false;
			try
			{
				//Console.WriteLine ("allocating with " + i);
				allocator (i);
			}
			catch (OutOfMemoryException)
			{
				caught = true;
			}
			/*
			if (!caught)
			{
				Console.WriteLine ("WTF?");
				//Environment.Exit (1);
			}
			*/
		}
	}

	public static void Ignore<T> (T x)
	{
	}

	public static void ProbeArray<T> (T[] a)
	{
		if (a == null)
			return;

		for (int i = 0; i < 1000; ++i)
		{
			a [i] = default (T);
			a [a.Length - i - 1] = default (T);
		}
	}

	public static void ProbeString (string s)
	{
		for (int i = 0; i < 1000; ++i)
		{
			if (s [s.Length - i - 1] != ' ')
				Environment.Exit (1);
		}
	}

	public static int Main ()
	{
		Console.WriteLine ("byte arrays");
		Test (i => ProbeArray (new byte [int.MaxValue - i]));
		Test (i => ProbeArray (new byte [int.MaxValue - i * 100]));

		Console.WriteLine ("int arrays");
		Test (i => ProbeArray (new int [int.MaxValue / 4 - i]));
		Test (i => ProbeArray (new int [int.MaxValue / 4 - i * 100]));

		Console.WriteLine ("large int arrays");
		Test (i => ProbeArray (new int [int.MaxValue - i]));
		Test (i => ProbeArray (new int [int.MaxValue - i * 100]));

		// FIXME: This commit 4gb of memory
		/*
		Console.WriteLine ("strings");
		Test (i => ProbeString ("abcd".PadRight(int.MaxValue - i)));
		Test (i => ProbeString ("abcd".PadRight(int.MaxValue - i * 100)));
		*/

		//Console.WriteLine ("no objects allocated - all good");
		return 0;
	}
}
