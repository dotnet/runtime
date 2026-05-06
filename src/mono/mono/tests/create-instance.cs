using System;
using System.Reflection;

class T {

	static int Main() {
		Assembly a = AppDomain.CurrentDomain.Load ("mscorlib");
		object o = a.CreateInstance ("System.Exception");
		if (o == null)
			return 1;
		if (!(o is System.Exception))
			return 2;
		object[] args = new object [2];
		args [0] = 'X';
		args [1] = 10;
		o = Activator.CreateInstance (typeof (System.String), args);
		if (o == null)
			return 3;
		if (!(o is System.String))
			return 4;
		if (!"XXXXXXXXXX".Equals (o)) {
			Console.WriteLine ("got: {0}", o);
			return 5;
		}
		return 0;
	}
}
