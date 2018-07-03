// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace DPStressHarness
{
    public enum TestPriority
    {
        BVT = 0,
        High = 1,
        Medium = 2,
        Low = 3
    }

    public class TestAttributeBase : Attribute
    {
        private string _title;
        private string _description = "none provided";
        private string _applicationName = "unknown";
        private string _improvement = "ADONETV3";
        private string _owner = "unknown";
        private string _category = "unknown";
        private TestPriority _priority = TestPriority.BVT;

        public TestAttributeBase(string title)
        {
            _title = title;
        }

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Improvement
        {
            get { return _improvement; }
            set { _improvement = value; }
        }

        public string Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        public TestPriority Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestAttribute : TestAttributeBase
    {
        private int _warmupIterations = 0;
        private int _testIterations = 1;

        public TestAttribute(string title) : base(title)
        {
        }

        public int WarmupIterations
        {
            get
            {
                string propName = "WarmupIterations";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _warmupIterations;
                }
            }
            set { _warmupIterations = value; }
        }

        public int TestIterations
        {
            get
            {
                string propName = "TestIterations";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _testIterations;
                }
            }
            set { _testIterations = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class StressTestAttribute : TestAttributeBase
    {
        private int _weight = 1;

        public StressTestAttribute(string title)
            : base(title)
        {
        }

        public int Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MultiThreadedTestAttribute : TestAttributeBase
    {
        private int _warmupDuration = 60;
        private int _testDuration = 60;
        private int _threads = 16;

        public MultiThreadedTestAttribute(string title)
            : base(title)
        {
        }

        public int WarmupDuration
        {
            get
            {
                string propName = "WarmupDuration";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _warmupDuration;
                }
            }
            set { _warmupDuration = value; }
        }

        public int TestDuration
        {
            get
            {
                string propName = "TestDuration";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _testDuration;
                }
            }
            set { _testDuration = value; }
        }

        public int Threads
        {
            get
            {
                string propName = "Threads";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _threads;
                }
            }
            set { _threads = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ThreadPoolTestAttribute : TestAttributeBase
    {
        private int _warmupDuration = 60;
        private int _testDuration = 60;
        private int _threads = 64;

        public ThreadPoolTestAttribute(string title)
            : base(title)
        {
        }

        public int WarmupDuration
        {
            get
            {
                string propName = "WarmupDuration";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _warmupDuration;
                }
            }
            set { _warmupDuration = value; }
        }

        public int TestDuration
        {
            get
            {
                string propName = "TestDuration";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _testDuration;
                }
            }
            set { _testDuration = value; }
        }

        public int Threads
        {
            get
            {
                string propName = "Threads";

                if (TestMetrics.Overrides.ContainsKey(propName))
                {
                    return int.Parse(TestMetrics.Overrides[propName]);
                }
                else
                {
                    return _threads;
                }
            }
            set { _threads = value; }
        }
    }
}
