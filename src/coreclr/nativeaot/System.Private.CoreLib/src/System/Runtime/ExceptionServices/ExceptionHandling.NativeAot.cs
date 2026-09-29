// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.ExceptionServices
{
    public static partial class ExceptionHandling
    {
        public static partial Exception? GetCurrentException()
        {
            unsafe
            {
                // Unlike CoreCLR, NativeAOT does not wrap thrown objects that do not derive from Exception.
                // Wrap them here so that the in-flight exception is reported consistently.
                return EH.GetCurrentException(RuntimeImports.RhpGetCurrentExInfo()) switch
                {
                    null => null,
                    Exception exception => exception,
                    object thrownObject => new RuntimeWrappedException(thrownObject),
                };
            }
        }
    }
}
