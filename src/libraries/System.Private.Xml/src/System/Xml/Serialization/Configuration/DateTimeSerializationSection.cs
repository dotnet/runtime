// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization.Configuration
{
    using System;
    using System.Configuration;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;

    internal sealed class DateTimeSerializationSection
    {
        public enum DateTimeSerializationMode
        {
            Default = 0,
            Roundtrip = 1,
            Local = 2,
        }

        public DateTimeSerializationSection()
        {
        }
    }
}
