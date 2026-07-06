using System;

public class S390xStructTest
{
    class Point
    {
        public double x;
        public float y;
    }

    public static float s390xHw()
    {
        Point p= new Point();
        p.x = 10.2;
        p.y = 20.4f;
        return  p.y;
    }
    public static int Main(){
        float result = s390xHw();
        Console.WriteLine(result);
        return result == 20.4f ? 0 : 1;
    }
}

