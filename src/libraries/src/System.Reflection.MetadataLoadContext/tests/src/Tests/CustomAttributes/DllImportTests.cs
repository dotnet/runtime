// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SampleMetadata;
using Xunit;

#pragma warning disable 0618 // Obsolete

namespace System.Reflection.Tests
{
    public static partial class CustomAttributeTests
    {
        [Fact]
        public static void TestDllImportPseudoCustomAttribute()
        {
            TypeInfo runtimeType = typeof(DllImportHolders).GetTypeInfo();  // Intentionally not projected - using to get expected results.
            MethodInfo[] runtimeMethods = runtimeType.DeclaredMethods.OrderBy(m => m.Name).ToArray();

            TypeInfo ecmaType = runtimeType.Project().GetTypeInfo();
            MethodInfo[] ecmaMethods = ecmaType.DeclaredMethods.OrderBy(m => m.Name).ToArray();

            Assert.Equal(runtimeMethods.Length, ecmaMethods.Length);
            for (int i = 0; i < runtimeMethods.Length; i++)
            {
                DllImportAttribute expected = runtimeMethods[i].GetCustomAttribute<DllImportAttribute>();
                CustomAttributeData cad = ecmaMethods[i].CustomAttributes.Single(c => c.AttributeType.Name == nameof(DllImportAttribute));
                DllImportAttribute actual = cad.UnprojectAndInstantiate<DllImportAttribute>();
                AssertEqual(expected, actual);
            }
        }

        private static void AssertEqual(DllImportAttribute d1, DllImportAttribute d2)
        {
            Assert.Equal(d1.BestFitMapping, d2.BestFitMapping);
            Assert.Equal(d1.CallingConvention, d2.CallingConvention);
            Assert.Equal(d1.CharSet, d2.CharSet);
            Assert.Equal(d1.EntryPoint, d2.EntryPoint);
            Assert.Equal(d1.ExactSpelling, d2.ExactSpelling);
            Assert.Equal(d1.PreserveSig, d2.PreserveSig);
            Assert.Equal(d1.SetLastError, d2.SetLastError);
            Assert.Equal(d1.ThrowOnUnmappableChar, d2.ThrowOnUnmappableChar);
            Assert.Equal(d1.Value, d2.Value);
        }

        [Theory]
        [MemberData(nameof(MarshalAsTheoryData))]
        public static void TestMarshalAsPseudoCustomAttribute(string fieldName, MarshalAsAttribute expected)
        {
            TypeInfo ecmaType = typeof(MarshalAsHolders).Project().GetTypeInfo();
            FieldInfo ecmaField = ecmaType.GetDeclaredField(fieldName);
            Assert.NotNull(ecmaField);
            CustomAttributeData cad = ecmaField.CustomAttributes.Single(c => c.AttributeType.Name == nameof(MarshalAsAttribute));
            MarshalAsAttribute actual = cad.UnprojectAndInstantiate<MarshalAsAttribute>();
            AssertEqual(expected, actual);
        }

