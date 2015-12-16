// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class Test
{
    public static int Main()
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
