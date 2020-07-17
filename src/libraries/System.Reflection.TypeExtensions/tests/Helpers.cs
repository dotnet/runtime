// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Tests
{
    public class Helpers
    {
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        public static EventInfo GetEvent(Type type, string name) => TypeExtensions.GetEvent(type, name, AllFlags);
        public static FieldInfo GetField(Type type, string name) => TypeExtensions.GetField(type, name, AllFlags);
        public static PropertyInfo GetProperty(Type type, string name) => TypeExtensions.GetProperty(type, name, AllFlags);
        public static MethodInfo GetMethod(Type type, string name) => TypeExtensions.GetMethod(type, name, AllFlags);
    }
}
