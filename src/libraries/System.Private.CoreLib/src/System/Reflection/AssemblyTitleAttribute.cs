// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyTitleAttribute : Attribute
    {
        public AssemblyTitleAttribute(string title)
        {
            Title = title;
        }

        public string Title { get; }
    }
}
