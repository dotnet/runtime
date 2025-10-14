
namespace AsyncTest;

public class Program
{
    // v2 -> v1 -> v2 -> v1
    static async Task Foo()
    {
        await Task.Yield();
        try
        {
            await V1Methods.Test0(Foo1);
        }
        catch (NotImplementedException ex)
        {
            throw;
        }
    }

    private static async Task<int> Foo1(int i)
    {
        await Task.Yield();
        try
        {
            await Foo2(i);
            return i * 2;
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"Caught exception in MyMethod2 with: {ex}");
            return -1;
        }
    }

    private static async Task<int> Foo2(int i)
    {
        try
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            for (int j = i; j > 0; j--)
            {
                await Foo2(j - 1);
            }
            if (i == 0) await V1Methods.Test2(i);
        }
        finally
        {
            Console.WriteLine($"In finally block of Foo2 with {i}");
            if (i == 2) throw new NotImplementedException("Not Found from Foo2");
        }
        return i * 2;
    }

    static async Task Bar(int i)
    {
        if (i == 0)
            throw new Exception("Exception from Bar");
        await Bar(i - 1);
    }

    static async Task<int> Baz()
    {
        throw new Exception("Exception from Baz");
    }

    static async Task<int> Moin()
    {
        var task = Moin2();
        Console.WriteLine("hello from Moin");
        return await task;
    }

    private static async Task<int> Moin2()
    {
        await Task.Delay(100);
        throw new Exception("Exception from Moin2");
    }
    
    static async Task<int> Qux(int i)
    {
        await Task.Yield();
        try
        {
            return 9 / i;
        }
        catch (DivideByZeroException ex)
        {
            throw new DivideByZeroException("Exception from Qux");
        }
    }
}