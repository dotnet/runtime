using System;
using System.Threading;

struct Gen
{
    public void Target() {}
}

public class Test
{
    public static int Main()
    {
        Thread[] threads = new Thread[1];
        Gen obj = new Gen();
        threads[0] = new Thread(new ThreadStart(obj.Target));
        return 100;
    }
}
