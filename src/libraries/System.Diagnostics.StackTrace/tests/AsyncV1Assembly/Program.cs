namespace AsyncRecursiveTest;

public static class V1Methods
{
    public static async Task Test0(Func<int, Task> method)
    {
        await Test1(method);
        await Task.Yield();
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
        }

        Console.WriteLine(2 * i);
    }

    public static async Task Test2(int i)
    {
        Console.WriteLine($"In Test2 with {i}");
        Console.ReadKey();
        throw new NullReferenceException("Exception from Test2");
    }
}