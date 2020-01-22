using System;

namespace Repro {
	public class Base {
		internal protected virtual int Test () {
			return 1;
		}
	}

	public class Generic<T> where T : Base {
		public int Run (T t) {
			return t.Test ();
		}
	}
}
