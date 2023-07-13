// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;

    public class TestClass
    {
        [Fact]
        public static int TestEntryPoint()
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

