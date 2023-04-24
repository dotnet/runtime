// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class SerializationInfoTests
    {
        [Fact]
        public void SerializationInfoAddGet()
        {
            var value = new Serializable();
            var si = new SerializationInfo(typeof(Serializable), new FormatterConverter());
            var sc = new StreamingContext();
            value.GetObjectData(si, sc);

            Assert.Equal(typeof(Serializable), si.ObjectType);
            Assert.Equal(typeof(Serializable).FullName, si.FullTypeName);
            Assert.Equal(typeof(Serializable).Assembly.FullName, si.AssemblyName);

            Assert.Equal(15, si.MemberCount);

            Assert.True(si.GetBoolean("bool"));
            Assert.Equal("hello", si.GetString("string"));
            Assert.Equal('a', si.GetChar("char"));

            Assert.Equal(byte.MaxValue, si.GetByte("byte"));

            Assert.Equal(decimal.MaxValue, si.GetDecimal("decimal"));
            Assert.Equal(double.MaxValue, si.GetDouble("double"));
            Assert.Equal(short.MaxValue, si.GetInt16("short"));
            Assert.Equal(int.MaxValue, si.GetInt32("int"));
            Assert.Equal(long.MaxValue, si.GetInt64("long"));
            Assert.Equal(sbyte.MaxValue, si.GetSByte("sbyte"));
            Assert.Equal(float.MaxValue, si.GetSingle("float"));
            Assert.Equal(ushort.MaxValue, si.GetUInt16("ushort"));
            Assert.Equal(uint.MaxValue, si.GetUInt32("uint"));
            Assert.Equal(ulong.MaxValue, si.GetUInt64("ulong"));
            Assert.Equal(DateTime.MaxValue, si.GetDateTime("datetime"));
        }

        [Fact]
        public void SerializationInfoEnumerate()
        {
            var value = new Serializable();
            var si = new SerializationInfo(typeof(Serializable), new FormatterConverter());
            var sc = new StreamingContext();
            value.GetObjectData(si, sc);

            int items = 0;
            foreach (SerializationEntry entry in si)
            {
                items++;
                switch (entry.Name)
                {
                    case "int":
                        Assert.Equal(int.MaxValue, (int)entry.Value);
                        Assert.Equal(typeof(int), entry.ObjectType);
                        break;
                    case "string":
                        Assert.Equal("hello", (string)entry.Value);
                        Assert.Equal(typeof(string), entry.ObjectType);
                        break;
                    case "bool":
                        Assert.True((bool)entry.Value);
                        Assert.Equal(typeof(bool), entry.ObjectType);
                        break;
                }
            }

            Assert.Equal(si.MemberCount, items);
        }

        [Fact]
        public void NegativeAddValueTwice()
        {
            var si = new SerializationInfo(typeof(Serializable), new FormatterConverter());
            si.AddValue("bool", true);
            Assert.Throws<SerializationException>(() => si.AddValue("bool", true));
        }

        [Fact]
        public void NegativeValueNotFound()
        {
            var si = new SerializationInfo(typeof(Serializable), new FormatterConverter());
            si.AddValue("a", 1);
            Assert.Throws<SerializationException>(() => si.GetInt32("b"));
        }
    }

    [Serializable]
    internal class Serializable : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("string", "hello");
            info.AddValue("bool", true);
            info.AddValue("char", 'a');
            info.AddValue("byte", byte.MaxValue);
            info.AddValue("decimal", decimal.MaxValue);
            info.AddValue("double", double.MaxValue);
            info.AddValue("short", short.MaxValue);
            info.AddValue("int", int.MaxValue);
            info.AddValue("long", long.MaxValue);
            info.AddValue("sbyte", sbyte.MaxValue);
            info.AddValue("float", float.MaxValue);
            info.AddValue("ushort", ushort.MaxValue);
            info.AddValue("uint", uint.MaxValue);
            info.AddValue("ulong", ulong.MaxValue);
            info.AddValue("datetime", DateTime.MaxValue);
        }
    }
}
