// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class History
{
    private static Object with = null;

    [Fact]
    public static int TestEntryPoint()
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

    internal void initHistory
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
