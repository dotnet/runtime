// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Allows a user to define events of interest. An instance of this
    /// class is passed to an EventReader to actually obtain the EventRecords.
    /// The EventLogQuery can be as simple specifying that all events are of
    /// interest, or it can contain query / xpath expressions that indicate exactly
    /// what characteristics events should have.
    /// </summary>
    public class EventLogQuery
    {
        public EventLogQuery(string path, PathType pathType)
            : this(path, pathType, null)
        {
        }

        public EventLogQuery(string path, PathType pathType, string query)
        {
            Session = EventLogSession.GlobalSession;
            Path = path;   // can be null
            ThePathType = pathType;

            if (query == null)
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
            }
            else
            {
                Query = query;
            }
        }

        public EventLogSession Session { get; set; }

        public bool TolerateQueryErrors { get; set; } = false;

        public bool ReverseDirection { get; set; } = false;

        internal string Path { get; }

        internal PathType ThePathType { get; }

        internal string Query { get; }
    }
}
