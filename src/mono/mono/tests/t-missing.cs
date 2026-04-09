using System;

namespace Missing {

#if FOUND
public class Foo1 {
	public class InnerFoo {}
}
#endif

public class Foo2 {

	public Foo2 () {
	}

#if FOUND
	public Foo2 (int i) {
	}

	public void missing () {
	}

	public static void static_missing () {
	}
#endif
}


public class Foo3 {
#if FOUND
	public static int i;
#endif
}

public class Foo4 {
#if FOUND
	public int i;
#endif
}

public class Foo5 {
#if FOUND
	public virtual void missing_virtual () {
	}
#endif
}

#if FOUND
public struct Foo6 {
}
#endif	

}
