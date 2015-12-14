

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DefaultNamespace
{
    public class Bug_Cb4270
    {
        public virtual bool runTest()
        {
            char c2 = ' ';

            Console.WriteLine("Next will fall into 'short' loop"); Console.Out.Flush();

            for (short aa = 0; aa <= 128; aa++)
            {
                Console.WriteLine("aa==" + aa); Console.Out.Flush();

                if (aa <= 127)
                    c2 = (char)aa;

                if (Char.IsWhiteSpace(c2) == true)
                {
                    Console.Write("IsWhitespace ::: ");

                    Console.WriteLine("(894f) Whitespace for  c2==" + c2);
                }
            }
            return true;
        }

        public static int Main(String[] args)
        {
            Console.WriteLine("Character.IsWhitespace() -- Bug_Cb4270 runTest started.");
            (new Bug_Cb4270()).runTest();
            return 100;
        }
    }
}