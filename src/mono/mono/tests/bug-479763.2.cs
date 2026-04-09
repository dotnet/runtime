using System;

public class Foo
{
    public event EventHandler Event;

    public void RaiseEvent()
    {
        Event(this, new EventArgs());
    }

    public void AddHandler<T>(string target)
    {
	    Action<object, EventArgs> fn = (sender, e) => Console.WriteLine(target);
	    EventHandler handler = Delegate.CreateDelegate(typeof(EventHandler),
			    fn.Target, fn.Method) as EventHandler;

	    Event += handler;
    }
}

public static class Program
{
    public static void Main()
    {
        var thing = new Foo();

        thing.AddHandler<Type>("hello");
        thing.RaiseEvent();
        thing.AddHandler<Type>("there");
    }
}
