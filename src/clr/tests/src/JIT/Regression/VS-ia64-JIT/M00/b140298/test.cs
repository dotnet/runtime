// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class History
{
    private static Object with = null;

    public static int Main()
    {
        CreateHistory(null, null, 0, 0, 0, 0, 0, DateTime.Now, 0, "ciao");
        return 100;
    }

    public static History CreateHistory(Object nearobj, Object amode,
           short inCustomerId,
           sbyte inCustomerDistrictId,
           short inCustomerWarehouseId,
           sbyte inDistrictId,
           short inWarehouseId,
           DateTime inDate,
           float inAmount,
           string inData) // 10-th argument goes in callerSP+0x18
    {
        History newHistory = null;
        newHistory = CreateEntity(null, amode, nearobj, with); // this is the call site
        newHistory.initHistory(inCustomerId,
                            inCustomerDistrictId,
                            inCustomerWarehouseId,
                            inDistrictId,
                            inWarehouseId,
                            inDate,
                            inAmount,
                            inData);
        return newHistory;
    }


    public static History CreateEntity(Object a, Object b, Object c, Object d)
    {
        return new History();
    }

    public void initHistory
    (short inCustomerId,
               sbyte inCustomerDistrictId,
               short inCustomerWarehouseId,
               sbyte inDistrictId,
               short inWarehouseId,
               DateTime inDate,
               float inAmount,
               string inData)
    {
    }
}
