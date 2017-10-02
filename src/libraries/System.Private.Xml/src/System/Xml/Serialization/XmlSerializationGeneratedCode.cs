// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System;
    using System.IO;
    using System.Collections;
    using System.ComponentModel;
    using System.Threading;
    using System.Reflection;
    using System.Security;
    using System.Globalization;

    ///<internalonly/>
#if XMLSERIALIZERGENERATOR
    internal abstract class XmlSerializationGeneratedCode
#else
    public abstract class XmlSerializationGeneratedCode
#endif
    {
        internal void Init(TempAssembly tempAssembly)
        {
        }

        // this method must be called at the end of serialization
        internal void Dispose()
        {
        }
    }
}
