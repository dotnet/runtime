// using System.Runtime.CompilerServices;

// class Program
// {
//     static volatile uint _debuggerAttached = 0xDEADBEEF;
//     static void Main()
//     {
//         AsyncMain();
//     }

//     static void AsyncMain()
//     {
//         RecursiveMethod(10).GetAwaiter().GetResult();
//     }

//     [MethodImpl(MethodImplOptions.NoInlining)]
//     static unsafe void AttachWarning()
//     {
//         Console.WriteLine($"Attach debugger now to {Environment.ProcessId}!");
//         nint ptr = (nint)Unsafe.AsPointer(ref _debuggerAttached);
//         Console.WriteLine($"Debugger flag at address: {ptr:X}");
//         while (_debuggerAttached == 0xDEADBEEF)
//         {
//             Thread.Sleep(10);
//         }
//         _debuggerAttached = 0xDEADBEEF;
//     }

//     static async Task<int> RecursiveMethod(int count)
//     {
//         Console.WriteLine($"Forward Count: {count}");
//         if (count != 0)
//         {
//             int result = await RecursiveMethod(count - 1);

//             Console.WriteLine($"Return Count: {result}");
//             if (count == 1)
//             {
//                 AttachWarning();
//             }
//             return count * 2;
//         }

//         Console.WriteLine("Base case reached.");
//         AttachWarning();

//         await Task.Yield();

//         Console.WriteLine("Returning from base case.");
//         AttachWarning();

//         return -1;
//     }
// }

namespace AsyncRecursiveTest;

public class Program
{
    static async Task Main()
    {
        await Task.Yield();
        try
        {
        // await MyMethod2(3);
            await V1Methods.Test(MyMethod);
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"Caught exception: {ex}");
        }
    }

    private static async Task<int> MyMethod2(int i)
    {
        await Task.Yield();
        try
        {
            await MyMethod(i);
            return i * 2;
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"Caught exception in MyMethod2 with: {ex}");
            return -1;
        }
    }

    private static async Task<int> MyMethod(int i)
    {
        try
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            for (int j = i; j > 0; j--)
            {
                await MyMethod(j - 1);
            }
            if (i == 0) await V1Methods.Test2(i);
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Caught exception in MyMethod with: {ex}");
            // }
        }
        finally
        {
            Console.WriteLine($"In finally block of MyMethod with {i}");
            if (i == 2) throw new NotImplementedException("Not Found from MyMethod");
            // throw new NotImplementedException("Exception from finally block of MyMethod");
        }

        Console.WriteLine(3 * i);
        return i * 2;
    }
}