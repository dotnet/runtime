using System;
using System.Threading.Tasks;


public enum someEnum2 {
    aaa,
    bbb
}
class Tests {
    private static GenericEnumTest<someEnum2> test1 = new GenericEnumTest<someEnum2>();
    public static async Task<int> Main(string[] args)
    {
        int retVal = await test1.ThrowExceptionWithGeneric(someEnum2.aaa);
        return retVal;
    }
}

public class GenericEnumTest<T> where T : struct {
    public enum anEnum {
        val1,
        val2
    }

    public async Task<int> ThrowExceptionWithGeneric(T val) {
        try {
            await ThrowExceptionTaskReturn(val);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
        return 0;
    }

    public async Task<anEnum> ThrowExceptionTaskReturn(T val) {
        for (int i = 0; i < 3; i++) {
            Console.WriteLine("[ASY] " + (3 - i) + " " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var nulla = default(string[]);
        // Causes exception to fire, walking the stack trace causes nullpointer exception bug
        var vala = nulla[0];

        return anEnum.val1;
    }
}