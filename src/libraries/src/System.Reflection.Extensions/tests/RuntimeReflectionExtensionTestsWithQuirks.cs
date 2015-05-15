// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace System.Reflection.Extensions.Tests
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

                foreach (PropertyInfo pi in type.AsType().GetRuntimeProperties())
                {
                    if (properties.Remove(pi.Name))
                        continue;

                    Assert.False(true, String.Format("Type: {0}, Property: {1} is not expected", type, pi));
                }

                foreach (String propertyName in properties)
                {
                    Assert.False(true, String.Format("Property: {0} cannot be found in {1}", propertyName, type));
                }
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

                foreach (EventInfo ei in type.AsType().GetRuntimeEvents())
                {
                    if (events.Remove(ei.Name))
                        continue;

                    Assert.False(true, String.Format("Type: {0}, Event: {1} is not expected", type, ei));
                }

                foreach (String eventName in events)
                {
                    Assert.False(true, String.Format("Event: {0} cannot be found", eventName));
                }
            }
        }

        private static TypeInfo[] GetTypes()
        {
            Assembly asm = typeof(PropertyDefinitions.BaseClass).GetTypeInfo().Assembly;
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
