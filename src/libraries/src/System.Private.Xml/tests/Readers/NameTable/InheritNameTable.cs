// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase NameTable user scenario inheritance
    //
    ////////////////////////////////////////////////////////////////
    [TestCase(Name = "XmlNameTable user scenario inheritance", Desc = "XmlNameTable inheritance")]
    public partial class TCUserNameTable : CTestCase
    {
        [Variation("Read xml file using custom name table")]
        public int v1()
        {
            string strFile = NameTable_TestFiles.GetTestFileName(EREADER_TYPE.GENERIC);

            // create custom nametable
            MyXmlNameTable nt = new MyXmlNameTable();
            string play = nt.Add("PLAY");
            string foo = nt.Add("http://www.foo.com");

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.NameTable = nt;
            xrs.DtdProcessing = DtdProcessing.Ignore;
            XmlReader r = XmlReader.Create(FilePathUtil.getStream(strFile), xrs);
            while (r.Read()) ;

            // verify name table
            object play2 = nt.Get("PLAY");
            object foo2 = nt.Get("http://www.foo.com");

            CError.Compare((object)play == play2, "play");
            CError.Compare((object)foo == foo2, "foo");

            CError.Compare(nt.Get("NONEMPTY0") != null, "NONEMPTY0");
            CError.WriteLine("Final count={0} atoms", nt.Count);

            return TEST_PASS;
        }
    }

    internal class MyXmlNameTable : XmlNameTable
    {
        private List<string> _Atoms = new List<string>();

        public int Count
        {
            get
            {
                return _Atoms.Count;
            }
        }

        private string LookupKey(string key)
        {
            for (int i = 0; i < _Atoms.Count; i++)
            {
                if (key == (string)_Atoms[i])
                {
                    return (string)_Atoms[i];
                }
            }

            return null;
        }

        public override string Add(string key)
        {
            string atom = LookupKey(key);
            if (atom == null)
            {
                atom = key;
                _Atoms.Add(atom);
            }

            return atom;
        }

        public override string Add(char[] array, int offset, int length)
        {
            string key = new string(array, offset, length);

            return Add(key);
        }

        public override string Get(string key)
        {
            return LookupKey(key);
        }

        public override string Get(char[] array, int offset, int length)
        {
            string key = new string(array, offset, length);

            return Get(key);
        }
    }
}