        public static IEnumerable<object[]> MarshalAsTheoryData
        {
            get
            {
                yield return new object[]
                {
                     "F1",
                    new MarshalAsAttribute(UnmanagedType.BStr)
                    {
                    },
                };
                yield return new object[]
                {
                     "F2",
                    new MarshalAsAttribute(UnmanagedType.Currency)
                    {
                    },
                };
                yield return new object[]
                {
                     "F3",
                    new MarshalAsAttribute(UnmanagedType.IDispatch)
                    {
                    },
                };
                yield return new object[]
                {
                     "F4",
                    new MarshalAsAttribute(UnmanagedType.IDispatch)
                    {
                        IidParameterIndex = 42,
                    },
                };
                yield return new object[]
                {
                     "F5",
                    new MarshalAsAttribute(UnmanagedType.ByValArray)
                    {
                        SizeConst = 1,
                    },
                };
                yield return new object[]
                {
                     "F6",
                    new MarshalAsAttribute(UnmanagedType.ByValArray)
                    {
                        SizeConst = 5,
                    },
                };
                yield return new object[]
                {
                     "F7",
                    new MarshalAsAttribute(UnmanagedType.ByValArray)
                    {
                        ArraySubType = UnmanagedType.FunctionPtr,
                        SizeConst = 1,
                    },
                };
                yield return new object[]
                {
                     "F8",
                    new MarshalAsAttribute(UnmanagedType.ByValArray)
                    {
                        ArraySubType = UnmanagedType.FunctionPtr,
                        SizeConst = 87,
                    },
                };
                yield return new object[]
                {
                     "F9",
                    new MarshalAsAttribute(UnmanagedType.SafeArray)
                    {
                    },
                };
                yield return new object[]
                {
                     "F10",
                    new MarshalAsAttribute(UnmanagedType.SafeArray)
                    {
                        SafeArraySubType = VarEnum.VT_BSTR,
                    },
                };
                yield return new object[]
                {
                     "F11",
                    new MarshalAsAttribute(UnmanagedType.SafeArray)
                    {
                        SafeArraySubType = VarEnum.VT_RECORD,
                        SafeArrayUserDefinedSubType = typeof(MyUdt),
                    },
                };
                yield return new object[]
                {
                     "F12",
                    new MarshalAsAttribute(UnmanagedType.SafeArray)
                    {
                        SafeArraySubType = VarEnum.VT_RECORD,
                        SafeArrayUserDefinedSubType = typeof(DateTime),
                    },
                };
                yield return new object[]
                {
                     "F13",
                    new MarshalAsAttribute(UnmanagedType.LPArray)
                    {
                        ArraySubType = (UnmanagedType)80,
                    },
                };
                yield return new object[]
                {
                     "F14",
                    new MarshalAsAttribute(UnmanagedType.LPArray)
                    {
                        ArraySubType = UnmanagedType.LPWStr,
                    },
                };
                yield return new object[]
                {
                     "F15",
                    new MarshalAsAttribute(UnmanagedType.LPArray)
                    {
                        ArraySubType = UnmanagedType.LPStruct,
                        SizeParamIndex = 677,
                    },
                };
                yield return new object[]
                {
                     "F16",
                    new MarshalAsAttribute(UnmanagedType.LPArray)
                    {
                        ArraySubType = UnmanagedType.LPStruct,
                        SizeConst = 87,
                        SizeParamIndex = 677,
                    },
                };
                yield return new object[]
                {
                     "F17",
                    new MarshalAsAttribute(UnmanagedType.CustomMarshaler)
                    {
                        MarshalCookie = "",
                        MarshalType = "SampleMetadata.MyUdt",
                        MarshalTypeRef = typeof(MyUdt),
                    },
                };
                yield return new object[]
                {
                     "F18",
                    new MarshalAsAttribute(UnmanagedType.CustomMarshaler)
                    {
                        MarshalCookie = "",
                        MarshalType = "System.DateTime, System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                        MarshalTypeRef = typeof(DateTime),
                    },
                };
                yield return new object[]
                {
                     "F19",
                    new MarshalAsAttribute(UnmanagedType.CustomMarshaler)
                    {
                        MarshalCookie = "",
                        MarshalType = "Blah",
                    },
                };
                yield return new object[]
                {
                     "F20",
                    new MarshalAsAttribute(UnmanagedType.CustomMarshaler)
                    {
                        MarshalCookie = "YumYum",
                        MarshalType = "Blah",
                    },
                };
            }
        }

        private static void AssertEqual(MarshalAsAttribute m1, MarshalAsAttribute m2)
        {
            Assert.Equal(m1.ArraySubType, m2.ArraySubType);
            Assert.Equal(m1.IidParameterIndex, m2.IidParameterIndex);
            Assert.Equal(m1.MarshalCookie, m2.MarshalCookie);
            // The assembly identity of the serialized marshal type depends on which contracts the test assembly is built against.
            Assert.Equal(m1.MarshalType.RemoveAssemblyQualification(), m2.MarshalType.RemoveAssemblyQualification());
            Assert.Equal(m1.MarshalTypeRef, m2.MarshalTypeRef);
            Assert.Equal(m1.SafeArraySubType, m2.SafeArraySubType);
            Assert.Equal(m1.SafeArrayUserDefinedSubType, m2.SafeArrayUserDefinedSubType);
            Assert.Equal(m1.SizeConst, m2.SizeConst);
            Assert.Equal(m1.SizeParamIndex, m2.SizeParamIndex);
            Assert.Equal(m1.Value, m2.Value);
        }

        private static string RemoveAssemblyQualification(this string s)
        {
            if (s == null)
                return null;

            int index = s.IndexOf(',');
            if (index == -1)
                return s;
            return s.Substring(0, index);
        }
    }
}
