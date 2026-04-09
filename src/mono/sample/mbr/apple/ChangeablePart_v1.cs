using System;

public class ChangeablePart
{
    public static int UpdateCounter (ref int counter)
    {
        return --counter;
    }
}
