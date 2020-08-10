// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options.Tests
{
    public class FakeOptions
    {
        public FakeOptions()
        {
            Message = "";
        }

        public string Message { get; set; }
    }

    public class FakeOptions2 : FakeOptions { }
}
