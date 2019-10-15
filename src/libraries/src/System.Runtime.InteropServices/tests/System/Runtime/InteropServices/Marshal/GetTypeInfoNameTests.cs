// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.ComTypes;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public class GetTypeInfoNameTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypeInfoName_ValidTypeInfo_ReturnsExpected()
        {
            Assert.Equal("strName", Marshal.GetTypeInfoName(new TypeInfo()));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypeInfoName_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypeInfoName((ITypeInfo)null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypeInfoName_NullTypeInfo_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeInfo", () => Marshal.GetTypeInfoName((ITypeInfo)null));
        }

        public class TypeInfo : ITypeInfo
        {
            public void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile)
            {
                strName = "strName";
                strDocString = "strDocString";
                dwHelpContext = 10;
                strHelpFile = "strHelpFile";
            }
            public void GetTypeAttr(out IntPtr ppTypeAttr)
            {
                throw new NotImplementedException();
            }

            public void GetTypeComp(out ITypeComp ppTComp)
            {
                throw new NotImplementedException();
            }

            public void GetFuncDesc(int index, out IntPtr ppFuncDesc)
            {
                throw new NotImplementedException();
            }

            public void GetVarDesc(int index, out IntPtr ppVarDesc)
            {
                throw new NotImplementedException();
            }

            public void GetNames(int memid, string[] rgBstrNames, int cMaxNames, out int pcNames)
            {
                throw new NotImplementedException();
            }

            public void GetRefTypeOfImplType(int index, out int href)
            {
                throw new NotImplementedException();
            }

            public void GetImplTypeFlags(int index, out ComTypes.IMPLTYPEFLAGS pImplTypeFlags)
            {
                throw new NotImplementedException();
            }

            public void GetIDsOfNames(string[] rgszNames, int cNames, int[] pMemId)
            {
                throw new NotImplementedException();
            }

            public void Invoke(object pvInstance, int memid, short wFlags, ref ComTypes.DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out int puArgErr)
            {
                throw new NotImplementedException();
            }

            public void GetDllEntry(int memid, ComTypes.INVOKEKIND invKind, IntPtr pBstrDllName, IntPtr pBstrName, IntPtr pwOrdinal)
            {
                throw new NotImplementedException();
            }

            public void GetRefTypeInfo(int hRef, out ITypeInfo ppTI)
            {
                throw new NotImplementedException();
            }

            public void AddressOfMember(int memid, ComTypes.INVOKEKIND invKind, out IntPtr ppv)
            {
                throw new NotImplementedException();
            }

            public void CreateInstance(object pUnkOuter, ref Guid riid, out object ppvObj)
            {
                throw new NotImplementedException();
            }

            public void GetMops(int memid, out string pBstrMops)
            {
                throw new NotImplementedException();
            }

            public void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex)
            {
                throw new NotImplementedException();
            }

            public void ReleaseTypeAttr(IntPtr pTypeAttr)
            {
                throw new NotImplementedException();
            }

            public void ReleaseFuncDesc(IntPtr pFuncDesc)
            {
                throw new NotImplementedException();
            }

            public void ReleaseVarDesc(IntPtr pVarDesc)
            {
                throw new NotImplementedException();
            }
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
