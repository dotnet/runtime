using System;

public class S390xStructTest
{
    struct Point
    {
        public int x;
        public float y;
    }

    public static float s390xHw()
    {
        Point p;
        p.x = 10;
        p.y = 20.2f;
        return  p.y;
    }
    public static int Main(){
        float result = s390xHw();
        return result == 20.2f ? 0 : 1;
    }
}


