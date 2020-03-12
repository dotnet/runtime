// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.AspNetCore.Testing
{
    public class RepeatContext
    {
        private static AsyncLocal<RepeatContext> _current = new AsyncLocal<RepeatContext>();

        public static RepeatContext Current
        {
            get => _current.Value;
            internal set => _current.Value = value;
        }

        public RepeatContext(int limit)
        {
            Limit = limit;
        }

        public int Limit { get; }

        public int CurrentIteration { get; set; }
    }
}
