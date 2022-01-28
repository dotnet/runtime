// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace System.Resources
{
    public partial class ResourceWriter
    {
        // Set this delegate to allow multi-targeting for .resources files.
        // not used by .NETCore since ResourceWriter doesn't support BinaryFormatted resources.
        public Func<Type, string>? TypeNameConverter { get; set; }

        // Adds a resource of type Stream to the list of resources to be
        // written to a file.  They aren't written until Generate() is called.
        // Doesn't close the Stream when done.
        public void AddResource(string name!!, Stream? value)
        {
            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);

            AddResourceInternal(name, value, false);
        }

        public void AddResourceData(string name!!, string typeName!!, byte[] serializedData!!)
        {
            AddResourceData(name, typeName, (object)serializedData);
        }

        private static string ResourceReaderTypeName => ResourceReaderFullyQualifiedName;
        private static string ResourceSetTypeName => ResSetTypeName;

        private static void WriteData(BinaryWriter writer, object dataContext)
        {
            byte[]? data = dataContext as byte[];

            Debug.Assert(data != null);

            writer.Write(data);
        }
    }
}
