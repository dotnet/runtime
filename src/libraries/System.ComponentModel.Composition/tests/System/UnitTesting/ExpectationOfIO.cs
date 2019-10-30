// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.UnitTesting
{
    public class Expectation<TInput, TOutput>
    {
        public Expectation(TInput input, TOutput output)
        {
            Input = input;
            Output = output;
        }

        public TInput Input
        {
            get;
            private set;
        }

        public TOutput Output
        {
            get;
            private set;
        }
    }
}
