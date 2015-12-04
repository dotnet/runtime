using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics.Tracing;

namespace BasicEventSourceTests
{
    
    public class TestsEventSourceReferences
    {
#if false // GetReferenceAssemblies appears to not exist in CoreCLR
        /// <summary>
        /// Because EventSource code is part of the framework code, we 
        /// have to make sure we are not referencing code outside its boundaries 
        /// like System.Linq.
        /// </summary>
        [Fact]
        public void Test_EventSource_Non_Mscorlib_References()
        {
            lock (TestUtilities.EventSourceTestLock)
            {
                Console.WriteLine("Starting to detect EventSource dependencies...");

                HashSet<string> eventSourceReferencedAssemblies = new HashSet<string>();

                eventSourceReferencedAssemblies.Add("mscorlib");
                eventSourceReferencedAssemblies.Add("System.Core");

                var eventSourceAssembly = typeof(EventSource).GetTypeInfo().Assembly;

                var referenceAssemblies = eventSourceAssembly.GetReferencedAssemblies().Select(t => t.Name).Distinct();

                foreach(var assembly in referenceAssemblies)
                {
                    Assert.True(eventSourceReferencedAssemblies.Contains(assembly));
                    Console.WriteLine(assembly);
                }
            
                Console.WriteLine("Success...");
            }
        }
#endif // false
    }
}
