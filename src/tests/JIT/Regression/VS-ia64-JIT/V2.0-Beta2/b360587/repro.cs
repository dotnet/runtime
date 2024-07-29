// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class MyDispose : IDisposable
{
    public int DisposeCounter = 0;
    private bool disposed = false;

    public void Dispose()
    {
        disposed = true;
        DisposeCounter++;
    }

    public bool IsDisposed
    {
        get
        {
            return disposed;
        }
        set
        {
            disposed = value;
        }
    }
}

public class Test_repro
{

    // OUT:
    //   TRUE  if the object was DISPOSED
    //   FALSE if the object was NOT DISPOSED
    public bool BrokenSwitch(int msg, MyDispose m)
    {
        bool result = true;

        switch (msg)
        {
            case 0:
                using (m)
                {
                    if (m.IsDisposed)
                    {
                        break;
                    }

                    result = false;
                }
                break;
            default:
                break;
        }

        return result;
    }

    // OUT:
    //   TRUE  if the object was DISPOSED
    //   FALSE if the object was NOT DISPOSED
    public bool WorkingSwitch(int msg, MyDispose m)
    {
        bool result = true;

        switch (msg)
        {
            case 0:
                using (m)
                {
                    if (!m.IsDisposed)
                    {
                        result = false;
                    }
                }
                break;
            default:
                break;
        }

        return result;
    }


    // OUT:
    //   TRUE  if the object was DISPOSED
    //   FALSE if the object was NOT DISPOSED
    public bool ReturnFromUsing(MyDispose m)
    {
        using (m)
        {
            if (!m.IsDisposed)
            {
                return false;
            }
        }

        return true;
    }


    // OUT:
    //   TRUE  if the object was DISPOSED
    //   FALSE if the object was NOT DISPOSED
    public bool GotoFromUsing(MyDispose m)
    {
        using (m)
        {
            if (!m.IsDisposed)
            {
                goto EXIT;
            }
        }

        return true;

    EXIT:

        return false;
    }

    public int SwitchTests()
    {
        MyDispose m = new MyDispose();
        bool wasDisposed;

        // Called with object not disposed
        m.DisposeCounter = 0;

        m.IsDisposed = false;
        wasDisposed = BrokenSwitch(0, m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("SwitchTests1: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }

        m.DisposeCounter = 0;
        m.IsDisposed = false;
        wasDisposed = WorkingSwitch(0, m) || wasDisposed;

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("SwitchTests2: MyDispose.Dispose() called too many times 12 != {0}", m.DisposeCounter);
            return -1;
        }
        if (wasDisposed)
        {
            // the object should not have been disposed entering
            //  these method calls
            // if the object was disposed then there was an issue
            Console.WriteLine("SwitchTests1: Object was Disposed upon entering the method call (in error)");
            return -2;
        }


        // called with object disposed
        m.DisposeCounter = 0;

        m.IsDisposed = true;
        wasDisposed = BrokenSwitch(0, m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("SwitchTests3: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }

        m.DisposeCounter = 0;
        m.IsDisposed = true;
        wasDisposed = WorkingSwitch(0, m) && wasDisposed;

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("SwitchTests4: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }
        if (!wasDisposed)
        {
            // the object should have been disposed entering
            //  these method calls
            // if the object was not disposed then there was an issue
            Console.WriteLine("SwitchTests2: Object was not Disposed upon entering the method call (in error)");
            return -2;
        }

        return 0;
    }

    public int ReturnTests()
    {
        MyDispose m = new MyDispose();
        bool wasDisposed;

        // Called with object not disposed
        m.DisposeCounter = 0;
        m.IsDisposed = false;
        wasDisposed = ReturnFromUsing(m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("ReturnTests1: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }
        if (wasDisposed)
        {
            // the object should not have been disposed entering
            //  this method call
            // if the object was disposed then there was an issue
            Console.WriteLine("ReturnTests1: Object was Disposed upon entering the method call (in error)");
            return -2;
        }

        // called with object disposed
        m.DisposeCounter = 0;
        m.IsDisposed = true;
        wasDisposed = ReturnFromUsing(m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("ReturnTests2: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }
        if (!wasDisposed)
        {
            // the object should have been disposed entering
            //  this method call
            // if the object was not disposed then there was an issue
            Console.WriteLine("ReturnTests2: Object was not Disposed upon entering the method call (in error)");
            return -2;
        }

        return 0;
    }

    public int GotoTests()
    {
        MyDispose m = new MyDispose();
        bool wasDisposed;

        // Called with object not disposed
        m.DisposeCounter = 0;
        m.IsDisposed = false;
        wasDisposed = GotoFromUsing(m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("GotoTests1: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }
        if (wasDisposed)
        {
            // the object should not have been disposed entering
            //  this method call
            // if the object was disposed then there was an issue
            Console.WriteLine("GotoTests1: Object was Disposed upon entering the method call (in error)");
            return -2;
        }

        // called with object disposed
        m.DisposeCounter = 0;
        m.IsDisposed = true;
        wasDisposed = GotoFromUsing(m);

        if (1 != m.DisposeCounter)
        {
            Console.WriteLine("GotoTests2: MyDispose.Dispose() called too many times 1 != {0}", m.DisposeCounter);
            return -1;
        }
        if (!wasDisposed)
        {
            // the object should have been disposed entering
            //  this method call
            // if the object was not disposed then there was an issue
            Console.WriteLine("GotoTests2: Object was not Disposed upon entering the method call (in error)");
            return -2;
        }


        return 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Test_repro t = new Test_repro();
        int retVal = 0;

        // using in switch statements
        retVal += t.SwitchTests();

        // return from using
        retVal += t.ReturnTests();

        // goto out of a using
        retVal += t.GotoTests();

        if (0 == retVal)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return 0;
        }
    }
}



