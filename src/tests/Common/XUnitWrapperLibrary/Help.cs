// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XUnitWrapperLibrary;

public static class Help
{
    public static void WriteHelpText()
    {
        string help = """
        Usage: XUnitWrapper [filter] [options]

        filter can be one of the following:
          * Fully qualified method name: Namespace.ClassName.MethodName
          * Substring of fully qualified method name: ClassName.MethodName or MethodName
          * Display Name full string: DisplayName=ArrayMarshalling/SafeArray/SafeArrayTest/SafeArrayTest.dll
          * Display Name substring: DisplayName~SafeArrayTest.dll
          * NOTE: This is a subset of the dotnet test filter syntax, documented at https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit

        Display Names:
          * For a given test, the display name can be one of the following:
            - For assemblies containing a single [Fact] test method, the display name is the assembly name.
            - For [Theory] tests, the display name is the fully qualified method name with the parameters for the test case.
            - If the parameters are computed at runtime, the display name will be the fully qualified method name.
            - For other tests, the display name is the fully qualified method name.

        Options:
            --help, -h, /?, -?       Display this help text.
            --exclusion-list=FILE
                                     Path to a file containing a list of tests to exclude.
                                     Each line in the file should be in the following format:
                                        displayName, reason
                                     (In the dotnet/runtime repository, this file is typically generated as part of the build process as 'src/tests/issues.targets'.)
                                     If you are running tests outside the repository context, you may need to create your own exclusion list file in the format shown above.
        """;

        Console.WriteLine(help);
    }
}
