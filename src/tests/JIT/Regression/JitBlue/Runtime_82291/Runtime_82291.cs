using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_82291 
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GetValueOrNull<T>()
    {
        if (default(T) != null)
         return true;
         
       return false;
    }    
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool GetValueOrNullSlow<T>()
    {
        if (default(T) != null)
         return false;
         
       return false;
    }    

    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void GetValueOrNullSlow2<T>()
    {
        if (default(T) != null)
         return;
         
       return;
    }   

    [Fact]
    public static void Test()
    {
       GetValueOrNull<int>();
       GetValueOrNullSlow<int>();
       GetValueOrNullSlow2<int>();
    } 
}