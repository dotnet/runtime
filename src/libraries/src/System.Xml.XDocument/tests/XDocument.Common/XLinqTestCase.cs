// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public class XLinqTestCase : TestCase, IComparable
    {
        private static string s_rootPath = null;
        private static string s_standardPath = null;

        public virtual void AddChild()
        {
        }

        public override int CompareTo(object obj)
        {
            TestCase temp = (TestCase)obj;
            return this.Attribute.Name.CompareTo(temp.Attribute.Name);
        }

        public static string RootPath
        {
            get
            {
                if (s_rootPath == null)
                {
                    s_rootPath = TestInput.Properties["CommandLine/DataPath"];
                }
                return s_rootPath;
            }
        }

        public static string StandardPath
        {
            get
            {
                if (s_standardPath == null)
                {
                    s_standardPath = TestInput.Properties["CommandLine/DataPath"];
                    s_standardPath = Path.Combine(s_standardPath, "StandardTests");
                }
                return s_standardPath;
            }
        }
    }
}
