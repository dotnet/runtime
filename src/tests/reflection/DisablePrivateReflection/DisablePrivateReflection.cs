// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

[assembly: TypeMapAssemblyTarget<PrivateReflectionUniverse>("DisablePrivateReflectionLibrary")]

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        DisablePrivateReflectionTests.Run();
        DisablePrivateReflectionConsumer.Run();
        return DisablePrivateReflectionTests.RunTypeMap();
    }
}
