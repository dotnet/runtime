// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SerializationTypes.CoreCLR
{
    [KnownType(typeof(List<SimpleType>))]
    [KnownType(typeof(SimpleType[]))]
    [DataContract]
    public class TypeWithKnownTypesOfCollectionsWithConflictingXmlName
    {
        [DataMember]
        public object Value1 = new List<SimpleType>();

        [DataMember]
        public object Value2 = new SimpleType[1];

    }
}
