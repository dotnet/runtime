// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
            Response obj = new Response();
            obj.DataUpdates.DataUpdateInfo.Add(new DataUpdatesDataUpdateInfo()
            {
                DataDate = new DateTime(2009, 4, 13),
                DataType = "Data",
                LastUpdatedDate = new DateTime(2010, 12, 12)
            });
            obj.DataUpdates.DataUpdateInfo.Add(new DataUpdatesDataUpdateInfo()
            {
                DataDate = new DateTime(2009, 4, 14),
                DataType = "Data",
                LastUpdatedDate = new DateTime(2010, 12, 12)
            });

            using StringWriter writer = new StringWriter();
            new XmlSerializer(typeof(Response)).Serialize(writer, obj);
            string serialized = writer.ToString();

            if (serialized.Contains("<Response") &&
                serialized.Contains("<DataUpdates>") &&
                serialized.Contains(@"<DataUpdateInfo DataDate=""2009-04-13T00:00:00"" DataType=""Data"" LastUpdatedDate=""2010-12-12T00:00:00"" />") &&
                serialized.Contains(@"<DataUpdateInfo DataDate=""2009-04-14T00:00:00"" DataType=""Data"" LastUpdatedDate=""2010-12-12T00:00:00"" />"))
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
