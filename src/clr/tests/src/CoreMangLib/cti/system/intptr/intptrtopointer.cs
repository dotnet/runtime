// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Security;

namespace ToPointer
{
    class IntPtrToPointer
    {
        static int  Main()
        {
            IntPtrToPointer dek = new IntPtrToPointer();
            TestLibrary.TestFramework.BeginTestCase("IntPtrToPointer");
            if (dek.RunTests())
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("PASS");
                return 100;
            }
            else
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("FAIL");
                return 0;
            }
        }
        public bool RunTests()
            {
                bool retVal = true;
                TestLibrary.TestFramework.LogInformation("[Positive]");
                retVal = PosTest1() && retVal;
                retVal = PosTest2() && retVal;
                retVal = PosTest3() && retVal;
             

                TestLibrary.TestFramework.LogInformation("[Negative]");
              
                return retVal;


            }

       [SecuritySafeCritical]
       unsafe public bool PosTest1()
        {
            bool retVal = true;
                  TestLibrary.TestFramework.BeginScenario("PosTest1: int*");
                  try
                  {  
                      int i = Int32.MaxValue;
                      IntPtr ptr1 = new IntPtr((void*)&i);
                      int* iAddress = (int*)ptr1.ToPointer();
                      if (iAddress != &i)
                      {
                          TestLibrary.TestFramework.LogError("001", "the address  is not equal to the i ");
                      }



                     
                          
                  }
                  catch(Exception e)
                  {
                      TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
                     retVal = false;

                  }
                  return retVal;
                               
              }

        [SecuritySafeCritical]
        unsafe public bool PosTest2()
        {
            bool retVal = true;
                   
       
            TestLibrary.TestFramework.BeginScenario("PosTest2: char*");
            try
            {

                char i = 'h';

                IntPtr ptr1 = new IntPtr((void*)&i);

                char* iAddress = (char*)ptr1.ToPointer();
                
                    if (iAddress != &i)
                    {
                        TestLibrary.TestFramework.LogError("003", "the address  is not equal to the i ");
                    }
 
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;

        }

        [SecuritySafeCritical]
        unsafe public bool PosTest3()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest3: read bool address ");
            try
            {

                bool i = true  ;

                IntPtr ptr1 = new IntPtr((void*)&i);

                bool* iAddress = (bool*)ptr1.ToPointer();

                if (*iAddress != true)
                {
                    TestLibrary.TestFramework.LogError("005", "the value match the address is not equal to the value of i ");
                }

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;

        }
    } 
}

