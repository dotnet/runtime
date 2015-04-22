// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Interface Properties
using System;
namespace JitInliningTest
{
    interface IEmployee
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
        private int counter;
        private string name;
        // Read-write instance property:
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        // Read-only instance property:
        public int Counter
        {
            get
            {
                return counter;
            }
        }
        // Constructor:
        public Employee()
        {
            counter = ++counter + numberOfEmployees;
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

