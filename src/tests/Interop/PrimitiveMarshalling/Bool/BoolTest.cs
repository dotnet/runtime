// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;

class Test
{
    const bool boolManaged = true;
    const bool boolNative = false;

    static void ReportFailure(string describe, bool expect, bool actual)
    {
        throw new Exception(" === Fail: " + describe + "\n\tExpected:" + expect + "\n\tActual:" + actual);        
    }
    
    public static int Main(string[] args)
    {
        //Test Method1
        bool boolValue1 = boolManaged;
        bool boolValueRet1 = BoolNative.Marshal_In(boolValue1);
        if (!boolValueRet1)
        {
            ReportFailure("Method Marshal_In[Managed Side],The return value is wrong", true, boolValueRet1);
        }

        //TestMethod2
        bool boolValue2 = boolManaged;
        bool boolValueRet2 = BoolNative.Marshal_InOut(boolValue2);
        if (!boolValueRet2)
        {
            ReportFailure("Method Marshal_InOut[Managed Side],The return value is wrong", true, boolValueRet2);
        }
        if (boolValue2 != boolManaged)
        {
            ReportFailure("Method Marshal_InOut[Managed Side],The parameter value is changed", boolManaged, boolValue2);
        }

        //TestMethod3
        bool boolValue3 = boolManaged;
        bool boolValueRet3 = BoolNative.Marshal_Out(boolValue3);
        if (!boolValueRet3)
        {
            ReportFailure("Method Marshal_Out[Managed Side],The return value is wrong", true, boolValueRet3);
        }
        if (boolValue3 != boolManaged)
        {
            ReportFailure("Method Marshal_Out[Managed Side],The parameter value is changed", boolManaged, boolValue3);
        }

        //TestMethod4
        bool boolValue4 = boolManaged;
        bool boolValueRet4 = BoolNative.MarshalPointer_In(ref boolValue4);
        if (!boolValueRet4)
        {
            ReportFailure("Method MarshalPointer_In[Managed Side],The return value is wrong", true, boolValueRet4);
        }
        if (boolValue4 != boolManaged)
        {
            ReportFailure("Method MarshalPointer_In[Managed Side],The parameter value is changed", boolManaged, boolValue4);
        }

        //TestMethod5
        bool boolValue5 = boolManaged;
        bool boolValueRet5 = BoolNative.MarshalPointer_InOut(ref boolValue5);
        if (!boolValueRet5)
        {
            ReportFailure("Method MarshalPointer_InOut[Managed Side],The return value is wrong", true, boolValueRet5);
        }
        if (boolValue5 != boolNative)
        {
            ReportFailure("Method MarshalPointer_InOut[Managed Side],The passed value is wrong", boolNative, boolValue5);
        }

        //TestMethod6
        bool boolValue6 = boolManaged;
        bool boolValueRet6 = BoolNative.MarshalPointer_Out(out boolValue6);
        if (!boolValueRet6)
        {
            ReportFailure("Method Marshal_Out[Managed Side],The return value is wrong", true, boolValueRet6);
        }
        if (boolValue6 != boolNative)
        {
            ReportFailure("Method Marshal_Out[Managed Side],The passed value is wrong", boolNative, boolValue6);
        } 

        //Test Method7
        bool boolValue7 = boolManaged;
        bool boolValueRet7 = BoolNative.Marshal_As_In(boolValue7);
        if (!boolValueRet7)
        {
            ReportFailure("Method Marshal_As_In[Managed Side],The return value is wrong", true, boolValueRet7);
        }

        //TestMethod8
        bool boolValue8 = boolManaged;
        bool boolValueRet8 = BoolNative.Marshal_As_InOut(boolValue8);
        if (!boolValueRet8)
        {
            ReportFailure("Method Marshal_As_InOut[Managed Side],The return value is wrong", true, boolValueRet8);
        }
        if (boolValue8 != boolManaged)
        {
            ReportFailure("Method Marshal_As_InOut[Managed Side],The parameter value is changed", boolManaged, boolValue8);
        }

        //TestMethod9
        bool boolValue9 = boolManaged;
        bool boolValueRet9 = BoolNative.Marshal_As_Out(boolValue9);
        if (!boolValueRet9)
        {
            ReportFailure("Method Marshal_As_Out[Managed Side],The return value is wrong", true, boolValueRet9);
        }
        if (boolValue9 != boolManaged)
        {
            ReportFailure("Method Marshal_As_Out[Managed Side],The parameter value is changed", boolManaged, boolValue9);
        }

        if (OperatingSystem.IsWindows())
        {
            TestVariantBool();
        }
        
        return 100;
    }

    private static void TestVariantBool()
    {
        
        if (!BoolNative.Marshal_ByValue_Variant(true, true))
        {
            ReportFailure("Method Marshal_ByValue_Variant[Managed Side], The return value is wrong", true, true);
        }
        
        if (!BoolNative.Marshal_ByValue_Variant(false, false))
        {
            ReportFailure("Method Marshal_ByValue_Variant[Managed Side], The return value is wrong", false, false);
        }

        bool boolValue10 = boolManaged;
        bool boolValueRet10 = BoolNative.Marshal_Ref_Variant(ref boolValue10);

        if (!boolValueRet10)
        {
            ReportFailure("Method Marshal_Ref_Variant[Managed Side], The return value is wrong.", true, boolValueRet10);
        }
        if (boolValue10 != boolNative)
        {
            ReportFailure("Method Marshal_Ref_Variant[Managed Side],The passed value is wrong", boolNative, boolValue10);
        } 
        
        var trueStruct = new BoolNative.ContainsVariantBool
        {
            value = true
        };

        var falseStruct = new BoolNative.ContainsVariantBool
        {
            value = false
        };
        
        if (!BoolNative.Marshal_ByValue_Struct_Variant(trueStruct, true))
        {
            ReportFailure("Method Marshal_ByValue_Variant[Managed Side], The return value is wrong", true, true);
        }
        
        if (!BoolNative.Marshal_ByValue_Struct_Variant(falseStruct, false))
        {
            ReportFailure("Method Marshal_ByValue_Variant[Managed Side], The return value is wrong", false, false);
        }

        var boolValue11 = new BoolNative.ContainsVariantBool
        {
            value = boolManaged
        };
        
        bool boolValueRet11 = BoolNative.Marshal_Ref_Struct_Variant(ref boolValue11);

        if (!boolValueRet11)
        {
            ReportFailure("Method Marshal_Ref_Variant[Managed Side], The return value is wrong.", true, boolValueRet11);
        }
        if (boolValue11.value != boolNative)
        {
            ReportFailure("Method Marshal_Ref_Variant[Managed Side],The passed value is wrong", boolNative, boolValue11.value);
        }
    }
}
