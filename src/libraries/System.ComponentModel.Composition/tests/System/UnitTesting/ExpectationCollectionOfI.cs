// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;

namespace System.UnitTesting
{
    public class ExpectationCollection<TInputAndOutput> : Collection<Expectation<TInputAndOutput>>
    {
        public void Add(TInputAndOutput input, TInputAndOutput output)
        {
            Add(new Expectation<TInputAndOutput>(input, output));
        }
    }
}
