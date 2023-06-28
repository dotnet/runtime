// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitInliningTest
{
    internal interface IEmployee
    {
        string Name
        {
            get;
            set;
        }

        int Counter
        {
            get;
        }
    }

    public class Employee : IEmployee
    {
        public static int numberOfEmployees;
        private int _counter;
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public int Counter
        {
            get
            {
                return _counter;
            }
        }
        public Employee()
        {
            _counter = ++_counter + numberOfEmployees;
        }
    }

    public class interfaceProperty
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Employee.numberOfEmployees = 1;
            Employee e1 = new Employee();
            e1.Name = "100";

            if (e1.Counter == 2)
                return Convert.ToInt32(e1.Name);
            else
                return 1;
        }
    }
}

