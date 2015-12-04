using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics.Tracing;
using Xunit;
using System.Reflection;

//using Mdt = MdtEventSources;
using Sdt = SdtEventSources;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace BasicEventSourceTests
{
    
    public class TestsManifestGeneration
    {
        /// <summary>
        /// Visual Studio Online bug #125529, EventSource would fail when an EventSource was named "EventSource".
        /// </summary>
        [Fact]
        public void Test_EventSource_NamedEventSource()
        {
            lock (TestUtilities.EventSourceTestLock)
            {
                using (var es = new SdtEventSources.DontPollute.EventSource())
                {
                    using (var el = new LoudListener())
                    {
                        int i = 12;
                        es.EventWrite(i);

                        Assert.Equal(1, LoudListener.LastEvent.EventId);
                        Assert.Equal(1, LoudListener.LastEvent.Payload.Count);
                        Assert.Equal(i, LoudListener.LastEvent.Payload[0]);
                    }
                }
            }
        }

        /// <summary>
        /// Validate generating manifests for the inproc as well as reflection-loaded event sources
        /// </summary>
        [Fact]
        public void Test_GenerateManifest()
        {
#if true
            Console.WriteLine("Disabled because it fails in checkin but not locally on 10/6/2015");
#else
            lock (TestUtilities.EventSourceTestLock)
            {
                TestUtilities.CheckNoEventSourcesRunning("Start");
                string dll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "EvtSrcForReflection.dll");
                string esdll = Path.Combine(Path.GetDirectoryName(dll), "Microsoft.Diagnostics.Tracing.EventSource.dll");
                Assembly.ReflectionOnlyLoadFrom(esdll);             // preload dependency of EvtSrcForReflection
                var esAssm = Assembly.ReflectionOnlyLoadFrom(dll);
                Type firstEvtSrcType = esAssm.GetTypes().FirstOrDefault(ty => ty.BaseType.Name == "EventSource");
                Assert.NotNull(firstEvtSrcType);
                Type reflectionEvtSrc = firstEvtSrcType;

                var esTypes = new Type[] { typeof(Mdt.EventSourceTest), 
                                           typeof(Mdt.SimpleEventSource), 
                                           typeof(Mdt.MyLoggingEventSource),
                                           typeof(Sdt.EventSourceTest), 
                                           typeof(Sdt.SimpleEventSource), 
                                           // typeof(Sdt.MyLoggingEventSource), // for SDT interface methods are skipped, so we get an empty manifest
                                           reflectionEvtSrc };

                if (!Directory.Exists(Environment.ExpandEnvironmentVariables(@"%TEMP%\man")))
                    Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(@"%TEMP%\man"));

                // Be helpful and tell people how to update the baselines.  
                Console.WriteLine("To Update baselines");
                Console.WriteLine(@"  cd to EventSource\Public\test\BasicEventSourceTest\Baselines");
                foreach (var eventSource in esTypes)
                {
                    string manfilename = SaveEventSourceManifest(eventSource, Environment.ExpandEnvironmentVariables(@"%TEMP%\man"));
                    string baselineManFile = GetPrefixFromType(eventSource) + eventSource.Name + ".man";
                    Console.WriteLine("  tf edit " + baselineManFile);
                    Console.WriteLine("  copy \"" + manfilename + "\" " + baselineManFile);
                }

                foreach (var eventSource in esTypes)
                {
                    string manfilename = SaveEventSourceManifest(eventSource, Environment.ExpandEnvironmentVariables(@"%TEMP%\man"));
                    string baselineManFile = @"Baselines\" + GetPrefixFromType(eventSource) + eventSource.Name + ".man";
                    ValidateManifest(baselineManFile, manfilename);
                }
                TestUtilities.CheckNoEventSourcesRunning("Stop");
            }
#endif
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
                    //else
                    //{
                    //    mi = eventSourceType.GetTypeInfo().GetMethod("GenerateManifest", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public,
                    //                                        null, new Type[] { typeof(Type), typeof(string) }, null);
                    //    man = (string)mi.Invoke(null, new object[] { eventSourceType, dllName });
                    //}
                //}
                //else
                //{
                //    man = EventSource.GenerateManifest(eventSourceType, dllName, EventManifestOptions.AllCultures);
                //}
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
            Console.WriteLine("Baseline: " + Path.GetFullPath(baseline));
            Console.WriteLine("Test: " + Path.GetFullPath(manifest));
            var baselineOrig = Path.GetFullPath(baseline);
            baselineOrig = baselineOrig.Replace(@"\bin\Debug\", @"\");
            baselineOrig = baselineOrig.Replace(@"\bin\Release\", @"\");
            Console.WriteLine("To Compare: windiff " + Path.GetFullPath(manifest) + " " + baselineOrig);
            Console.WriteLine("To Update: copy " + Path.GetFullPath(manifest) + " " + baselineOrig);

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
