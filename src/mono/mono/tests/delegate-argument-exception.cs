using System;

class Test {
	public static int Main () {
		return new Test().CallCreateDelegate();
	}

	public int CallCreateDelegate() {
		try	{
			var m = typeof(Test).GetMethod("Foo");
			var a = (Action) Delegate.CreateDelegate(typeof(Action), this, m);
			a();
		}
		catch (ArgumentException) {
			return 0;
		}

		return 1;
	}

	public void Foo<T>()
	{
	}
}