using System;
using System.Security.Cryptography;

public class Test
{
    public static int Main()
    {
        using (SHA256 mySHA256 = SHA256.Create())
        {
            byte[] data = { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
            byte[] hashed = mySHA256.ComputeHash(data);
            string asStr = string.Join(' ', hashed);
            Console.WriteLine("TestOutput -> Hashed: " + asStr);
            return 0;
        }
    }
}
