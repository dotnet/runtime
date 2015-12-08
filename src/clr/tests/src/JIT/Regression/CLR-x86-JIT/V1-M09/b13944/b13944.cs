

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DefaultNamespace
{
    internal class X
    {
        public virtual int Blah(int what)
        {
            try
            {
                throw new Exception();
            }
            catch (Exception)
            {
            }
            finally
            {
            }


            return -1;
        }


        public static int Main(String[] argv)
        {
            int i = 0;
            X x = new X();


            i = x.Blah(1);
            return 100;
        }
    }
}
