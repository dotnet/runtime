using System;

public class S390xStructTest
{
    class Point
    {
        public double x;
        public float y;
    }

    public static double s390xHw()
    {
        Point p= new Point();
        p.x = 10.2;
        p.y = 20.4f;
        return  p.x;
    }
    public static int Main(){
        double result = s390xHw();
        Console.WriteLine(result);
        return result == 10.2 ? 0 : 1;
    }
}

