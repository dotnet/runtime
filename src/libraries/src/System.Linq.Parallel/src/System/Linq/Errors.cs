// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Linq
{
    internal static class Error
    {
        internal static Exception ArgumentNull(string s) { return new ArgumentNullException(s); }

        internal static Exception ArgumentOutOfRange(string s) { return new ArgumentOutOfRangeException(s); }

        internal static Exception MoreThanOneElement() { return new InvalidOperationException(Strings.MoreThanOneElement); }

        internal static Exception MoreThanOneMatch() { return new InvalidOperationException(Strings.MoreThanOneMatch); }

        internal static Exception NoElements() { return new InvalidOperationException(Strings.NoElements); }

        internal static Exception NoMatch() { return new InvalidOperationException(Strings.NoMatch); }

        internal static Exception NotSupported() { return new NotSupportedException(); }
    }

    internal static class Strings
    {
        internal static String EmptyEnumerable { get { return SR.EmptyEnumerable; } }
        internal static String MoreThanOneElement { get { return SR.MoreThanOneElement; } }
        internal static String MoreThanOneMatch { get { return SR.MoreThanOneMatch; } }
        internal static String NoElements { get { return SR.NoElements; } }
        internal static String NoMatch { get { return SR.NoMatch; } }
    }
}
