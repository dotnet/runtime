using System;

class PinList
{
    class Pinned
    {
	public Pinned ()
	{
	}
    }

    Pinned reference;
    PinList next;

    PinList (PinList n)
    {
	next = n;
    }

    static PinList MakeList (int length)
    {
	PinList l = null;
	for (int i = 0; i < length; ++i)
	    l = new PinList (l);
	return l;
    }

    static void AssignReferences (PinList l, Pinned[] objs)
    {
	int i = 0;
	int n = objs.Length;
	while (l != null)
	{
	    l.reference = objs [i++ % n];
	    l = l.next;
	}
    }

    static Pinned Work (PinList list, Pinned[] objs, int i)
    {
	if (i >= objs.Length)
	{
	    for (int j = 0; j < 10; ++j)
	    {
		MakeList (1 << 19);
		AssignReferences (list, objs);
	    }
	    return null;
	}
	else
	{
	    Pinned obj = new Pinned ();
	    objs [i] = obj;
	    Pinned dummy = Work (list, objs, i + 1);
	    return obj != dummy ? obj : dummy; // to keep obj alive
	}
    }

    static void Benchmark (PinList list, int n)
    {
	Pinned[] objs = new Pinned [n];
	Work (list, objs, 0);
    }

    public static void Main ()
    {
	PinList list = MakeList (1 << 24);
	Console.WriteLine ("long list constructed");
	Benchmark (list, 10);
	GC.Collect (1);
	Benchmark (list, 100);
    }
}
