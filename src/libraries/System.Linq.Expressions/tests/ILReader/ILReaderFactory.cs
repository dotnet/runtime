// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection.Emit;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public class ILReaderFactory
    {
        public static ILReader Create(object obj)
        {
            if (obj is DynamicMethod dm)
            {
                return new ILReader(new DynamicMethodILProvider(dm), new DynamicScopeTokenResolver(dm));
            }

            throw new NotSupportedException($"Reading IL from type '{obj.GetType()}' is currently not supported.");
        }
    }
}
