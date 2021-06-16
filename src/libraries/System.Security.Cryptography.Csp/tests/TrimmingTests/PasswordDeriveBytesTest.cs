using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

/// <summary>
/// Tests that using PasswordDeriveBytes without specifying a hash algorithm name
/// works correctly in a trimmed application.
/// </summary>
class Program
{
    static int Main()
    {
        string testPassword = "PasswordGoesHere";
        byte[] testSalt = new byte[] { 9, 5, 5, 5, 1, 2, 1, 2 };

        byte[] expected = HexToByteArray("12F2497EC3EB78B0EA32AABFD8B9515FBC800BEEB6316A4DDF4EA62518341488A116DA3BBC26C685");

        using (var deriveBytes = new PasswordDeriveBytes(testPassword, testSalt))
        {
            byte[] output = deriveBytes.GetBytes(expected.Length);
            if (output.SequenceEqual(expected))
            {
                return 100;
            }
        }

        return -1;
    }

    private static byte[] HexToByteArray(string hexString)
    {
        byte[] bytes = new byte[hexString.Length / 2];

        for (int i = 0; i < hexString.Length; i += 2)
        {
            ReadOnlySpan<char> s = hexString.AsSpan(i, 2);
            bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
        }

        return bytes;
    }
}
