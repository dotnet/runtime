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

    static int list_size = 0;

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
		MakeList (list_size >> 5);
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
	list_size = 1 << 15;
	TestTimeout timeout = TestTimeout.Start(TimeSpan.FromSeconds(TestTimeout.IsStressTest ? 60 : 5));

	for (int it1 = 1; it1 <= 10; it1++, list_size <<= 1) {
		PinList list = MakeList (list_size);
		Console.WriteLine ("long list constructed {0}", it1);
		for (int it2 = 0; it2 < 5; it2++) {
			Benchmark (list, 10 * it1);
			GC.Collect (1);

			if (!timeout.HaveTimeLeft)
				return;
		}
	}
    }
}
