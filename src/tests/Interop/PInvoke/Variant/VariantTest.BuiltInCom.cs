// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;
using static VariantNative;

#pragma warning disable CS0612, CS0618
partial class Test
{
    public static int Main()
    {
        bool builtInComDisabled=false;
        var comConfig = AppContext.GetData("System.Runtime.InteropServices.BuiltInComInterop.IsSupported");
        if(comConfig != null && !bool.Parse(comConfig.ToString()))
        {
            builtInComDisabled=true;
        }

        Console.WriteLine($"Built-in COM Disabled?: {builtInComDisabled}");
        try
        {
            TestByValue(!builtInComDisabled);
            TestByRef(!builtInComDisabled);
            TestOut();
            TestFieldByValue(!builtInComDisabled);
            TestFieldByRef(!builtInComDisabled);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test failed: {e}");
            return 101;
        }
        return 100;
    }
}
