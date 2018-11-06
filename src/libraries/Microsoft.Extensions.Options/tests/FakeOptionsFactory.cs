// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Options.Tests
{
    public class FakeOptionsFactory : IOptionsFactory<FakeOptions>
    {
        public static FakeOptions Options = new FakeOptions();

        public FakeOptions Create(string name) => Options;
    }
}
