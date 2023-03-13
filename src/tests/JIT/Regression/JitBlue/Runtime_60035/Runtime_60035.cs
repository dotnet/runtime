// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Text.Encodings.Web;

namespace Runtime_60035
{
    public class Program
    {
        public static int Main()
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes("https://github.com/dotnet/runtime");
            Console.WriteLine(UrlEncoder.Default.FindFirstCharacterToEncodeUtf8(inputBytes));
            return 100;
        }
    }
}
