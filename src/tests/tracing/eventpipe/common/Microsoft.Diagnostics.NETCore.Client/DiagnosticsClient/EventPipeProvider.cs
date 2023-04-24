// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public sealed class EventPipeProvider
    {
        public EventPipeProvider(string name, EventLevel eventLevel, long keywords = 0xF00000000000, IDictionary<string, string> arguments = null)
        {
            Name = name;
            EventLevel = eventLevel;
            Keywords = keywords;
            Arguments = arguments;
        }

        public long Keywords { get; }

        public EventLevel EventLevel { get; }

        public string Name { get; }

        public IDictionary<string, string> Arguments { get; }

        public override string ToString()
        {
            return $"{Name}:0x{Keywords:X16}:{(uint)EventLevel}{(Arguments == null ? "" : $":{GetArgumentString()}")}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            return this == (EventPipeProvider)obj;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash ^= this.Name.GetHashCode();
            hash ^= this.Keywords.GetHashCode();
            hash ^= this.EventLevel.GetHashCode();
            hash ^= GetArgumentString().GetHashCode();
            return hash;
        }

        public static bool operator ==(EventPipeProvider left, EventPipeProvider right)
        {
            return left.ToString() == right.ToString();
        }

        public static bool operator !=(EventPipeProvider left, EventPipeProvider right)
        {
            return !(left == right);    
        }

        internal string GetArgumentString()
        {
            if (Arguments == null)
            {
                return "";
            }
            return string.Join(";", Arguments.Select(a => {
                var escapedKey = a.Key.Contains(";") || a.Key.Contains("=") ? $"\"{a.Key}\"" : a.Key;
                var escapedValue = a.Value.Contains(";") || a.Value.Contains("=") ? $"\"{a.Value}\"" : a.Value;
                return $"{escapedKey}={escapedValue}";
            }));
        }

    }
}
