// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public struct WaitForChangedResult
    {
        internal WaitForChangedResult(WatcherChangeTypes changeType, string name, string oldName, bool timedOut)
        {
            ChangeType = changeType;
            Name = name;
            OldName = oldName;
            TimedOut = timedOut;
        }

        internal static readonly WaitForChangedResult TimedOutResult = 
            new WaitForChangedResult(changeType: 0, name: null, oldName: null, timedOut: true);

        public WatcherChangeTypes ChangeType { get; set; }
        public string Name { get; set; }
        public string OldName { get; set; }
        public bool TimedOut { get; set; }
    }
}
