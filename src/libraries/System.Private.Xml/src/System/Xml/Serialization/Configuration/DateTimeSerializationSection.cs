// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Reflection;

namespace System.Xml.Serialization.Configuration
{
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
