// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace DefaultNamespace
{
    using System;

    class TestClass
    {
        public static int Main(String[] args)
        {

            try
            {
                throw new Exception();
            }
            catch (Exception /*e1*/)
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception /*e2*/)
                {
                }
                finally
                {
                    try
                    {
                        throw new Exception();
                    }
                    catch (Exception /*e3*/)
                    {
                    }

                }
                return 100;
            }
        }
    };
};

