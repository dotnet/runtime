// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    internal sealed partial class UnixFileSystem
    {
        public override bool CaseSensitive { get { return false; } } // TODO: Issue #1086. OSX could be either true or false, though most commonly false.
    }
}
