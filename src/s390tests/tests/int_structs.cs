using System;

public class S390xStructTest
{
    struct Point
    {
        public int x;
        public int y;
    }

    public static int s390xHw()
    {
        Point p;
        p.x = 10;
        p.y = 20;
        return  p.x;
    }
    public static int Main(){
        int result = s390xHw();
        return result == 10 ? 0 : 1;
    }
}

