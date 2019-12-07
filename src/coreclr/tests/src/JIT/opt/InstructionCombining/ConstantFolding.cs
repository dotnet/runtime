// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public class Program
{
    public static int Main(string[] args)
    {
        int failures = 0;
        failures += new ConstantFoldingTests1().RunTests();
        failures += new ConstantFoldingTests2().RunTests();
        failures += new ConstantFoldingTests3().RunTests();
        return 100 + failures;
    }
}