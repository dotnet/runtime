// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        Customer[] customers = new Customer[]
        {
            new Customer() { Name = "Bob", Age = 23 },
            new Customer() { Name = "Sue", Age = 43 },
            new Customer() { Name = "Pat", Age = 20 },
        };

        var query = customers.AsQueryable()
            .OrderByDescending(c => c.Age)
            .Skip(1)
            .Take(1);
        Customer c = query.Single();

        if (c.Name != "Bob" && c.Age != 23)
        {
            return -1;
        }

        return 100;
    }

    private class Customer
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
