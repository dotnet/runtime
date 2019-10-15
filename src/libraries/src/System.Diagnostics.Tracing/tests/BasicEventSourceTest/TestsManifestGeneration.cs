// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;
using System.Reflection;

using SdtEventSources;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace BasicEventSourceTests
{
    public class TestsManifestGeneration
    {
        /// <summary>
        /// EventSource would fail when an EventSource was named "EventSource".
        /// </summary>
        [Fact]
        [ActiveIssue("dotnet/corefx #18808", TargetFrameworkMonikers.NetFramework)]
        public void Test_EventSource_NamedEventSource()
        {
            using (var es = new SdtEventSources.DontPollute.EventSource())
            {
                using (var el = new LoudListener(es))
                {
                    int i = 12;
                    es.EventWrite(i);

                    Assert.Equal(1, LoudListener.t_lastEvent.EventId);
                    Assert.Equal(1, LoudListener.t_lastEvent.Payload.Count);
                    Assert.Equal(i, LoudListener.t_lastEvent.Payload[0]);
                }
            }
        }

        /// <summary>
        /// Calls GenerateManifest() on the event source passed in, saves the output to a file under 'folder'
        /// and returns the filename under which the manifest was saved.
        /// </summary>
        /// <param name="eventSourceType">The EventSource derived class whose manifest we want saved</param>
        /// <param name="folder">The folder where the manifest will be saved</param>
        /// <returns>The filename under 'folder' that contains the ETW manifest</returns>
        private string SaveEventSourceManifest(Type eventSourceType, string folder)
        {
            var manfilename = Path.Combine(folder, GetPrefixFromType(eventSourceType) + eventSourceType.Name + ".man");
            using (var manfile = new System.IO.StreamWriter(new System.IO.MemoryStream(Encoding.Unicode.GetBytes(manfilename))))
            {
                string man = null;
                string dllName = Path.GetFileName(eventSourceType.GetTypeInfo().Assembly.Location);
                //if (!eventSourceType.GetTypeInfo().Assembly.ReflectionOnly)
                //{
                var baseAssm = eventSourceType.GetTypeInfo().BaseType.GetTypeInfo().Assembly;
                var tyGmf = (baseAssm != null) ? baseAssm.GetType(eventSourceType.GetTypeInfo().BaseType.Namespace + ".EventManifestOptions") : null;
                MethodInfo mi = null;
                if (tyGmf != null)
                {
                    //mi = eventSourceType.GetTypeInfo().GetMethod("GenerateManifest", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public,
                    //                               null, new Type[] { typeof(Type), typeof(string), tyGmf }, null);
                    mi = eventSourceType.GetTypeInfo().GetDeclaredMethod("GenerateManifest");
                }
                if (mi != null)
                {
                    man = (string)mi.Invoke(null, new object[] { eventSourceType, dllName, 2 });
                }
                manfile.Write(man);
            }
            return manfilename;
        }

        private static string GetPrefixFromType(Type eventSourceType)
        {
            string prefix = string.Empty;
            if (eventSourceType.Namespace == "MdtEventSources")
                prefix = "Mdt";
            else if (eventSourceType.Namespace == "SdtEventSources")
                prefix = "Sdt";
            return prefix;
        }

        private static void ValidateManifest(string baseline, string manifest)
        {
            Debug.WriteLine("Baseline: " + Path.GetFullPath(baseline));
            Debug.WriteLine("Test: " + Path.GetFullPath(manifest));
            var baselineOrig = Path.GetFullPath(baseline);
            baselineOrig = baselineOrig.Replace(@"\bin\Debug\", @"\");
            baselineOrig = baselineOrig.Replace(@"\bin\Release\", @"\");
            Debug.WriteLine("To Compare: windiff " + Path.GetFullPath(manifest) + " " + baselineOrig);
            Debug.WriteLine("To Update: copy " + Path.GetFullPath(manifest) + " " + baselineOrig);

            var baselineLines = File.ReadLines(baseline).ToArray();
            var manifestLines = File.ReadLines(manifest).ToArray();
            bool foundProvider = false;
            for (int i = 0; i < baselineLines.Length; ++i)
            {
                if (foundProvider)
                    Assert.Equal(baselineLines[i], manifestLines[i]);
                else
                {
                    if (baselineLines[i].StartsWith("<provider "))
                        foundProvider = true;
                    else
                        Assert.Equal(baselineLines[i], manifestLines[i]);
                }
            }
        }
    }
}
