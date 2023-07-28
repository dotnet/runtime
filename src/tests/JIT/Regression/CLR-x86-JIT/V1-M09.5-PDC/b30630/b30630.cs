// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;
    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            bool param3 = false;
            try
            {
                //do anything here...
            }
            finally
            {
                do
                {
                    //and here...
                } while (param3);
            }
            return 100;
        }
    }
}
