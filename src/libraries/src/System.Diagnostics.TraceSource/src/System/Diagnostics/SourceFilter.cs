// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace System.Diagnostics
{
    public class SourceFilter : TraceFilter
    {
        private string _src;

        public SourceFilter(string source)
        {
            Source = source;
        }

        public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage,
                                         object[] args, object data1, object[] data)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return String.Equals(_src, source);
        }

        public String Source
        {
            get
            {
                return _src;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("source");
                _src = value;
            }
        }
    }
}

