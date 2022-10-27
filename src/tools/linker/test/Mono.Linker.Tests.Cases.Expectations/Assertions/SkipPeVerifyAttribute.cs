// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{

    public enum SkipPeVerifyForToolchian
    {
        Pedump
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SkipPeVerifyAttribute : BaseExpectedLinkedBehaviorAttribute
    {
        public SkipPeVerifyAttribute()
        {
        }

        public SkipPeVerifyAttribute(SkipPeVerifyForToolchian toolchain)
        {
        }

        public SkipPeVerifyAttribute(string assemblyName)
        {
        }
    }
}
