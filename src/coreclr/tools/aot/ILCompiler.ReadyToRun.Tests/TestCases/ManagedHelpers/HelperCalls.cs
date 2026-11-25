// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class HelperCalls
{
    public static int Add(int left, int right) => checked(left + right);

    public static int Divide(int dividend, int divisor) => dividend / divisor;

    public static int GetElement(int[] array, int index) => array[index];

    public static int Sum(int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            result = checked(result + i);
        }

        return result;
    }
}
