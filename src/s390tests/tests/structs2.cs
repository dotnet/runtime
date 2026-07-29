using System;

public struct Point
{
    public int X;
    public int Y;
}

public struct Rectangle
{
    public Point TopLeft;
    public Point BottomRight;
}

public class S390xStructTest
{
    public static int s390xHw()
    {
        // Zero-initialize the entire struct
        Rectangle rect = new Rectangle();

        return rect.TopLeft.X +
               rect.TopLeft.Y +
               rect.BottomRight.X +
               rect.BottomRight.Y;
    }
}

public class Program
{
    public static int Main()
    {
        int result = S390xStructTest.s390xHw();
        Console.WriteLine(result);

        return result == 0 ? 0 : 1;
    }
}
