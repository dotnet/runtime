// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Inline_GenericMethods
{
    public class Inline_GenericMethods
    {
        internal static void GetType_NoInline<T>()
        {
            Console.WriteLine(typeof(T));
        }

        [Fact]
        public static int TestEntryPoint()
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
