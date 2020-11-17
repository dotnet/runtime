// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;


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
