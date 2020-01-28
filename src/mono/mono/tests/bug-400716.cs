using System;
using System.Collections.Generic;
using System.Linq;

namespace Repro {
	public enum Bla {
		A,B
	}

	class Driver {

		static void TestEnumerator<K, T> ()
		{
			object obj = new K[10];
			IEnumerable<T> dd = (IEnumerable<T>)obj;
			var tx = dd.GetEnumerator ();
			IEnumerator<T> x = (IEnumerator<T>)tx;
			x.MoveNext ();
			T t = x.Current;
		}

		static void TestBadEnumerator<K, T> ()
		{
			try {
				object obj = new K[10];
				IEnumerable<T> dd = (IEnumerable<T>)obj;
				var tx = dd.GetEnumerator ();
				IEnumerator<T> x = (IEnumerator<T>)tx;
				x.MoveNext ();
				T t = x.Current;
				throw new Exception (string.Format ("An InvalidCastException should be thrown for {0} and {1}", typeof (K), typeof (T)));
			} catch (InvalidCastException) {

			}
		}

		public static int Main ()
		{
			TestEnumerator<byte, byte> ();
			TestEnumerator<byte, sbyte> ();
			TestEnumerator<sbyte, byte> ();
			TestEnumerator<sbyte, sbyte> ();

			TestEnumerator<int, int> ();
			TestEnumerator<int, uint> ();
			TestEnumerator<uint, int> ();
			TestEnumerator<uint, uint> ();

			TestEnumerator<Bla, Bla> ();
			TestEnumerator<Bla, int> ();

			TestEnumerator<byte[], byte[]> ();
			TestEnumerator<byte[], sbyte[]> ();
			TestEnumerator<byte[], ICollection<byte>> ();
			TestEnumerator<byte[], IEnumerable<byte>> ();
			TestEnumerator<byte[], IList<byte>> ();
			TestEnumerator<byte[], ICollection<sbyte>> ();
			TestEnumerator<byte[], IEnumerable<sbyte>> ();
			TestEnumerator<byte[], IList<sbyte>> ();
			TestEnumerator<byte[], object> ();
			TestEnumerator<byte[], Array> ();

			TestBadEnumerator<byte[], object[]> ();
			TestBadEnumerator<byte[], byte> ();
			TestBadEnumerator<byte[], sbyte> ();
			TestBadEnumerator<byte[], ICollection<object>> ();

			TestEnumerator<char[], char[]> ();
			TestEnumerator<char[], IEnumerable<char>> ();

			TestEnumerator<int[], IEnumerable<int>> ();
			TestEnumerator<int[], IEnumerable<uint>> ();

			return 0;
		}
	}
}

