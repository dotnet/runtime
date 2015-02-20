// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    // This is a stub implementation of the Debugger class.  The implementation of this class is not portable
    // since each runtime will have a different way of interacting with the debugger.  The long term plan is
    // to make System.Diagnostics.Debug a partial facade over mscorlib.  Once that is done, this type will be
    // removed and instead we will have a type forward to the implementation in mscorlib.
    public static class Debugger
    {
        public static bool IsAttached
        {
            get { return false; }
                
        }

        public static void Break()
        {
            throw new NotImplementedException();
        }

        public static bool Launch()
        {
            throw new NotImplementedException();
        }
    }
}