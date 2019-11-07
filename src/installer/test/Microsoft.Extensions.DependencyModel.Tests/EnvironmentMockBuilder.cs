// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class EnvironmentMockBuilder
    {
        private Dictionary<string, string> _variables = new Dictionary<string, string>();
        private bool _isWindows;

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

        public EnvironmentMockBuilder SetIsWindows(bool value)
        {
            _isWindows = value;
            return this;
        }

        internal IEnvironment Build()
        {
            return new EnvironmentMock(_variables, _isWindows);
        }

        private class EnvironmentMock : IEnvironment
        {
            private Dictionary<string, string> _variables;
            private bool _isWindows;

            public EnvironmentMock(Dictionary<string, string> variables, bool isWindows)
            {
                _variables = variables;
                _isWindows = isWindows;
            }

            public string GetEnvironmentVariable(string name)
            {
                string value = null;
                _variables.TryGetValue(name, out value);
                return value;
            }

            public bool IsWindows()
            {
                return _isWindows;
            }
        }
    }
}
