// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class EnvironmentMockBuilder
    {
        private Dictionary<string, string> _variables = new Dictionary<string, string>();

        internal static IEnvironment Empty { get; } = Create().Build();

        public static EnvironmentMockBuilder Create()
        {
            return new EnvironmentMockBuilder();
        }

        public EnvironmentMockBuilder AddVariable(string name, string value)
        {
            _variables.Add(name, value);
            return this;
        }

        internal IEnvironment Build()
        {
            return new EnvironmentMock(_variables);
        }

        private class EnvironmentMock : IEnvironment
        {
            private Dictionary<string, string> _variables;

            public EnvironmentMock(Dictionary<string, string> variables)
            {
                _variables = variables;
            }

            public string GetEnvironmentVariable(string name)
            {
                string value = null;
                _variables.TryGetValue(name, out value);
                return value;
            }
        }
    }
}
