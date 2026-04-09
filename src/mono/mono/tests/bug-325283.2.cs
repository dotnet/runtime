using System;

public interface IFoo
{
	void Tst();
}

public class Foo : IFoo
{
	public void Tst ()
	{
	}
}

public abstract class BusinessBase<TYPE> where TYPE : BusinessBase<TYPE>, new ()
{
	public static void Load<KEY> (KEY id)
	{
		TYPE instance = new TYPE ();
		instance = instance.DataSelect<KEY> (id);
	}

	protected abstract TYPE DataSelect<KEY> (KEY id);
}

public class Page : BusinessBase<Page>
{
	protected override Page DataSelect<Guid> (Guid k)
	{
		return new Page ();
	}

	public static void Test<T> (T t) where T : IFoo
	{
		t.Tst();
	}
}

class D
{
	static void Main ()
	{
		Page.Load<Guid> (new Guid ());
		Page.Test<Foo> (new Foo ());

	}
}

