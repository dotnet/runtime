using System;
using System.Runtime.CompilerServices;

public class TestGC
{

   private static ConditionalWeakTable<string, string> tbl;
   private static WeakReference[] weakRefKeyArr;
   private static WeakReference[] weakRefValArr;

   private static string key0;
   private static string key21;
   private static string key99;

   private static string value0;
   private static string value21;
   private static string value99;

   /* 
   * Ensure that a key that has no managed references to it gets automatically removed from the 
   * dictionary after GC happens. Also make sure the value gets gc’d as well. 
   */
    public static void TestKeyWithNoReferences_Pass1(int length)
    {
        tbl = new ConditionalWeakTable<string, string>();

        weakRefKeyArr = new WeakReference[length];
        weakRefValArr = new WeakReference[length];

        for (int i = 0; i < length; i++)
        {
            String key = "KeyTestString" + i.ToString();
            String value = "ValueTestString" + i.ToString();
            tbl.Add(key, value);
            
            // create a weak reference for the key
            weakRefKeyArr[i] = new WeakReference(key, true);
            weakRefValArr[i] = new WeakReference(value, true);
        }
    }


    public static void TestKeyWithNoReferences_Pass2(int length)
    {
        // force GC to happen
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();


        // note, this assignment will prevent the object from being collected if it isn’t already
        for (int i = 0; i < length; i++)
        {
            Object targetKey = weakRefKeyArr[i].Target;
            Object targetVal = weakRefValArr[i].Target;

            if (targetKey == null && targetVal == null)
            {
                   Console.WriteLine("Object at index " +i+ " was collected");

                   string val;
                   // the key should no longer be in the table
                   Test.Eval(!tbl.TryGetValue("TestString" + i.ToString(), out val), "Err_001: Expected TryGetValue to return false");
            }
            else
            {
                Test.Eval(false, "Err_002: Object at index " + i + " was not collected");
            }
        }

        GC.KeepAlive(tbl);
    }

       
    /*
     * Ensure that a key whose value has a reference to the key or a reference to another object 
     * which has a reference to the key, gets automatically removed from the dictionary after GC 
     * happens (provided there are no references to the value outside the dictionary.) 
     * Also make sure the value gets gc’d as well.
     * 
     * In this case we pass the same string array to the function, so keys and values have references to each other
     * (But only within the dictionary)
     * */

    public static void TestKeyWithInsideReferences_Pass1(int length)
    {

        tbl = new ConditionalWeakTable<string,string>();

        weakRefKeyArr = new WeakReference[length];
        weakRefValArr = new WeakReference[length];

        for (int i = 0; i < length; i++)
        {
	    String key = "SomeTestString" + i.ToString();
            String value = key;
            tbl.Add(key, value);

            // create a weak reference for the key
            weakRefKeyArr[i] = new WeakReference(key, true);
            weakRefValArr[i] = new WeakReference(value, true);
        }
    }

    public static void TestKeyWithInsideReferences_Pass2(int length)
    {
        // force GC to happen
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();


        // note, this assignment will prevent the object from being collected if it isn’t already
        for (int i = 0; i < length; i++)
        {
            Object targetKey = weakRefKeyArr[i].Target;
            Object targetVal = weakRefValArr[i].Target;

            if (targetKey == null && targetVal == null)
            {
                Console.WriteLine("Object at index " + i + " was collected");

                string val;

                // the key should no longer be in the table
                Test.Eval(!tbl.TryGetValue("SomeTestString" + i.ToString(), out val), "Err_003: Expected TryGetValue to return false");
            }
            else
            {
                Test.Eval(false, "Err_004: Object at index " + i + " was not collected");
            }
        }

        GC.KeepAlive(tbl);
    }

    /*
     * Ensure that a key whose value is referenced outside the dictionary does not get 
     * automatically removed from the dictionary after GC happens and the key doesn't get gc'd.
     */
    public static void TestKeyWithOutsideReferences_Pass1(int length) 
    {
        tbl = new ConditionalWeakTable<string, string>();

        weakRefKeyArr = new WeakReference[length];
        weakRefValArr = new WeakReference[length];

        for (int i = 0; i < length; i++)
        {
            String key = "OtherKeyTestString" + i.ToString();
            String value = "OtherValueTestString" + i.ToString();
            tbl.Add(key, value);


	    // these assignments should prevent the object from being collected
	    if (i == 0)
	    {
                key0 = key;
                value0 = value;
            }

            if (i == 21)
	    {
                key21 = key;
                value21 = value;
            }

            if (i == 99)
	    {
                key99 = key;
                value99 = value;
            }

            // create a weak reference for the key
            weakRefKeyArr[i] = new WeakReference(key, true);
            weakRefValArr[i] = new WeakReference(value, true);
        }
    }

    public static void TestKeyWithOutsideReferences_Pass2(int length) 
    {
        // force GC to happen
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // check that all other objects were collected except for the 3 above

        // note, this assignment will prevent the object from being collected if it isn’t already
        for (int i = 0; i < length; i++)
        {
            Object targetKey = weakRefKeyArr[i].Target;
            Object targetVal = weakRefValArr[i].Target;

            if (targetKey == null && targetVal == null)
            {
                if (i == 0 || i == 21 || i == 99)
                {
                    // these items shouldn't have been collected
                    Test.Eval(false, "Err_005: Object at index " + i + " was collected");
                }
                else
                {
                    Console.WriteLine("Pass: Object at index " + i + " was collected");
                    Test.Eval(true);
                }
            }
            else
            {
                if (i == 0 || i == 21 || i == 99)
                {
                    Console.WriteLine("Pass: Object at index " + i + " was not collected");
                    Test.Eval(true);
                }
                else
                {
                    // these items should have been collected
                    Test.Eval(false, "Err_006: Object at index " + i + " was not collected");
                }
            }
        }
        
        // check that the 3 values above were not removed from the dictionary
        string val;

        Test.Eval(tbl.TryGetValue(key0, out val), "Err_007: Expected TryGetValue to return true");
        Test.Eval(val == value0, "Err_008: The value returned by TryGetValue doesn't match the expected value");

        Test.Eval(tbl.TryGetValue(key21, out val), "Err_009: Expected TryGetValue to return true");
        Test.Eval(val == value21, "Err_010: The value returned by TryGetValue doesn't match the expected value");

        Test.Eval(tbl.TryGetValue(key99, out val), "Err_011: Expected TryGetValue to return true");
        Test.Eval(val == value99, "Err_012: The value returned by TryGetValue doesn't match the expected value");

        GC.KeepAlive(tbl);
    }

    public static int Main()
    {
        try
        {
	    // Changing this test to 2 passes - the code has been refactored so there are no 
 	    // outstanding locals with original references to the keys. 
            // This test was failing on IA64 because of IA64 JIT or GC reporting locals longer than necessary
	    // and the entires weren't getting reclaimed. 

            Console.WriteLine("\nTest keys with inside references");
            TestKeyWithInsideReferences_Pass1(100);
            TestKeyWithInsideReferences_Pass2(100);


            Console.WriteLine("\nTest keys with no references");
            TestKeyWithNoReferences_Pass1(50);
            TestKeyWithNoReferences_Pass2(50);


            Console.WriteLine("\nTest keys with outside references");
            TestKeyWithOutsideReferences_Pass1(100);
            TestKeyWithOutsideReferences_Pass2(100);
            
        
            if (Test.result)
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Test threw unexpected exception:\n{0}", e);
            return 102;
        }
    }
}
