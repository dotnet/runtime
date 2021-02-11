using System;

public class Program
{
    public static int Main()
    {
        System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom("module-cctor.exe");
        Console.WriteLine("assembly loaded");
        Type type = asm.GetType("NS.TestClass", true);
        Console.WriteLine("got type 'NS.TestClass'");

        System.Reflection.FieldInfo field = type.GetField("TestField");
        Console.WriteLine("about to get value of 'TestField'");
        Console.WriteLine("got field 'TestField'");
	int val = (int)field.GetValue(null);
        Console.WriteLine("Value of field: " + val);
	if (val == 1)
		return 0;
	return 1;
    }
}


