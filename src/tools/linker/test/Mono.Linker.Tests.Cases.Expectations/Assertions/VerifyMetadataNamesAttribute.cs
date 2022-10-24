// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class VerifyMetadataNamesAttribute : BaseExpectedLinkedBehaviorAttribute
    {
        public VerifyMetadataNamesAttribute()
        {
        }
    }
}
