// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

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
    bool InstVerify(System.Type t1);
}

public struct Gen<T> : IGen<T>
{
    public T Fld1;

    public Gen(T fld1)
    {
        Fld1 = fld1;
    }

    public bool InstVerify(System.Type t1)
    {
        bool result = true;

        if (!(Fld1.GetType().Equals(t1)))
        {
            result = false;
            Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(IGen<T>));
        }

        return result;
    }
}

public class Test
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

    public static int Main()
    {

        IGen<int> IGenInt = new Gen<int>(new int());
        Eval(IGenInt.InstVerify(typeof(int)));

        IGen<double> IGenDouble = new Gen<double>(new double());
        Eval(IGenDouble.InstVerify(typeof(double)));

        IGen<string> IGenString = new Gen<string>("string");
        Eval(IGenString.InstVerify(typeof(string)));

        IGen<object> IGenObject = new Gen<object>(new object());
        Eval(IGenObject.InstVerify(typeof(object)));

        IGen<Guid> IGenGuid = new Gen<Guid>(new Guid());
        Eval(IGenGuid.InstVerify(typeof(Guid)));

        IGen<RefX1<int>> IGenConstructedReference = new Gen<RefX1<int>>(new RefX1<int>());
        Eval(IGenConstructedReference.InstVerify(typeof(RefX1<int>)));

        IGen<ValX1<string>> IGenConstructedValue = new Gen<ValX1<string>>(new ValX1<string>());
        Eval(IGenConstructedValue.InstVerify(typeof(ValX1<string>)));

        IGen<int[]> IGen1DIntArray = new Gen<int[]>(new int[1]);
        Eval(IGen1DIntArray.InstVerify(typeof(int[])));

        IGen<string[,]> IGen2DStringArray = new Gen<string[,]>(new string[1, 1]);
        Eval(IGen2DStringArray.InstVerify(typeof(string[,])));

        IGen<object[][]> IGenJaggedObjectArray = new Gen<object[][]>(new object[1][]);
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
