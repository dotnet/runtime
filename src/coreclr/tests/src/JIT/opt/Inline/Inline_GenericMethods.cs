// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Inline_GenericMethods
{
    class Inline_GenericMethods
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
