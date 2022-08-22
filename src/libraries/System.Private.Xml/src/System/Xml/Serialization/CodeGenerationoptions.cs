// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Serialization
{
    /// <summary>
    /// Specifies various flavours of XmlCodeExporter generated code.
    /// </summary>
    [Flags]
    public enum CodeGenerationOptions
    {
        /// <summary>
        /// Default: use clr primitives for xsd primitives, generate fields and arrays.
        /// </summary>
        [XmlIgnore]
        None = 0,

        /// <summary>
        /// Generate propertyes instead of fields
        /// </summary>
        [XmlEnum("properties")]
        GenerateProperties = 0x1,

        /// <summary>
        /// Generate new RAD asynchronous pattern. The feature allows customers to use an event-based model for invoking Web services asynchronously
        /// </summary>
        [XmlEnum("newAsync")]
        GenerateNewAsync = 0x2,

        /// <summary>
        /// Generate old asynchronous pattern: BeginXXX/EndXXX.
        /// </summary>
        [XmlEnum("oldAsync")]
        GenerateOldAsync = 0x4,


        /// <summary>
        /// Generate OM using explicit ordering feature.
        /// </summary>
        [XmlEnum("order")]
        GenerateOrder = 0x08,

        /// <summary>
        /// Generate OM INotifyPropertyChanged interface to enable data binding.
        /// </summary>
        [XmlEnum("enableDataBinding")]
        EnableDataBinding = 0x10,
    }
}
