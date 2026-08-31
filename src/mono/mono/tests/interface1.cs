namespace intf {
	public class A {
		public virtual int method () {
			return 1;
		}
	}
	public interface B {
		int method ();
	}
	public class C : A, B {
		
		static int Main() {
			C c = new C ();
			if (c.method() != 1)
				return 1;
			return 0;
		}
	}
}
