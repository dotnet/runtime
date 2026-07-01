using System;

public class S390xStructTest
{
    struct Point
    {
        public int x;
        public long y;
    }

    public static long s390xHw()
    {
        Point p;
        p.x = 10;
        p.y = 20;
        return  p.y;
    }
    public static int Main(){
        long result = s390xHw();
        return result == 20 ? 0 : 1;
    }
}

