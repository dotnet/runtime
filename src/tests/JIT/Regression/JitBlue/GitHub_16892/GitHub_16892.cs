using System;
using System.Runtime.CompilerServices;
using Xunit;
//
//  Test case for a GC Stress 4 failure
//
//  This test was failing during a GC Stress 4 run in the method Test(...)
//
//  The failure requires that this test be built with Debug codegen
//
//  The GC Stress failure will occur if the JIT
//     1. has evaluated and stored the two outgoing stack based arguments: a5, a6
//     2. and then performs a call to the helper CORINFO_HELP_RNGCHKFAIL
//
//  With the fix the JIT will evaluate the arr[3] with the rangecheck 
//  into a new compiler temp, before storing any outgoing arguments.
//

class Item
{
    int _value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Item(int value)  { _value = value; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetValue() { return _value; }
}

public class Program
{
    Item[] itemArray;

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Init()
    {
        itemArray = new Item[11];
        for (int i=0; i<11; i++)
        {
            itemArray[i] = new Item(i);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Compute(Item r1, Item r2, Item r3, Item r4, Item s5, Item s6)
    {
        int result = r1.GetValue();
        result += r2.GetValue();
        result += r3.GetValue();
        result += r4.GetValue();
        result += s5.GetValue();
        result += s6.GetValue();
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    int Test(Item a1, Item a2, Item a4, Item a5, Item a6)
    {
        Item[] arr = itemArray;
        int result = 0;

        // Insure that we have to generate fully interruptible GC information
        // Form a possible infinte loop that the JIT believes could execute
        // without encountering a GC safe point.
        //
        do {
            if (result < 5)
            {
                result = Compute(a1, a2, arr[3], a4, a5, a6);
            }
        } while (result < 0);

        return result;
            
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Program prog = new Program();

        prog.Init();

        Item[] arr = prog.itemArray;

        Item obj1 = arr[1];
        Item obj2 = arr[2];
        Item obj3 = arr[3];
        Item obj4 = arr[4];
        Item obj5 = arr[5];
        Item obj6 = arr[6];

        int result = prog.Test(obj1, obj2, obj4, obj5, obj6);

        if (result == 21)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed");
            return -1;
        }
    }
}
