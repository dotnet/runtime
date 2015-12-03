// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
namespace DefaultNamespace
{
    internal class Bug31
    {
        public static int Main(String[] args)
        {
            Console.WriteLine("abc");
            try
            {
                Directory.CreateDirectory("EraseThisDir");
            }
            catch (Exception)
            {
            }
            Console.WriteLine("xyz");
            return 100;
        }
    }
}
