// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class Program
{
    private ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();

    public int RunTest()
    {
        bool lockTaken = false;
        int retCode = 25;
        try
        {
            lockTaken = false;
            Console.WriteLine("Attempting rwls.TryEnterUpgradeableReadLock(-2647); on unowned lock");
            lockTaken = rwls.TryEnterUpgradeableReadLock(-2647);
            retCode = -100;
            Console.WriteLine("Expected exception but aquired lock. Failing test.");            
        }
        catch (ArgumentException ae)
        {
            retCode = retCode + 25;
            Console.WriteLine("As expected: Caught ArgumentException\n{0}", ae.Message);
        }
        finally
        {
            if (lockTaken)
            {
                rwls.ExitUpgradeableReadLock();
            }
        }

        try
        {
            lockTaken = false;
            Console.WriteLine();
            Console.WriteLine("Attempting rwls.TryEnterReadLock(-2647); on unowned lock");
            lockTaken = rwls.TryEnterReadLock(-2647);
            retCode = -110;
            Console.WriteLine("Expected exception but aquired lock. Failing test.");
        }
        catch (ArgumentException ae)
        {
            retCode = retCode + 25;
            Console.WriteLine("As expected: Caught ArgumentException\n{0}", ae.Message);
        }
        finally
        {
            if (lockTaken)
            {
                rwls.ExitReadLock();
            }
        }

        try
        {
            lockTaken = false;
            Console.WriteLine();
            Console.WriteLine("Attempting rwls.TryEnterWriteLock(-2647); on unowned lock");
            lockTaken = rwls.TryEnterWriteLock(-2647);
            retCode = -120;
            Console.WriteLine("Expected exception but aquired lock. Failing test.");
        }
        catch (ArgumentException ae)
        {
            retCode = retCode + 25;
            Console.WriteLine("As expected: Caught ArgumentException\n{0}", ae.Message);
        }
        finally
        {
            if (lockTaken)
            {
                rwls.ExitWriteLock();
            }
        }

        return retCode;
    }

    static int Main()
    {
        return (new Program()).RunTest();
    }
}