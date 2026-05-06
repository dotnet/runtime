// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

namespace TestCS
{
    public class Class8
    {
        static int returnCode = 99;
        static string expectedExceptionString;
        static string expectedOuterExceptionString = "Foobar";
                
        [Fact]
        static public int TestEntryPoint()
        {
            Object foo = null;
             try
             {
                foo.GetType();
             }
             catch(Exception e)
             {
                expectedExceptionString = e.Message;
             }
                 
            try
            {
                DoIt();
            }
            catch(Exception e)
            {                          
                  if (e.Message != expectedOuterExceptionString)
                        returnCode = 98;
          
                Console.WriteLine("Outer Exception Message Found: " + e.Message);
                Console.WriteLine("Outer Exception Message Expected: " + expectedOuterExceptionString);
            }
                        
             if (returnCode == 100)
                        Console.WriteLine("Test PASSED");
             else
                        Console.WriteLine("Test FAILED");

             return returnCode;
        }

        static public void DoIt()
        {
            try
            {
                ThrowException();
            }
            finally
            {
                Foobar();
            }
        }
 
        static private void ThrowException()
        {
            try
            {
                throw new Exception(expectedOuterExceptionString);
            }
            catch(Exception)
            {
                throw;
            }
        }

        static public void Foobar()
        {
            Object foo = null;

            try
            {
                foo.GetType();
            }

            catch(Exception e)
            {
                // The message here should be "Object reference not set to an instance of an object."
                // But it displays "Foobar" instead
                //
                  if (e.Message != expectedExceptionString)
                        returnCode = 98;
                  else
                        returnCode = 100;
                  
                Console.WriteLine("Message Found: " + e.Message);
                Console.WriteLine("Message Expected: " + expectedExceptionString);
            }
        }
    }
}
