// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public class EventSourceCreationData
    {
        private int _categoryCount;

        private EventSourceCreationData() { }

        public EventSourceCreationData(string source, string logName)
        {
            Source = source;
            LogName = logName;
        }

        internal EventSourceCreationData(string source, string logName, string machineName) : this(source, logName)
        {
            MachineName = machineName;
        }

        public string LogName { get; set; } = "Application";

        public string MachineName { get; set; } = ".";

        public string Source { get; set; }

        public string MessageResourceFile { get; set; }

        public string ParameterResourceFile { get; set; }

        public string CategoryResourceFile { get; set; }

        public int CategoryCount
        {
            get { return _categoryCount; }
            set
            {
                if (value > ushort.MaxValue || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(CategoryCount));

                _categoryCount = value;
            }
        }
    }
}
