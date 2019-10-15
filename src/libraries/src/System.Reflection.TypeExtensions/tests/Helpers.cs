// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    public class Helpers
    {
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static EventInfo GetEvent(Type type, string name) => type.GetEvent(name, AllFlags);
        public static FieldInfo GetField(Type type, string name) => type.GetField(name, AllFlags);
        public static PropertyInfo GetProperty(Type type, string name) => type.GetProperty(name, AllFlags);
        public static MethodInfo GetMethod(Type type, string name) => type.GetMethod(name, AllFlags);
    }
}
