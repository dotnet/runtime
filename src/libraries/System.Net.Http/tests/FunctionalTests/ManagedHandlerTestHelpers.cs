// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    internal static class ManagedHandlerTestHelpers
    {
        private const string ManagedHandlerEnvVar = "COMPlus_UseManagedHttpClientHandler";
        public static void SetEnvVar() => Environment.SetEnvironmentVariable(ManagedHandlerEnvVar, "true");
        public static void RemoveEnvVar() => Environment.SetEnvironmentVariable(ManagedHandlerEnvVar, null);
        public static bool IsEnabled => Environment.GetEnvironmentVariable(ManagedHandlerEnvVar) == "true";
    }
}
