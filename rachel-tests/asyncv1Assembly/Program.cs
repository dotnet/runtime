namespace AsyncRecursiveTest;

public static class V1Methods
{
    public static async Task Test(Func<int, Task> method)
    {
        await Test1(method);
        await Task.Yield();
        // Console.ReadKey();
        // throw new Exception("Exception from Test");
    }

    public static async Task Test1(Func<int, Task> method)
    {
        const int i = 3;
        Console.WriteLine(i);
        try
        {
            await method(i);
        }
        catch (Exception ex) when (ex.Message.Contains("404"))
        {
            Console.WriteLine($"Caught exception in Test1 with: {ex}");
            // throw new NotImplementedException("Not implemented exception from Test1");
        }
        // finally
        // {
        //     Console.WriteLine("In finally block of Test1");
        //     throw new NotImplementedException("Exception from finally block of Test1");
        // }
        Console.WriteLine(2 * i);
    }

    public static async Task Test2(int i)
    {
        Console.WriteLine($"In Test2 with {i}");
        Console.ReadKey();
        throw new NullReferenceException("Exception from Test2");
        // await Test3(i);
    }

    // public static async Task Test3(int i)
    // {
    //     await Task.Yield();
    //     Console.WriteLine($"In Test3 with {i}");
    //     throw new Exception("Exception from Test3");
    // }
}