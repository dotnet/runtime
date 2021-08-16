// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Schema;

namespace System.Xml.Serialization.TrimmingTests
{
    internal class Program
    {
        // Preserve these types until XmlSerializer is fully trim-safe.
        // see https://github.com/dotnet/runtime/issues/44768
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Response))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataUpdates))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataUpdatesDataUpdateInfo))]
        public static int Main()
        {
            using StringReader stringReader = new StringReader(@"<?xml version=""1.0"" encoding=""UTF-8""?>
				<Response>
				    <DataUpdates>
				        <DataUpdateInfo DataDate=""2009-04-13T00:00:00"" DataType=""Data"" LastUpdatedDate=""2010-12-12T02:53:19.257"" />
				        <DataUpdateInfo DataDate=""2009-04-14T00:00:00"" DataType=""Data"" LastUpdatedDate=""2010-12-12T02:53:19.257"" />
				        <DataUpdateInfo DataDate=""2009-04-15T00:00:00"" DataType=""Data"" LastUpdatedDate=""2010-12-12T01:52:51.047"" />
				    </DataUpdates>
				</Response>");

            Response obj = (Response)new XmlSerializer(typeof(Response)).Deserialize(stringReader);
            if (obj.DataUpdates.DataUpdateInfo.Count == 3 &&
                obj.DataUpdates.DataUpdateInfo.All(i => i.DataDate.Year == 2009 && i.LastUpdatedDate.Year == 2010))
            {
                return 100;
            }

            return -1;
        }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class Response
    {
        public Response()
        {
            this.DataUpdates = new DataUpdates();
        }

        [XmlElement(Order = 0)]
        public DataUpdates DataUpdates { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class DataUpdates
    {
        public DataUpdates()
        {
            this.DataUpdateInfo = new List<DataUpdatesDataUpdateInfo>();
        }

        [XmlElement("DataUpdateInfo", Form = XmlSchemaForm.Unqualified, Order = 0)]
        public List<DataUpdatesDataUpdateInfo> DataUpdateInfo { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true)]
    public class DataUpdatesDataUpdateInfo
    {
        public DataUpdatesDataUpdateInfo()
        {
        }

        [XmlAttribute]
        public DateTime DataDate { get; set; }

        [XmlAttribute]
        public string DataType { get; set; }

        [XmlAttribute]
        public DateTime LastUpdatedDate { get; set; }
    }
}
