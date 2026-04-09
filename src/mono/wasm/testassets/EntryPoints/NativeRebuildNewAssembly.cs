using System;
using System.Security.Cryptography;
using System.Text;
public class Test
{
    public static int Main()
    {
        string input = "Hello, world!";
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            Console.WriteLine($"Hash of {input}: {Convert.ToBase64String(hashBytes)}");
        }
        return 42;
    }
}
