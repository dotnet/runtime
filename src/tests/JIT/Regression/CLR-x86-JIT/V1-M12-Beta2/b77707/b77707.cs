// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b77707
{
    using System;

    public class AA
    {
        public static Array Method1()
        {
            Array[] arr = new Array[1];
            try
            {
                return arr[0];
            }
            finally
            {
                throw new Exception();
            }
            return arr[0];
        }
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134975", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsWasmReadyToRun))]
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Method1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
