// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
        public static int Main()
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

