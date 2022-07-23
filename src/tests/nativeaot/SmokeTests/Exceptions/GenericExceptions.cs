// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

public class GenericExceptions
{
    public static void RunTests() 
    {
        AssertEqual(GenericExceptions_Case1<DivideByZeroException, NullReferenceException>(1, 0, null), 44);
        AssertExpects<NullReferenceException>(() => GenericExceptions_Case1<DivideByZeroException, NotSupportedException>(1, 0, null));
        GenericExceptions_Case1<DivideByZeroException, NotSupportedException>(1, 0, "not null");
        AssertExpects<NullReferenceException>(() => GenericExceptions_Case1<InvalidOperationException, NotSupportedException>(1, 10, null));
        AssertExpects<DivideByZeroException>(() => GenericExceptions_Case1<InvalidOperationException, NotSupportedException>(1, 0, "not null"));

        AssertEqual(GenericExceptions_Case2<string, Array>(() => throw new MyException<string>()), 111);
        AssertEqual(GenericExceptions_Case2<string, Array>(() => throw new MyException<Array>()), 222);
        AssertEqual(GenericExceptions_Case2<string, Array>(() => throw new MyException<List<int>>()), 42);
        AssertEqual(GenericExceptions_Case2<string, Array>(() => throw new MyException<GenericExceptions>()), 42);
        AssertEqual(GenericExceptions_Case2<string, Array>(() => throw new InvalidOperationException()), 42);
        AssertEqual(_counter, 24);
    }

    public static void AssertEqual(int a, int b)
    {
        if (a != b)
            throw new InvalidOperationException($"{a} != {b}");
    }

    public static void AssertExpects<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"{typeof(T)} was expected to be thrown");
    }

    public class MyException<T> : Exception { }

    private static int _counter = 0;

    public static int GenericExceptions_Case1<T1, T2>(int a, int b, object o) 
        where T1 : Exception 
        where T2 : Exception
    {
        try
        {
            _counter++;
            return a / b;
        }
        catch (T1)
        {
            _counter++;
            return 44;
        }
        finally
        {
            try
            {
                _counter++;
                Console.WriteLine(o.ToString());
            }
            catch (T2 t)
            {
                _counter++;
                Console.WriteLine(t.GetType().Name);
            }
        }
    }

    public static int GenericExceptions_Case2<T1, T2>(Action action)
    {
        try
        {
            action?.Invoke();
            return 1;
        }
        catch (MyException<T1> e1)
        {
            _counter++;
            int hc = 100;
            if (e1.GetHashCode() > 1000) // just some BBs in the catch handler
                hc += 100;
            return 111;
        }
        catch (MyException<T2> e2)
        {
            _counter++;
            int hc = 100;
            if (e2.GetHashCode() > 1000) // just some BBs in the catch handler
                hc += 100;
            return 222;
        }
        catch (MyException<object> e2)
        {
            _counter++;
            Console.WriteLine("re-throw");
            throw;
        }
        catch
        {
            _counter++;
            return 42;
        }
        finally
        {
            _counter++;
        }
    }
}
