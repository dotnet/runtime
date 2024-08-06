// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace RuntimeLibrariesTest
{
    /// <summary>
    /// A method to be called before any tests in the assembly are executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AssemblyInitializeAttribute : Attribute
    {
    }

    /// <summary>
    /// A method to be called after all tests in the assembly are executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AssemblyCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// A test class. A test class using this attribute must use instance methods for Tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
    }

    /// <summary>
    /// A test method in a test class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// A method to be called before each test is executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestInitializeAttribute : Attribute
    {
    }

    /// <summary>
    /// A method to be called after each test is executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestCleanupAttribute : Attribute
    {
    }


    /// <summary>
    /// Indicates that a test should be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class IgnoreAttribute : Attribute
    {
        public readonly string Description;

        public IgnoreAttribute()
        { }

        public IgnoreAttribute(string description)
        {
            Description = description;
        }
    }

    /// <summary>
    /// Indicates that a test is expected to throw an exception.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExpectedExceptionAttribute : Attribute
    {
        public readonly string Description;
        public readonly Type ExceptionType;

        private ExpectedExceptionAttribute()
        { }

        public ExpectedExceptionAttribute(Type type) : this(type, null)
        { }

        public ExpectedExceptionAttribute(Type type, string description)
        {
            ExceptionType = type;
            Description = description;
        }
    }
    /// <summary>
    /// Indicates that this TestMethod has specific requirements. This will be propagated to generated runproj files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CLRTestRequiresAttribute : Attribute
    {
        public readonly string Requirements;

        public CLRTestRequiresAttribute(String Requirements)
        {
            this.Requirements = Requirements;
        }
    }

    /// <summary>
    /// Contracts required for the test class to build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ContractsRequiredAttribute : Attribute
    {
        public readonly string[] Contracts;

        /// <summary>
        /// Comma separated string containing all of the contracts required.
        /// </summary>
        /// <example>contracts = "System.Threading, System.Threading.Tasks, System.Runtime"</example>
        /// <param name="contract">contracts required.</param>
        public ContractsRequiredAttribute(string contracts)
        {
            string[] parsed = contracts.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Contracts = parsed;
        }

        /// <summary>
        /// Pass an array of strings representing a contract.
        /// </summary>
        /// <remarks>
        /// Not CLS-Compliant.
        /// </remarks>
        /// <param name="contracts">contracts required.</param>
        public ContractsRequiredAttribute(string[] contracts)
        {
            Contracts = contracts;
        }
    }
}
