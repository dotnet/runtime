// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace XPathTests.Common
{
    public class XPathResult
    {
        public XPathResultToken[] Results { get; set; }
        public int CurrentPosition { get; set; }

        public XPathResult(int currentPosition, params XPathResultToken[] resultTokens)
        {
            CurrentPosition = currentPosition;
            Results = resultTokens;
        }
    }
}
