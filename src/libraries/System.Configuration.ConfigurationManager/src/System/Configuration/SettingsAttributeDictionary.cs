// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Serialization;

namespace System.Configuration
{
    public class SettingsAttributeDictionary : Hashtable
    {
        public SettingsAttributeDictionary() : base() { }

        public SettingsAttributeDictionary(SettingsAttributeDictionary attributes) : base(attributes) { }

        protected SettingsAttributeDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
