// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public static class CommonUtilities
    {
        public static void DeleteFile(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        public static bool TestListeners(TraceSource source, Type[] types, string[] names)
        {
            Type[] filters = new Type[types.Length];
            return TestListeners(source, types, names, filters);
        }

        public static bool TestListeners(TraceSource source, Type[] types, string[] names, Type[] filters)
        {
            //This tests all the listeners for a TestSource
            TraceListenerCollection collection = source.Listeners;
            for (int i = 0; i < types.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < collection.Count; j++)
                {
                    if (collection[j].GetType().Equals(types[i]))
                    {
                        found = true;
                        Assert.True(collection[j].Name == names[i]);
                        if (filters[i] == null)
                            Assert.True(collection[j].Filter == null);
                        else
                            Assert.True(collection[j].Filter.GetType().Equals(filters[i]));
                    }
                }

                Assert.True(found);
            }

            return true;
        }

        public static bool TestListenerContent(Type[] types, string[] names, string presentMsg)
        {
            return TestListenerContent(types, names, presentMsg, null);
        }

        public static bool TestListenerContent(Type[] types, string[] names, string presentMsg, string absentMsg)
        {
            //This can test the listener content specified
            for (int i = 0; i < types.Length; i++)
            {
                bool found = false;
                string traceValue;
                switch (types[i].Name)
                {
                    case "TextWriterTraceListener":
                    case "DelimitedListTraceListener":
                        found = true;
                        traceValue = File.ReadAllText(names[i]);
                        if (absentMsg != null)
                            Assert.True(traceValue.IndexOf(absentMsg) == -1);
                        if (presentMsg != null)
                            Assert.True(traceValue.IndexOf(presentMsg) > -1);
                        break;

                    case "DefaultTraceListener":
                        found = true;
                        break;
                    default:
                        break;
                }

                Assert.True(found);
            }

            return true;
        }
    }
}