// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public class Uri
    {
        internal const char c_DummyChar = (char)0xFFFF;     //An Invalid Unicode character used as a dummy char passed into the parameter
        internal const int c_MaxUriBufferSize = 0xFFF0;

        internal static bool IriParsingStatic(UriParser? syntax)
        {
            throw new NotImplementedException();
        }
    }
}
