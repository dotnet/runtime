using System;
using System.Reflection;

unsafe public class C {
    public int Value;

    static void Main()
    {
        C a = new C { Value = 12 };
        FieldInfo info = typeof(C).GetField("Value");
        TypedReference reference = __makeref(a);

        if (!(reference is TypedReference reference0)) 
            throw new Exception("TypedReference");

        info.SetValueDirect(reference0, 34);

        Console.WriteLine($"a.Value = {a.Value}");
        if (a.Value != 34)
            throw new Exception("SetValueDirect");

        int z = 56;
        if (CopyRefInt(ref z) != 56) 
            throw new Exception("ref z");

        Console.WriteLine("ok");
    }

    static int CopyRefInt(ref int z)
    {
        if (!(z is int z0)) 
            throw new Exception("CopyRefInt");
        return z0;
    }
}