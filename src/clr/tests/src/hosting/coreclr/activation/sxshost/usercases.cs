using System;
using System.Collections;
using System.Collections.Generic;

// The class UserCases.Calls contains (as static members) transparent methods to be used
// as calls at the base of a SxS stack in SameStackHost2 tests. To select method Foo from
// this file, the first argument to SameStackHost2 should be: UserCases_Calls_Foo.
class Calls
{
	static int Return100()
	{
		Console.WriteLine("Returning 100...");
		return 100;
	}

    static int ThrowManaged()
    {
        Console.WriteLine("Throwing managed exception..");
        throw new NotSupportedException("blagh");
        return 100;
    }
    
    static int InterfaceThrow()
    {
        Console.WriteLine("Interface call to null object");
        Array a = null;
        IList<int> l = (IList<int>)a;
        l.Clear();
        return 100;
    }
}