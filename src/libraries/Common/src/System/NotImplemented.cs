// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    //
    // This class enables one to throw a NotImplementedException using the following idiom:
    //
    //     throw NotImplemented.ByDesign;
    //
    // Used by methods whose intended implementation is to throw a NotImplementedException (typically
    // virtual methods in public abstract classes that intended to be subclassed by third parties.)
    //
    // This makes it distinguishable both from human eyes and CCI from NYI's that truly represent undone work.
    //
    internal static class NotImplemented
    {
        internal static Exception ByDesign => new NotImplementedException();

        internal static Exception ByDesignWithMessage(string message)
        {
            return new NotImplementedException(message);
        }
    }
}
