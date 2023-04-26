// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace TestAnonymousTypes
{
    public class Customer
    {
        public string Name;
        public string Address;
        public int Zip;
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Customer c = new Customer { Name = "Sree", Address = "something somethwere", Zip = 98007 };

            var q = new
            {
                Name0 = c.Name,
                Address0 = c.Address,
                Zip0 = c.Zip,
                Name1 = c.Name,
                Address1 = c.Address,
                Zip1 = c.Zip,
                Name2 = c.Name,
                Address2 = c.Address,
                Zip2 = c.Zip,
                Name3 = c.Name,
                Address3 = c.Address,
                Zip3 = c.Zip,
                Name4 = c.Name,
                Address4 = c.Address,
                Zip4 = c.Zip,
                Name5 = c.Name,
                Address5 = c.Address,
                Zip5 = c.Zip,
                Name6 = c.Name,
                Address6 = c.Address,
                Zip6 = c.Zip,
                Name7 = c.Name,
                Address7 = c.Address,
                Zip7 = c.Zip,
                Name8 = c.Name,
                Address8 = c.Address,
                Zip8 = c.Zip,
                Name9 = c.Name,
                Address9 = c.Address,
                Zip9 = c.Zip,
                Name10 = c.Name,
                Address10 = c.Address,
                Zip10 = c.Zip,
                Name11 = c.Name,
                Address11 = c.Address,
                Zip11 = c.Zip,
                Name12 = c.Name,
                Address12 = c.Address,
                Zip12 = c.Zip,
                Name13 = c.Name,
                Address13 = c.Address,
                Zip13 = c.Zip,
                Name14 = c.Name,
                Address14 = c.Address,
                Zip14 = c.Zip,
                Name15 = c.Name,
                Address15 = c.Address,
                Zip15 = c.Zip,
                Name16 = c.Name,
                Address16 = c.Address,
                Zip16 = c.Zip,
                Name17 = c.Name,
                Address17 = c.Address,
                Zip17 = c.Zip,
                Name18 = c.Name,
                Address18 = c.Address,
                Zip18 = c.Zip,
                Name19 = c.Name,
                Address19 = c.Address,
                Zip19 = c.Zip,
                Name20 = c.Name,
                Address20 = c.Address,
                Zip20 = c.Zip,
                Name21 = c.Name,
                Address21 = c.Address,
                Zip21 = c.Zip,
                Name22 = c.Name,
                Address22 = c.Address,
                Zip22 = c.Zip,
                Name23 = c.Name,
                Address23 = c.Address,
                Zip23 = c.Zip,
                Name24 = c.Name,
                Address24 = c.Address,
                Zip24 = c.Zip,
                Name25 = c.Name,
                Address25 = c.Address,
                Zip25 = c.Zip,
                Name26 = c.Name,
                Address26 = c.Address,
                Zip26 = c.Zip
            };

            return 100;
        }
    }
}

