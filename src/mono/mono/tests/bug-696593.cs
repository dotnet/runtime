using System;

public class Program {
	public static int Main(string[] args) {
		new Class3();
		return 0;
	}
}

public interface Interface {
	void Method<T>(T arg);
}

public abstract class Class1 : Interface {
	public abstract void Method<T>(T arg);
}

public abstract class Class2<T> : Class1 {
}

public class Class3 : Class2<object> {
	public override void Method<T>(T arg) {}
}
