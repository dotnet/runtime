// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: IFormatter;
**
**
** Purpose: The interface for all formatters.
**
**
===========================================================*/
namespace System.Runtime.Serialization {
    using System.Runtime.Remoting;
    using System;
    using System.IO;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface IFormatter {
        Object Deserialize(Stream serializationStream);

        void Serialize(Stream serializationStream, Object graph);


        ISurrogateSelector SurrogateSelector {
            get;
            set;
        }

        SerializationBinder Binder {
            get;
            set;
        }

        StreamingContext Context {
            get;
            set;
        }
    }
}
