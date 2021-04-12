// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public struct WaitForChangedResult
    {
        internal WaitForChangedResult(WatcherChangeTypes changeType, string? name, string? oldName, bool timedOut)
        {
            ChangeType = changeType;
            Name = name;
            OldName = oldName;
            TimedOut = timedOut;
        }

        internal static readonly WaitForChangedResult TimedOutResult =
            new WaitForChangedResult(changeType: 0, name: null, oldName: null, timedOut: true);

        public WatcherChangeTypes ChangeType { readonly get; set; }
        public string? Name { readonly get; set; }
        public string? OldName { readonly get; set; }
        public bool TimedOut { readonly get; set; }
    }
}
