// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Options.Tests
{
    public class FakeOptionsFactory : IOptionsFactory<FakeOptions>
    {
        public static FakeOptions Options = new FakeOptions();

        public FakeOptions Create(string name) => Options;
    }
}
