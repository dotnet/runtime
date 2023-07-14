// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct ValX0 { }
public struct ValY0 { }
public struct ValX1<T> { }
public struct ValY1<T> { }
public struct ValX2<T, U> { }
public struct ValY2<T, U> { }
public struct ValX3<T, U, V> { }
public struct ValY3<T, U, V> { }
public class RefX0 { }
public class RefY0 { }
public class RefX1<T> { }
public class RefY1<T> { }
public class RefX2<T, U> { }
public class RefY2<T, U> { }
public class RefX3<T, U, V> { }
public class RefY3<T, U, V> { }


public interface IGen<T>
{
    void _Init(T fld1);
    bool InstVerify(System.Type t1);
}

public interface IGenSub<T> : IGen<T> { }

public class GenInt : IGenSub<int>
{
    int Fld1;

    public void _Init(int fld1)
    {
        Fld1 = fld1;
    }

    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<int>));
        }

        return result;
    }
}

public class GenDouble : IGen<double>
{
    double Fld1;

    public void _Init(double fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<double>));
        }

        return result;
    }
}

public class GenString : IGen<String>
{
    string Fld1;

    public void _Init(string fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<string>));
        }

        return result;
    }
}

public class GenObject : IGen<object>
{
    object Fld1;

    public void _Init(object fld1)
    {
        Fld1 = fld1;
    }

    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<object>));
        }

        return result;
    }
}

public class GenGuid : IGen<Guid>
{
    Guid Fld1;

    public void _Init(Guid fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<Guid>));
        }

        return result;
    }
}

public class GenConstructedReference : IGen<RefX1<int>>
{
    RefX1<int> Fld1;

    public void _Init(RefX1<int> fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<RefX1<int>>));
        }

        return result;
    }
}

public class GenConstructedValue : IGen<ValX1<string>>
{
    ValX1<string> Fld1;

    public void _Init(ValX1<string> fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<ValX1<string>>));
        }

        return result;
    }
}


public class Gen1DIntArray : IGen<int[]>
{
    int[] Fld1;

    public void _Init(int[] fld1)
    {
        Fld1 = fld1;
    }

    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<int[]>));
        }

        return result;
    }
}

public class Gen2DStringArray : IGen<string[,]>
{
    string[,] Fld1;

    public void _Init(string[,] fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<string[,]>));
        }

        return result;
    }
}

public class GenJaggedObjectArray : IGen<object[][]>
{
    object[][] Fld1;

    public void _Init(object[][] fld1)
    {
        Fld1 = fld1;
    }


    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<object[][]>));
        }

        return result;
    }
}


public class Test_Class01
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine("Test Failed at location: " + counter);
        }

    }

    [Fact]
    public static int TestEntryPoint()
    {
        IGen<int> IGenInt = new GenInt();
        IGenInt._Init(new int());
        Eval(IGenInt.InstVerify(typeof(int)));

        IGen<double> IGenDouble = new GenDouble();
        IGenDouble._Init(new double());
        Eval(IGenDouble.InstVerify(typeof(double)));

        IGen<string> IGenString = new GenString();
        IGenString._Init("string");
        Eval(IGenString.InstVerify(typeof(string)));

        IGen<object> IGenObject = new GenObject();
        IGenObject._Init(new object());
        Eval(IGenObject.InstVerify(typeof(object)));

        IGen<Guid> IGenGuid = new GenGuid();
        IGenGuid._Init(new Guid());
        Eval(IGenGuid.InstVerify(typeof(Guid)));

        IGen<RefX1<int>> IGenConstructedReference = new GenConstructedReference();
        IGenConstructedReference._Init(new RefX1<int>());
        Eval(IGenConstructedReference.InstVerify(typeof(RefX1<int>)));

        IGen<ValX1<string>> IGenConstructedValue = new GenConstructedValue();
        IGenConstructedValue._Init(new ValX1<string>());
        Eval(IGenConstructedValue.InstVerify(typeof(ValX1<string>)));

        IGen<int[]> IGen1DIntArray = new Gen1DIntArray();
        IGen1DIntArray._Init(new int[1]);
        Eval(IGen1DIntArray.InstVerify(typeof(int[])));

        IGen<string[,]> IGen2DStringArray = new Gen2DStringArray();
        IGen2DStringArray._Init(new string[1, 1]);
        Eval(IGen2DStringArray.InstVerify(typeof(string[,])));

        IGen<object[][]> IGenJaggedObjectArray = new GenJaggedObjectArray();
        IGenJaggedObjectArray._Init(new object[1][]);
        Eval(IGenJaggedObjectArray.InstVerify(typeof(object[][])));

        if (result)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed");
            return 1;
        }
    }

}
