using System;

public class S390xStructTest
{
    struct Point
    {
        public int x;
        public double y;
    }

    public static double s390xHw()
    {
        Point p;
        p.x = 10;
        p.y = 20.2;
        return  p.y;
    }
    public static int Main(){
        double result = s390xHw();
        return result == 20.2 ? 0 : 1;
    }
}


