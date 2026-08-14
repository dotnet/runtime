using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

        // Also reference System.Text.RegularExpressions, which is not otherwise part of
        // the test app's closure, so that rebuilding pulls in a *new* assembly. This
        // ensures native artifacts that depend on the set of assemblies (e.g. the AOT
        // modules table driver-gen.c) are regenerated on rebuild.
        bool isMatch = Regex.IsMatch(input, "[A-Za-z]+");
        Console.WriteLine($"Input '{input}' matches: {isMatch}");
        return 42;
    }
}
