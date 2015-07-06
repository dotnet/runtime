// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.FileSystem.Tests;
using EnumerableTests;
using Xunit;

public class Directory_IEnumeratorTests : FileSystemTest
{
    private static EnumerableUtils s_utils;

    [Fact]
    public void runTest()
    {
        s_utils = new EnumerableUtils();

        s_utils.CreateTestDirs(TestDirectory);

        TestIEnumerator();
        TestClone();

        s_utils.DeleteTestDirs();

        Assert.True(s_utils.Passed);
    }

    private static void TestIEnumerator()
    {
        String chkptFlag = "chkpt_ienum_";
        int failCount = 0;

        IEnumerable<String> dirs_temp = Directory.EnumerateDirectories(s_utils.testDir, "*", SearchOption.AllDirectories);
        IEnumerator dirs = dirs_temp.GetEnumerator();

        try
        {
            if (!dirs.MoveNext())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: MoveNext returned false. Expected some contents");
            }
            if (dirs.Current == null)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: Current returned null. Expected some contents");
            }

            // Reset should throw
            dirs.Reset();
            failCount++;
            Console.WriteLine(chkptFlag + "3: Reset didn't throw exception. It should have thrown NotSupportedException");
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "4: Unexpected exception...");
            Console.WriteLine(e);
        }
        finally
        {
            ((IEnumerator<String>)dirs).Dispose();  // if we jump out while enumerating, the enumerator still holds the file handle
        }


        string testname = "IEnumerator";
        s_utils.PrintTestStatus(testname, "EnumerateDirectories", failCount);
    }

    // This testcase hits the FSEIterator's Clone method. Ensures that 2 different enumerators are returned and quick
    // functionality test.
    private static void TestClone()
    {
        String chkptFlag = "chkpt_clone_";
        int failCount = 0;

        IEnumerable<String> dirs_temp = Directory.EnumerateDirectories(s_utils.testDir, "*", SearchOption.AllDirectories);
        IEnumerator<String> dirs_enum_a = dirs_temp.GetEnumerator();

        dirs_enum_a.MoveNext();
        String da1 = dirs_enum_a.Current;
        dirs_enum_a.MoveNext();
        String da2 = dirs_enum_a.Current;

        IEnumerator<String> dirs_enum_b = dirs_temp.GetEnumerator();
        dirs_enum_b.MoveNext();
        String db1 = dirs_enum_b.Current;
        dirs_enum_b.MoveNext();
        String db2 = dirs_enum_b.Current;

        if (da1 != db1)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "1: Enumerator A's 1st element ({0}) not equal to Enumerator B's 1st element ({1})", da1, db1);
        }
        if (da2 != db2)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "2: Enumerator A's 2nd element ({0}) not equal to Enumerator B's 2nd element ({1})", da1, db1);
        }
        if (da1 == da2)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "3: Enumerator A's 1st and 2nd elements are the same: {0}", da1);
        }

        // if we jump out while enumerating, the enumerator still holds the file handle
        ((IEnumerator<String>)dirs_enum_a).Dispose();
        ((IEnumerator<String>)dirs_enum_b).Dispose();

        string testname = "Clone";
        s_utils.PrintTestStatus(testname, "EnumerateDirectories", failCount);
    }
}
