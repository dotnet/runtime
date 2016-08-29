using System;
using System.Collections.Generic;

class ReferenceType
{
    internal byte Value;
    public ReferenceType(byte value) { Value = value; }
}

class My
{
    static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            Console.WriteLine(message);
            Environment.Exit(1);
        }
    }

    static int Sum(Span<int> span)
    {
        int sum = 0;
        for (int i = 0; i < span.Length; i++)
            sum += span[i];
        return sum;
    }

    static void TestSum()
    {
        int[] a = new int[] { 1, 2, 3, 4 };
        Span<int> span = new Span<int>(a);
        AssertTrue(Sum(span) == 10, "Unexpected sum of array");
        Span<int> slice = span.Slice(1, 2);
        AssertTrue(Sum(slice) == 5, "Unexpected sum of slice");
    }

    static void TestReferenceTypes()
    {
        var underlyingArray = new ReferenceType[] { new ReferenceType(0), new ReferenceType(1), new ReferenceType(2) };
        var slice = new Span<ReferenceType>(underlyingArray);

        for (int i = 0; i < underlyingArray.Length; i++)
        {
            AssertTrue(underlyingArray[i].Value == slice[i].Value, "Values are different");
            AssertTrue(object.ReferenceEquals(underlyingArray[i], slice[i]), "References are broken");
        }
    }

    static void TestArrayCoVariance()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        try
        {
            new Span<object>(objArray);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }

        var objEmptyArray = Array.Empty<ReferenceType>();
        try
        {
            new Span<object>(objEmptyArray);
            AssertTrue(false, "Expected exception not thrown");
        }
        catch (ArrayTypeMismatchException)
        {
        }
    }

    static void TestArrayCoVarianceReadOnly()
    {
        var array = new ReferenceType[1];
        var objArray = (object[])array;
        AssertTrue(new ReadOnlySpan<object>(objArray).Length == 1, "Unexpected length");

        var objEmptyArray = Array.Empty<ReferenceType>();
        AssertTrue(new ReadOnlySpan<object>(objEmptyArray).Length == 0, "Unexpected length");
   }

    static void Main()
    {
        TestSum();
        TestReferenceTypes();
        TestArrayCoVariance();
        TestArrayCoVarianceReadOnly();
        Console.WriteLine("All tests passed");
    }
}
