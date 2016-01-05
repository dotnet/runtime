// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Runtime.CompilerServices;

public class WipOrderRow
{
}

public class WIPOrder
{
#pragma warning disable 0414
    private ProductionContext _context;
    private WipOrderRow _wipOrder;
#pragma warning restore 0414

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public WIPOrder(ProductionContext context, WipOrderRow wipOrder)
    {
        _context = context;
        _wipOrder = wipOrder;
    }
}

public class ProductionContext
{
    public ProductionContext()
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public WipOrderRow SelectWipOrderByPK(string wipOrderNo, short wipOrderType)
    {
        return null;
    }

    public string ReportedWipOrderNo
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get
        {
            return null;
        }
    }

    public short ReportedWipOrderType
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        get
        {
            return 0;
        }
    }
}


public class ProgressConsumerBuilder
{
    private ProductionContext _productionContext;

    public ProgressConsumerBuilder(ProductionContext productionContext)
    {
        _productionContext = productionContext;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public WIPOrder BuildOrder()
    {
        WIPOrder order = new WIPOrder(_productionContext,
                                      _productionContext.SelectWipOrderByPK(_productionContext.ReportedWipOrderNo,
                                                                           _productionContext.ReportedWipOrderType));
        return order;
    }
}

public class MainApp
{
    private static int Main()
    {
        try
        {
            ProductionContext pc = new ProductionContext();
            ProgressConsumerBuilder pb = new ProgressConsumerBuilder(pc);
            pb.BuildOrder();
            Console.WriteLine("Test Success");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Test Failed:" + e.Message);
            return 101;
        }
    }
}

