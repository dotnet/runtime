// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

