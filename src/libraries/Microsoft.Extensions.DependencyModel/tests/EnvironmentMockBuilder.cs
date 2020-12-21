// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class EnvironmentMockBuilder
    {
        private Dictionary<string, string> _variables = new Dictionary<string, string>();
        private Dictionary<string, object> _appContextData = new Dictionary<string, object>();
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

        public EnvironmentMockBuilder AddAppContextData(string name, object value)
        {
            _appContextData.Add(name, value);
            return this;
        }

        public EnvironmentMockBuilder SetIsWindows(bool value)
        {
            _isWindows = value;
            return this;
        }

        internal IEnvironment Build()
        {
            return new EnvironmentMock(_variables, _appContextData, _isWindows);
        }

        private class EnvironmentMock : IEnvironment
        {
            private Dictionary<string, string> _variables;
            private Dictionary<string, object> _appContextData;
            private bool _isWindows;

            public EnvironmentMock(Dictionary<string, string> variables, Dictionary<string, object> appContextData, bool isWindows)
            {
                _variables = variables;
                _appContextData = appContextData;
                _isWindows = isWindows;
            }

            public string GetEnvironmentVariable(string name)
            {
                _variables.TryGetValue(name, out string value);
                return value;
            }

            public object GetAppContextData(string name)
            {
                _appContextData.TryGetValue(name, out object value);
                return value;
            }

            public bool IsWindows()
            {
                return _isWindows;
            }
        }
    }
}
