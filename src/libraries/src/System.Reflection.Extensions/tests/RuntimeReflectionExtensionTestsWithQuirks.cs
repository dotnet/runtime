// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

//
// About the "desktopQuirk"
//
//   On the desktop CLR, GetRuntimeProperties() and GetRuntimeEvents() behave inconsistently from the other
//   GetRuntime*() apis in that they suppress hidden instance properties and events from base classes.
//
//   On .NET Native, the GetRuntime*() apis all behave consistently (i.e. the results include hidden instance
//   members from base classes.)
//

namespace System.Reflection.Tests
{
    public class RuntimeReflectionExtensionsTestsWithQuirks
    {
        [Fact]
        public void GetRuntimeProperties()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeProperties(null);
            });

            List<String> properties = new List<String>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("PropertyDefinitions", StringComparison.Ordinal))
                    continue;

                properties.Clear();
                properties.AddRange((IEnumerable<String>)type.GetDeclaredField("DeclaredPropertyNames").GetValue(null));
                properties.AddRange((IEnumerable<String>)type.GetDeclaredField("InheritedPropertyNames").GetValue(null));

                Assert.All(type.AsType().GetRuntimeProperties(), p => Assert.True(properties.Remove(p.Name)));
                Assert.Empty(properties);
            }
        }

        [Fact]
        public void GetRuntimeEvents()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeEvents(null);
            });

            List<String> events = new List<String>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("EventDefinitions", StringComparison.Ordinal))
                    continue;

                if (type.IsInterface)
                    continue;

                events.Clear();
                events.AddRange((IEnumerable<String>)type.GetDeclaredField("DeclaredEvents").GetValue(null));
                events.AddRange((IEnumerable<String>)type.GetDeclaredField("InheritedEvents").GetValue(null));

                Assert.All(type.AsType().GetRuntimeEvents(), e => Assert.True(events.Remove(e.Name)));
                Assert.Empty(events);
            }
        }

        private static TypeInfo[] GetTypes()
        {
            Assembly asm = typeof(PropertyTestBaseClass).GetTypeInfo().Assembly;
            var list = new List<TypeInfo>();
            foreach (var t in asm.DefinedTypes)
            {
                if (t.Namespace == null) continue; // These are classes in the global namespace; not under test
                list.Add(t);
            }
            return list.ToArray();
        }
    }
}
