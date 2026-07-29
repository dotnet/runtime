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
        Rectangle rect = new Rectangle
        {
            TopLeft = new Point { X = 10, Y = 20 },
            BottomRight = new Point { X = 30, Y = 40 }
        };

        return rect.BottomRight.Y;
    }
}

public class Program
{
    public static int Main()
    {
        int result = S390xStructTest.s390xHw();
        Console.WriteLine(result);

        return result == 40 ? 0 : 1;
    }
}

