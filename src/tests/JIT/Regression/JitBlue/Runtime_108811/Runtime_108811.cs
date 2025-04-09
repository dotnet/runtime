// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

public class Runtime_108811
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [Fact]
    public static int Test()
    {
        var hex = "3AE46205A50ED00E7612A91692552B7A";
        var key = "abcdef1234567890";
        var iv = "abcdef1234567890";
        var bytes = Convert.FromHexString(hex);
        int retval = 100;
        for (int i = 0; i < 200; i++)
        {
            var result = new Runtime_108811().Decrypt(bytes, key, iv, out _);
            Console.Write($"{result} ");
            if (result != 9)
            {
                retval = -1;
                break;
            }
            Thread.Sleep(10);
        }
        Console.WriteLine();
        return retval;
    }

    public int Decrypt(byte[] buffer, string key, string iv, out byte[] decryptedData)
    {
        int decryptedByteCount = 0;
        decryptedData = new byte[buffer.Length];

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.KeySize = 128;
        aes.Padding = PaddingMode.Zeros;

        var instPwdArray = Encoding.ASCII.GetBytes(key);
        var instSaltArray = Encoding.ASCII.GetBytes(iv);

        using (var decryptor = aes.CreateDecryptor(instPwdArray, instSaltArray))
        {
            using (var memoryStream = new MemoryStream(buffer))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    int read;
                    do
                    {
                        read = cryptoStream.Read(
                            decryptedData,
                            decryptedByteCount,
                            decryptedData.Length - decryptedByteCount);
                        decryptedByteCount += read;
                    } while (read != 0);
                }
            }
        }
        // Found the accurate length of decrypted data
        while (decryptedData[decryptedByteCount - 1] == 0 && decryptedByteCount > 0)
            decryptedByteCount--;
        return decryptedByteCount;
    }
}