using System;
using System.Runtime.InteropServices;

public class Tests
{
    public static int Main () {
        try {
            var del = (Func<string, string>)Delegate.CreateDelegate (typeof (Func<string, string>), null, typeof (object).GetMethod ("ToString"));
            Console.WriteLine (del ("FOO"));
        } catch(Exception e) {
            Console.WriteLine(e);
            return 0;
        }
        
        return 100;
        
    }
}