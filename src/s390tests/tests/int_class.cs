using System;

public class S390xStructTest
{
    class Point
    {
        public int x;
        public long y;
    }

    public static int s390xHw()
    {
        Point p= new Point();
        p.x = 10;
        p.y = 20;
        return  p.x;
    }
    public static int Main(){
        int result = s390xHw();
        Console.WriteLine(result);
        return result == 10? 0 : 1;
    }
}

