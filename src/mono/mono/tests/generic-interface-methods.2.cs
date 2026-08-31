using System.Collections.Generic;

public class ClassA {};

public interface IGen<T> {
	Dictionary<T,S> makeDict<S> ();
}

public class Gen<T> : IGen<T> {
	public Dictionary<T,S> makeDict<S> () {
		return new Dictionary <T,S> ();
	}
}

public class Gen2<T,S> {
	public Dictionary<T,S> makeDict (IGen<T> igt) {
		return igt.makeDict<S> ();
	}
}

public class main {
	public static int Main () {
		Gen<string> gs = new Gen<string> ();
		Gen2<string,object> g2so = new Gen2<string,object> ();
		Gen2<string,string> g2ss = new Gen2<string,string> ();
		Gen2<string,ClassA> g2sa = new Gen2<string,ClassA> ();
		Gen2<string,int> g2si = new Gen2<string,int> ();

		if (g2so.makeDict (gs).GetType () != typeof (Dictionary<string,object>))
			return 1;
		if (g2ss.makeDict (gs).GetType () != typeof (Dictionary<string,string>))
			return 1;
		if (g2sa.makeDict (gs).GetType () != typeof (Dictionary<string,ClassA>))
			return 1;
		if (g2si.makeDict (gs).GetType () != typeof (Dictionary<string,int>))
			return 1;

		return 0;
	}
}
