// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexRunnerFactory class is a base class for compiled regex code.

namespace System.Text.RegularExpressions
{
    abstract public class RegexRunnerFactory
    {
        protected RegexRunnerFactory() { }
        abstract protected internal RegexRunner CreateInstance();
    }

}

