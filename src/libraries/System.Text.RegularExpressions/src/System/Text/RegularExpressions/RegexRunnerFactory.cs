// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    public abstract class RegexRunnerFactory
    {
        protected RegexRunnerFactory() { }
        protected internal abstract RegexRunner CreateInstance();
    }
}
