// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.Serialization;

namespace System.Configuration
{
    public class SettingsAttributeDictionary : Hashtable
    {
        public SettingsAttributeDictionary() : base() { }

        public SettingsAttributeDictionary(SettingsAttributeDictionary attributes) : base(attributes) { }

        protected SettingsAttributeDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
