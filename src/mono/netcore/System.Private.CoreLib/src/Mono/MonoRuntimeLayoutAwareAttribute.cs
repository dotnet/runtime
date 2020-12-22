// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono
{

    [AttributeUsage(AttributeTargets.Class)]
    internal class MonoRuntimeLayoutAwareAttribute : Attribute
    {
        public MonoRuntimeLayoutAwareAttribute (string fieldName) {}
    }
}
