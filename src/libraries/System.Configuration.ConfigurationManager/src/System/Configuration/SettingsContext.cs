// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Serialization;

namespace System.Configuration
{
    public class SettingsContext : Hashtable
    {
        public SettingsContext() : base() { }

        protected SettingsContext(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
