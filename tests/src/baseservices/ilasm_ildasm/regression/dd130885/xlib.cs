using System;
public struct myDateTime : IEquatable<myDateTime>
{
    public UInt64 dateData;
    public override string ToString()
    {
         return "myDateTime";
    }
    public bool Equals(myDateTime d)
    {
         return dateData == d.dateData;
    }
    public void InstanceMethod()
    {
        Console.WriteLine("InstanceMethod");
    }
    public void GenericMethod<T>()
    {
        Console.WriteLine(typeof(T));
    }
}
