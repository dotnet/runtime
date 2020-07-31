// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Xml.Schema;

namespace System.Xml
{
    internal interface IValidationEventHandling
    {
        // This is a ValidationEventHandler, but it is not strongly typed due to dependencies on System.Xml.Schema
        object? EventHandler { get; }

        // The exception is XmlSchemaException, but it is not strongly typed due to dependencies on System.Xml.Schema
        void SendEvent(Exception exception, XmlSeverityType severity);
    }
}
