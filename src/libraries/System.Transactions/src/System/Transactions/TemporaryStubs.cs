// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

// TODO #9582: Replace with real IFormatter / BinaryFormatter when available

namespace System.Runtime.Serialization
{
    internal interface IFormatter
    {
        object Deserialize(Stream serializationStream);
        void Serialize(Stream serializationStream, object graph);
    }
}

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal class BinaryFormatter : IFormatter
    {
        public object Deserialize(Stream serializationStream)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            throw new NotImplementedException();
        }
    }
}
