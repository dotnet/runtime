// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

namespace TestLibrary
{
    [Export]
    public class ClassWithDependency
    {
        public TestDependency _dep;
        [ImportingConstructor]
        public ClassWithDependency(TestDependency dep)
        {
            _dep = dep;
        }
    }

    [Export]
    public class ClassWithDependencyAndSameBaseType
    {
        public IDependency _dep;
        [ImportingConstructor]
        public ClassWithDependencyAndSameBaseType(IDependency dep)
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
