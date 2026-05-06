using System;

public class Gen<T> {}

static class Stuff
{
    public static Type GetOpenType <T> ()
    {
	return typeof (Gen<>);
    }

    public static Type GetClosedType <T> ()
    {
	return typeof (Gen<T>);
    }

    static int Main (string[] args)
    {
	if (GetOpenType<string> () != typeof (Gen<>))
	    return 1;
	if (GetClosedType<string> () != typeof (Gen<string>))
	    return 1;
	return 0;
    }
}
