using System;
using System.Reflection;

public class EventSink
{
    static public void Click(int x, int y)
    {
        Console.WriteLine("[User Event Handler] Event called with " + x + ":" + y);
    }
}