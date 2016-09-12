// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class SerializationBinderTests
    {
        [Fact]
        public void BindToName_NullDefaults()
        {
            var b = new TrackAllBindToTypes();
            string assemblyName, typeName;
            b.BindToName(typeof(string), out assemblyName, out typeName);
            Assert.Null(assemblyName);
            Assert.Null(typeName);
        }

        [Fact]
        public void BindToType_AllValuesTracked()
        {
            var s = new MemoryStream();
            var f = new BinaryFormatter();

            f.Serialize(s, DayOfWeek.Monday);
            s.Position = 0;

            var t = new TrackAllBindToTypes();
            f.Binder = t;
            f.Deserialize(s);

            Assert.Contains(t.Binds, kvp => kvp.Value.Contains("System.DayOfWeek"));
        }

        private class TrackAllBindToTypes : SerializationBinder
        {
            public readonly List<KeyValuePair<string, string>> Binds = new List<KeyValuePair<string, string>>();

            public override Type BindToType(string assemblyName, string typeName)
            {
                Binds.Add(new KeyValuePair<string, string>(assemblyName, typeName));
                return Assembly.Load(new AssemblyName(assemblyName)).GetType(typeName);
            }
        }
    }
}
