using System;

class CreateSpanTest
{
    static int Main()
    {
        ReadOnlySpan<int> intSpan = (ReadOnlySpan<int>)new int[]{25,15,35,25};
        int result = 0;
        foreach (int i in intSpan)
        {
            result += i;
        }
        return result;
    }
}