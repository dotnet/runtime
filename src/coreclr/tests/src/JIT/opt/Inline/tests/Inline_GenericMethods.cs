// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Inline_GenericMethods
{
    internal class Inline_GenericMethods
    {
        public static void GetType_NoInline<T>()
        {
            Console.WriteLine(typeof(T));
        }

        public static int Main()
        {
            try
            {
                GetType_NoInline<Inline_GenericMethods>();
                return 100;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 101;
            }
        }
    }
}
