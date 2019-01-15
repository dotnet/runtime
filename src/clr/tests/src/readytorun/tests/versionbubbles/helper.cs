using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class Helper
{
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public string GetLastMethodName()
    {
        StackTrace st = new StackTrace();
        return st.GetFrame(0).GetMethod().Name;
    }
}
