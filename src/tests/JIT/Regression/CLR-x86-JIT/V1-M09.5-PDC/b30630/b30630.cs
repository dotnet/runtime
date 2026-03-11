// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b30630
{
    using System;
    public class App
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
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
        }
    }
}
