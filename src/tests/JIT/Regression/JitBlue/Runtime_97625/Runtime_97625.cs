// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public static class Runtime_97625
{
    public class CustomModel
    {
        public decimal Cost { get; set; }
    }

    [Fact]
    public static int Test()
    {
        List<CustomModel> models = new List<CustomModel>();
        models.Add(new CustomModel { Cost = 1 });
        return models.Average (x => x.Cost) == 1 ? 100 : -1;
    }
}
