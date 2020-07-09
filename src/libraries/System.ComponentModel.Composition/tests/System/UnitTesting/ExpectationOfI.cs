// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.UnitTesting
{
    public class Expectation<TInputAndOutput> : Expectation<TInputAndOutput, TInputAndOutput>
    {
        public Expectation(TInputAndOutput input, TInputAndOutput output)
            : base(input, output)
        {
        }
    }

}
