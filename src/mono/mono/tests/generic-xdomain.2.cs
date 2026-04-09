using System;
using System.ComponentModel;
using System.Runtime.Remoting;

namespace Test {
	public class Test {
		static void Main ()
		{
			AppDomain domain = AppDomain.CreateDomain ("new-domain");
			domain.DoCallBack (Run);
			Type stType = typeof (Something<string, string>);
			Other<string, string> st = (Other<string, string>) domain.CreateInstanceAndUnwrap (stType.Assembly.FullName, stType.FullName);
			Console.WriteLine ("in main int: {0}", st.getInt ());
			Console.WriteLine ("in main types: {0}", st.getTypeNames<Test> ());
		}

	    	public static void Run ()
		{
		    DoRun<string, string>(new Something<string, string> ());
		}

	    	public static void DoRun<T1, T2> (Other<T1, T2> some)
	    	{
			Console.WriteLine ("domain: {0}", AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine ("This is null: {0}", some.Mappings == null);
			Console.WriteLine ("int: {0}", some.getInt ());
		}
	}

	public class Other<T1, T2> : MarshalByRefObject {
		public T2 Mappings {
			get { return default(T2); }
		}

		public virtual int getInt () {
			return 123;
		}

		public virtual string getTypeNames<T3> () {
			return "error";
		}
	}

	public class Something<T1, T2> : Other<T1,T2> {
		public override int getInt () {
			return 456;
		}

		public override string getTypeNames<T3> () {
			Console.WriteLine ("getTypeNames in {0}", AppDomain.CurrentDomain.FriendlyName);
			return typeof(T1).ToString () + " " + typeof(T2).ToString () + " " + typeof (T3).ToString ();
		}
	}
}
