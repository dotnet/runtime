// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Resources.ResourceWriterTests
{
    public class ResourceReaderTests
    {
        private const int ResSetVersion = 2;
        private const int ResourceManagerHeaderVersionNumber = 1;
        private const int ResourceManagerMagicNumber = unchecked((int)0xBEEFCACE);
        private const string ResourceReaderFullyQualifiedName = "System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private static Dictionary<string, string> s_dict = new Dictionary<string, string>{
                                                                           { "name1", "value1"},
                                                                           { "name2", "value2"},
                                                                           { "name3", "value3"}
                                                                         };
        public static ResourceWriter GenerateResourceStream(Dictionary<string, string> inp_dict, MemoryStream ms)
        {

            ResourceWriter rw = new ResourceWriter(ms);
            foreach (KeyValuePair<string, string> e in inp_dict)
            {
                string name = e.Key;
                string values = e.Value;
                rw.AddResource(name, values);
            }
            rw.Generate();
            ms.Seek(0L, SeekOrigin.Begin);
            return rw;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(100)]
        public static void ReadResource(int numberOfLeadingBytes)
        {
            var buffer = new byte[4096];
            using (var ms2 = new MemoryStream(buffer, true))
            {
                ms2.Write(new byte[numberOfLeadingBytes], 0, numberOfLeadingBytes);
                using (var rw = GenerateResourceStream(s_dict, ms2))
                {
                    //Rewind to beginning of stream

                    ms2.Seek(numberOfLeadingBytes, SeekOrigin.Begin);

                    var reder = new ResourceReader(ms2);

                    var s_found_list = new List<string>();
                    foreach (DictionaryEntry entry in reder)
                    {
                        string key = (string)entry.Key;
                        string value = (string)entry.Value;
                        string found = s_dict[key];
                        Assert.True(string.Compare(value, found) == 0, "expected: " + value + ", but got : " + found);
                        s_found_list.Add(key);
                    }

                    Assert.True(s_found_list.Count == s_dict.Count);
                }
            }
        }

        [Fact]
        public static void ReadResource1()
        {

            using (var ms2 = new MemoryStream())
            {
                using (var rw = GenerateResourceStream(s_dict, ms2))
                {
                    //Rewind to beginning of stream

                    ms2.Seek(0L, SeekOrigin.Begin);

                    var reader = new ResourceReader(ms2);

                    var s_found_key = new List<string>();
                    var s_found_value = new List<string>();
                    var enume = reader.GetEnumerator();

                    while (enume.MoveNext())
                    {
                        s_found_key.Add((string)enume.Key);
                        s_found_value.Add((string)enume.Value);
                    }
                    enume.Reset();
                    int i = 0;
                    while (enume.MoveNext())
                    {
                        string key = s_found_key[i];
                        string found_key = (string)enume.Key;

                        string value = s_found_key[i];
                        string found_value = (string)enume.Key;
                        i++;
                        Assert.True(string.Compare(key, found_key) == 0, "expected: " + key + ", but got : " + found_key);
                        Assert.True(string.Compare(value, found_value) == 0, "expected: " + value + ", but got : " + found_value);
                    }


                    Assert.True(s_found_key.Count == s_dict.Count);
                }
            }

        }

        [Fact]
        public static void ExceptionforResourceReaderNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MemoryStream ms2 = null;
                var rw = new ResourceReader(ms2);
            });
        }

        [Fact]
        public static void ExceptionforResourceReaderDispose01()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        rr1.Dispose();
                        var s_found_list = new List<string>();
                        foreach (DictionaryEntry entry in rr1)
                        {
                            string key = (string)entry.Key;
                            string value = (string)entry.Value;
                            string found = s_dict[key];
                            Assert.True(string.Compare(value, found) == 0, "expected: " + value + ", but got : " + found);
                            s_found_list.Add(key);
                        }

                    }
                }
            });
        }

        [Fact]
        public static void Exception_Enumarator_Current_Dispose()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        IDictionaryEnumerator enumarator = rr1.GetEnumerator();
                        rr1.Dispose();
                        var shouldnotgethere = enumarator.Current;


                    }
                }
            });
        }
        [Fact]
        public static void Exception_Enumarator_Entry()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        IDictionaryEnumerator enumarator = rr1.GetEnumerator();
                        rr1.Dispose();
                        var shouldnotgethere = enumarator.Entry;


                    }
                }
            });
        }
        [Fact]
        public static void Exception_Enumerator_Key()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        IDictionaryEnumerator enumarator = rr1.GetEnumerator();
                        rr1.Dispose();
                        var shouldnotgethere = enumarator.Key;


                    }
                }
            });
        }
        [Fact]
        public static void Exception_Enumerator_Reset()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        IDictionaryEnumerator enumarator = rr1.GetEnumerator();
                        rr1.Dispose();
                        enumarator.Reset();


                    }
                }
            });
        }
        [Fact]
        public static void Exception_Enumerator_Value()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var ms2 = new MemoryStream())
                {
                    using (var rw = GenerateResourceStream(s_dict, ms2))
                    {
                        ms2.Seek(0L, SeekOrigin.Begin);
                        var rr1 = new ResourceReader(ms2);

                        IDictionaryEnumerator enumarator = rr1.GetEnumerator();
                        rr1.Dispose();
                        var shouldnotgethere = enumarator.Value;


                    }
                }
            });
        }

        [Fact]
        public static void ReadV1Resources()
        {
            //NOte: The below ByteStream was generated by V1 framework resource writer from the below dictionary
            byte[] _RefBuffer1 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 88, 0, 0, 0, 41, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 45, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 1, 0, 0, 0, 12, 0, 0, 0, 1, 0, 0, 0, 23, 83, 121, 115, 116, 101, 109, 46, 83, 116, 114, 105, 110, 103, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 129, 56, 148, 171, 124, 133, 198, 205, 9, 176, 129, 210, 193, 133, 199, 216, 245, 127, 11, 239, 60, 90, 212, 15, 202, 130, 229, 15, 241, 187, 198, 30, 88, 117, 120, 32, 253, 221, 196, 54, 242, 213, 88, 62, 206, 25, 176, 106, 0, 0, 0, 0, 47, 0, 0, 0, 108, 0, 0, 0, 153, 0, 0, 0, 216, 0, 0, 0, 255, 0, 0, 0, 76, 1, 0, 0, 155, 1, 0, 0, 186, 1, 0, 0, 251, 1, 0, 0, 62, 2, 0, 0, 129, 2, 0, 0, 146, 3, 0, 0, 42, 65, 0, 114, 0, 103, 0, 95, 0, 73, 0, 110, 0, 118, 0, 97, 0, 108, 0, 105, 0, 100, 0, 77, 0, 101, 0, 109, 0, 98, 0, 101, 0, 114, 0, 73, 0, 110, 0, 102, 0, 111, 0, 0, 0, 0, 0, 56, 73, 0, 110, 0, 118, 0, 97, 0, 108, 0, 105, 0, 100, 0, 79, 0, 112, 0, 95, 0, 71, 0, 101, 0, 116, 0, 73, 0, 100, 0, 68, 0, 105, 0, 102, 0, 102, 0, 70, 0, 114, 0, 111, 0, 109, 0, 77, 0, 101, 0, 109, 0, 73, 0, 100, 0, 59, 0, 0, 0, 40, 65, 0, 114, 0, 103, 0, 95, 0, 69, 0, 110, 0, 117, 0, 109, 0, 78, 0, 111, 0, 116, 0, 67, 0, 108, 0, 111, 0, 110, 0, 101, 0, 97, 0, 98, 0, 108, 0, 101, 0, 136, 0, 0, 0, 58, 65, 0, 114, 0, 103, 0, 95, 0, 84, 0, 97, 0, 114, 0, 103, 0, 101, 0, 116, 0, 78, 0, 111, 0, 116, 0, 86, 0, 97, 0, 108, 0, 105, 0, 100, 0, 70, 0, 111, 0, 114, 0, 73, 0, 82, 0, 101, 0, 102, 0, 108, 0, 101, 0, 99, 0, 116, 0, 170, 0, 0, 0, 34, 65, 0, 114, 0, 103, 0, 95, 0, 71, 0, 101, 0, 116, 0, 77, 0, 101, 0, 116, 0, 104, 0, 78, 0, 111, 0, 116, 0, 70, 0, 110, 0, 100, 0, 214, 0, 0, 0, 72, 78, 0, 111, 0, 116, 0, 83, 0, 117, 0, 112, 0, 112, 0, 111, 0, 114, 0, 116, 0, 101, 0, 100, 0, 95, 0, 65, 0, 100, 0, 100, 0, 105, 0, 110, 0, 103, 0, 77, 0, 101, 0, 116, 0, 104, 0, 115, 0, 78, 0, 111, 0, 116, 0, 83, 0, 117, 0, 112, 0, 112, 0, 111, 0, 114, 0, 116, 0, 101, 0, 100, 0, 35, 1, 0, 0, 74, 78, 0, 111, 0, 116, 0, 83, 0, 117, 0, 112, 0, 112, 0, 111, 0, 114, 0, 116, 0, 101, 0, 100, 0, 95, 0, 65, 0, 100, 0, 100, 0, 105, 0, 110, 0, 103, 0, 70, 0, 105, 0, 101, 0, 108, 0, 100, 0, 115, 0, 78, 0, 111, 0, 116, 0, 83, 0, 117, 0, 112, 0, 112, 0, 111, 0, 114, 0, 116, 0, 101, 0, 100, 0, 81, 1, 0, 0, 26, 77, 0, 105, 0, 115, 0, 115, 0, 105, 0, 110, 0, 103, 0, 77, 0, 101, 0, 109, 0, 98, 0, 101, 0, 114, 0, 126, 1, 0, 0, 60, 73, 0, 110, 0, 118, 0, 97, 0, 108, 0, 105, 0, 100, 0, 67, 0, 97, 0, 115, 0, 116, 0, 95, 0, 81, 0, 73, 0, 70, 0, 111, 0, 114, 0, 69, 0, 110, 0, 117, 0, 109, 0, 86, 0, 97, 0, 114, 0, 70, 0, 97, 0, 105, 0, 108, 0, 101, 0, 100, 0, 145, 1, 0, 0, 62, 73, 0, 110, 0, 118, 0, 97, 0, 108, 0, 105, 0, 100, 0, 79, 0, 112, 0, 95, 0, 73, 0, 110, 0, 118, 0, 97, 0, 108, 0, 105, 0, 100, 0, 78, 0, 101, 0, 119, 0, 69, 0, 110, 0, 117, 0, 109, 0, 86, 0, 97, 0, 114, 0, 105, 0, 97, 0, 110, 0, 116, 0, 198, 1, 0, 0, 62, 65, 0, 114, 0, 103, 0, 95, 0, 79, 0, 98, 0, 106, 0, 101, 0, 99, 0, 116, 0, 78, 0, 111, 0, 116, 0, 86, 0, 97, 0, 108, 0, 105, 0, 100, 0, 70, 0, 111, 0, 114, 0, 77, 0, 101, 0, 116, 0, 104, 0, 111, 0, 100, 0, 73, 0, 110, 0, 102, 0, 111, 0, 11, 2, 0, 0, 32, 65, 0, 114, 0, 103, 0, 95, 0, 78, 0, 111, 0, 65, 0, 99, 0, 99, 0, 101, 0, 115, 0, 115, 0, 83, 0, 112, 0, 101, 0, 99, 0, 58, 2, 0, 0, 0, 57, 84, 104, 101, 32, 115, 112, 101, 99, 105, 102, 105, 101, 100, 32, 109, 101, 109, 98, 101, 114, 32, 105, 110, 102, 111, 32, 105, 115, 32, 110, 111, 116, 32, 118, 97, 108, 105, 100, 32, 102, 111, 114, 32, 116, 104, 105, 115, 32, 73, 82, 101, 102, 108, 101, 99, 116, 46, 0, 75, 84, 104, 101, 32, 68, 73, 83, 80, 73, 68, 32, 114, 101, 116, 117, 114, 110, 101, 100, 32, 102, 114, 111, 109, 32, 71, 101, 116, 73, 68, 115, 79, 102, 78, 97, 109, 101, 115, 32, 105, 115, 32, 100, 105, 102, 102, 101, 114, 101, 110, 116, 32, 102, 114, 111, 109, 32, 116, 104, 101, 32, 99, 97, 99, 104, 101, 100, 32, 68, 73, 83, 80, 73, 68, 46, 0, 32, 84, 104, 101, 32, 101, 110, 117, 109, 101, 114, 97, 116, 111, 114, 32, 105, 115, 32, 110, 111, 116, 32, 99, 108, 111, 110, 101, 97, 98, 108, 101, 46, 0, 42, 84, 104, 101, 32, 116, 97, 114, 103, 101, 116, 32, 105, 115, 32, 110, 111, 116, 32, 118, 97, 108, 105, 100, 32, 102, 111, 114, 32, 116, 104, 105, 115, 32, 73, 82, 101, 102, 108, 101, 99, 116, 46, 0, 75, 67, 97, 110, 110, 111, 116, 32, 103, 101, 116, 32, 116, 104, 101, 32, 118, 97, 108, 117, 101, 32, 102, 111, 114, 32, 116, 104, 101, 32, 112, 114, 111, 112, 101, 114, 116, 121, 32, 105, 110, 102, 111, 32, 115, 105, 110, 99, 101, 32, 105, 116, 32, 100, 111, 101, 115, 32, 110, 111, 116, 32, 104, 97, 118, 101, 32, 97, 32, 103, 101, 116, 116, 101, 114, 46, 0, 44, 73, 68, 105, 115, 112, 97, 116, 99, 104, 69, 120, 32, 100, 111, 101, 115, 32, 110, 111, 116, 32, 115, 117, 112, 112, 111, 114, 116, 32, 97, 100, 100, 105, 110, 103, 32, 109, 101, 116, 104, 111, 100, 115, 46, 0, 43, 73, 68, 105, 115, 112, 97, 116, 99, 104, 69, 120, 32, 100, 111, 101, 115, 32, 110, 111, 116, 32, 115, 117, 112, 112, 111, 114, 116, 32, 97, 100, 100, 105, 110, 103, 32, 102, 105, 101, 108, 100, 115, 46, 0, 17, 77, 101, 109, 98, 101, 114, 32, 110, 111, 116, 32, 102, 111, 117, 110, 100, 46, 0, 51, 81, 73, 32, 102, 111, 114, 32, 73, 69, 110, 117, 109, 86, 65, 82, 73, 65, 78, 84, 32, 102, 97, 105, 108, 101, 100, 32, 111, 110, 32, 116, 104, 101, 32, 117, 110, 109, 97, 110, 97, 103, 101, 100, 32, 115, 101, 114, 118, 101, 114, 46, 0, 67, 86, 97, 114, 105, 97, 110, 116, 32, 114, 101, 116, 117, 114, 110, 101, 100, 32, 102, 114, 111, 109, 32, 73, 110, 118, 111, 107, 101, 40, 41, 32, 111, 110, 32, 109, 101, 109, 98, 101, 114, 32, 68, 73, 83, 80, 73, 68, 95, 78, 69, 87, 69, 78, 85, 77, 32, 105, 115, 32, 105, 110, 118, 97, 108, 105, 100, 46, 0, 45, 84, 104, 101, 32, 111, 98, 106, 101, 99, 116, 32, 105, 115, 32, 110, 111, 116, 32, 118, 97, 108, 105, 100, 32, 102, 111, 114, 32, 116, 104, 105, 115, 32, 109, 101, 116, 104, 111, 100, 32, 105, 110, 102, 111, 46, 0, 68, 77, 117, 115, 116, 32, 115, 112, 101, 99, 105, 102, 121, 32, 98, 105, 110, 100, 105, 110, 103, 32, 102, 108, 97, 103, 115, 32, 100, 101, 115, 99, 114, 105, 98, 105, 110, 103, 32, 116, 104, 101, 32, 105, 110, 118, 111, 107, 101, 32, 111, 112, 101, 114, 97, 116, 105, 111, 110, 32, 114, 101, 113, 117, 105, 114, 101, 100, 46, };

            Dictionary<string, string> s_dictv1 = new Dictionary<string, string>
             {
                { "Arg_InvalidMemberInfo","The specified member info is not valid for this IReflect."},
                {"InvalidOp_GetIdDiffFromMemId","The DISPID returned from GetIDsOfNames is different from the cached DISPID."},
                {"Arg_EnumNotCloneable","The enumerator is not cloneable."},
                {"Arg_TargetNotValidForIReflect","The target is not valid for this IReflect."},
                {"Arg_GetMethNotFnd","Cannot get the value for the property info since it does not have a getter."},
                {"NotSupported_AddingMethsNotSupported","IDispatchEx does not support adding methods."},
                {"NotSupported_AddingFieldsNotSupported","IDispatchEx does not support adding fields."},
                {"MissingMember","Member not found."},
                {"InvalidCast_QIForEnumVarFailed","QI for IEnumVARIANT failed on the unmanaged server."},
                {"InvalidOp_InvalidNewEnumVariant","Variant returned from Invoke() on member DISPID_NEWENUM is invalid."},
                {"Arg_ObjectNotValidForMethodInfo","The object is not valid for this method info."},
                {"Arg_NoAccessSpec","Must specify binding flags describing the invoke operation required."}

             };

            using (var ms2 = new MemoryStream(_RefBuffer1))
            {
                using (var rw = new ResourceReader(ms2))
                {
                    //Rewind to beginning of stream

                    ms2.Seek(0L, SeekOrigin.Begin);

                    var reder = new ResourceReader(ms2);

                    var s_found_list = new List<string>();
                    foreach (DictionaryEntry entry in reder)
                    {
                        string key = (string)entry.Key;
                        string value = (string)entry.Value;
                        string found = s_dictv1[key];
                        Assert.True(string.Compare(value, found) == 0, "expected: " + value + ", but got : " + found);
                        s_found_list.Add(key);
                    }

                    Assert.True(s_found_list.Count == s_dictv1.Count);
                }

            }
        }

        [Fact]
        public static void Exception_incorrect_magic_number()
        {
            using (var ms2 = new MemoryStream())
            {
                using (var rw = GenerateResourceStream(s_dict, ms2))
                {
                    ms2.Seek(0L, SeekOrigin.Begin);
                    using (var bw = new BinaryWriter(ms2))
                    {
                        bw.Write(2000);//Our resource reader expects the ResourceManager magic number here

                        //  ms2.Seek(0L, SeekOrigin.Begin);
                        AssertExtensions.Throws<ArgumentException>(null, () =>
                        {
                            var resReader = new ResourceReader(ms2);
                        });
                    }
                }
            }
        }
        [Fact]
        public static void Exception_Corrupted_resources()
        {
            byte[] _RefBuffer12 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50 };
            using (var ms2 = new MemoryStream(_RefBuffer12))
            {
                using (var reader = new ResourceReader(ms2))
                {

                    var s_found_list = new List<string>();
                    Assert.Throws<FormatException>(() =>
                    {
                        foreach (DictionaryEntry entry in reader)
                        {
                            string key = (string)entry.Key;
                            s_found_list.Add(key);
                        }
                    });
                }
            }
        }
        [Fact]
        public static void Exception_Corrupted_resources1()
        {
            byte[] _RefBuffer12 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50, 1};
            using (var ms2 = new MemoryStream(_RefBuffer12))
            {
                using (var reader = new ResourceReader(ms2))
                {
                    var s_found_list = new List<DictionaryEntry>();
                    Assert.Throws<BadImageFormatException>(() =>
                    {
                        var enume = reader.GetEnumerator();

                        while (enume.MoveNext())
                        {
                            s_found_list.Add(enume.Entry);
                        }

                    });
                }
            }
        }
        [Fact]
        public static void Exception_EOF()
        {
            byte[] _RefBuffer12 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109};
            using (var ms2 = new MemoryStream(_RefBuffer12))
            {
                Assert.Throws<BadImageFormatException>(() =>
                {
                    using (var rw = new ResourceReader(ms2))
                    {

                    }
                });
               
            }
        }
        [Fact]
        public static void Read_resources_withobject()
        {
            //The following byte array corresponds to  
            //rw.AddResource("name1", "value1");
            //rw.AddResource("name2", (object)bytearray);
            //rw.AddResource("name3", "value3");

            byte[] _RefBuffer12 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 30, 1, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 32, 17, 1, 0, 0, 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50, 1, 6, 118, 97, 108, 117, 101, 51, 1, 6, 118, 97, 108, 117, 101, 51 };

            
            Dictionary<string, object> s_dict_expected = new Dictionary<string, object>
            {
                { "name1", "value1"},
                { "name2", new byte[]{
                    206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50, 1, 6, 118, 97, 108, 117, 101, 51
                }},
                { "name3", "value3"}
            };
            using (var ms2 = new MemoryStream(_RefBuffer12))
            {
                using (var rw = new ResourceReader(ms2))
                {

                    ms2.Seek(0L, SeekOrigin.Begin);

                    var reader = new ResourceReader(ms2);

                    var s_found_list = new List<string>();

                    foreach (DictionaryEntry entry in reader)
                    {
                        string key = (string)entry.Key;
                        object actual = entry.Value;
                        object expected = s_dict_expected[key];
                        Assert.Equal(expected, actual);
                        s_found_list.Add(key);
                    }
                }
            }
        }

        [Fact]
        public static void Exception_opening_opened_resources()
        {
            byte[] _RefBuffer12 = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50 };
            using (var ms2 = new MemoryStream(_RefBuffer12))
            {
                using (var rw = new ResourceReader(ms2))
                {

                   
                    AssertExtensions.Throws<ArgumentException>(null, () =>
                    {
                        var rr = new ResourceReader(ms2);
                    });
                }
            }
        }

    }


}

