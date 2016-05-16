// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class DescriptorTestAttribute : Attribute
    {
        public string TestString;

        public DescriptorTestAttribute(string testString = nameof(TestString))
        {
            TestString = testString;
        }

        public override bool Equals(object obj)
        {
            return (obj as DescriptorTestAttribute)?.TestString.Equals(TestString, StringComparison.Ordinal) ?? false;
        }

        public override int GetHashCode() => TestString.GetHashCode();
    }
}
