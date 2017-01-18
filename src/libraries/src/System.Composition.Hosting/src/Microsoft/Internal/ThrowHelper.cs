// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Composition.Hosting.Properties;

namespace Microsoft.Internal
{
    internal static class ThrowHelper
    {
        private static Exception LogException(Exception e)
        {
            Debug.WriteLine(Resources.Diagnostic_ThrowingException, e.ToString());
            return e;
        }

        public static ArgumentException ArgumentException(string message)
        {
            var e = new ArgumentException(message);
            LogException(e);
            return e;
        }

        public static CompositionFailedException CardinalityMismatch_TooManyExports(string exportKey)
        {
            var e = new CompositionFailedException(string.Format(Resources.CardinalityMismatch_TooManyExports, exportKey));
            LogException(e);
            return e;
        }

        public static CompositionFailedException CompositionException(string message)
        {
            var e = new CompositionFailedException(message);
            LogException(e);
            return e;
        }

        internal static Exception NotImplemented_MetadataCycles()
        {
            var e = new NotImplementedException(Resources.NotImplemented_MetadataCycles);
            LogException(e);
            return e;
        }
    }
}
