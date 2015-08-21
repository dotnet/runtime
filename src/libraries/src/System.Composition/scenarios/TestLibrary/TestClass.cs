// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLibrary
{
    [Export]
    [CLSCompliant(false)]
    public class ClassWithDependecy
    {
        public TestDependency _dep;
        [ImportingConstructor]
        public ClassWithDependecy(TestDependency dep)
        {
            _dep = dep;
        }
    }

    [Export]
    [CLSCompliant(false)]
    public class ClassWithDependecyAndSameBaseType
    {
        public IDependency _dep;
        [ImportingConstructor]
        public ClassWithDependecyAndSameBaseType(IDependency dep)
        {
            _dep = dep;
        }
    }


    [Export]
    [Export(typeof(IDependency))]
    public class TestDependency : IDependency
    {
        public TestDependency()
        {
        }
    }

    public class NotRealDependency : IDependency
    {
        public NotRealDependency()
        {
        }
    }


    public interface IDependency
    {
    }
}
