using System;

class Program
{
    public static int Main()
    {
        new G<L1>().Test();
        new G<L2>().Test();
        return 100;
    }
}

public interface ILogic{
    abstract static void Run();
}

public class L1 : ILogic{
    public static void Run(){
        Console.WriteLine("L1");
    }
}

public class L2 : ILogic{
    public static void Run(){
        Console.WriteLine("L2");
    }
}

struct G<T> where T : ILogic
{
    public void Test()
    {
        T.Run();
    }
}
