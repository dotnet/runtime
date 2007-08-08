using System;

public interface IX {}
public interface IY : IX {}

public class X : IX {
        public override string ToString () {
                return "X";
        }
}

public class Y : IY {
        public override string ToString () {
                return "Y";
        }
}

public interface IA {
        IX Prop { get; }
}

public interface IB : IA {
        new IY Prop { get; }
}

public interface IC : IB {
}

public class A : IA {

        IX IA.Prop {
                get { return new X (); }
        }
}

public class B : A, IA, IB {
        IX IA.Prop {
                get { return new Y (); }
        }

        IY IB.Prop {
                get { return new Y (); }
        }
}

public class C : B, IC {
}

class MainClass {
        static int Main(string[] args) {
                IC c = new C ();
                IX w = ((IA)c).Prop;
		if (w.ToString () == "Y") {
			return 0;
		} else {
			return 1;
		}
                Console.WriteLine (w.ToString ());
        }
}
