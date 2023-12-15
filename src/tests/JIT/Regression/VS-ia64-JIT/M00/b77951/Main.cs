// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Test_Main
{
    [Fact]
    public static int TestEntryPoint()
    {
        int[] arrayValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        Property prop = new Property(0);

        // Verify property.
        for (int ii = 0; ii < arrayValues.Length; ++ii)
        {
            if (prop.Item != arrayValues[ii])
                return Property.NULLDATA;

            prop.Item = ii + 1;
        }

        // Successful execution.        
        return 100;
    }
}
