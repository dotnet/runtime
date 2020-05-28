using System;

// Regression test for bug #59235

public static class Program {
	delegate void MyDel (int i, int j);

	public static void Main (string[] args) {
		var o = new MyTarget ();
		Console.WriteLine ("Hashcode1: " + o.GetHashCode ());

		MyDel d = o.DoStuff;
		Console.WriteLine ("Hashcode2: " + d.GetHashCode ());
		Console.WriteLine ("Hashcode3: " + o.GetHashCode ());

		o.Dispose ();
		Console.WriteLine ("Hashcode4: " + d.GetHashCode ());
	}

	class MyTarget : IDisposable {
		public int counter = 0;
		bool avail = true;

		public void DoStuff (int i, int j) {
			counter += i + j;
		}

		public void Dispose () {
			avail = false;
		}

		public override int GetHashCode () {
			if (!avail)
				throw new ObjectDisposedException ("MyTarget is dead");
			return counter.GetHashCode ();
		}
	}
}
