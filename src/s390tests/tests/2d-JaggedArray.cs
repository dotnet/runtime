using System;

public class S390xArrayTest
{
    public static int s390xHw()
    {
        int[][] matrix =
        {
            new int[] { 1, 2, 3 },
            new int[] { 4, 5, 6 },
            new int[] { 7, 8, 9 }
        };

        return matrix[1][2];
    }
}

public class Program
{
    public static int Main()
    {
        int result = S390xArrayTest.s390xHw();
        Console.WriteLine(result);

        return result == 6 ? 0 : 1;
    }
}
