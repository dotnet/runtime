// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace System.Xml.Serialization.TrimmingTests
{
    /// <summary>
    /// Tests that using XmlSerializer with linker option '--enable-opt sealer' works
    /// when IsDynamicCodeSupported==false.
    /// </summary>
    internal class Program
    {
        // Preserve these types until XmlSerializer is fully trim-safe.
        // see https://github.com/dotnet/runtime/issues/44768
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Response))]
        public static int Main()
        {
            // simulate IsDynamicCodeSupported==false by setting the SerializationMode to ReflectionOnly
            const int ReflectionOnly = 1;
            typeof(XmlSerializer).GetField("s_mode", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, ReflectionOnly);

            using StringReader stringReader = new StringReader(@"<?xml version=""1.0"" encoding=""UTF-8""?>
				<Response DataType=""Data"">
				</Response>");

            Response obj = (Response)new XmlSerializer(typeof(Response)).Deserialize(stringReader);
            if (obj.DataType == "Data")
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
        [XmlAttribute]
        public string DataType { get; set; }
    }
}
