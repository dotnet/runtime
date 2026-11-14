// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using StrippedNamesLibrary;

// The StrippedNamesLibrary assembly is listed in IlcStripNamesAssembly (see StrippedNames.csproj), so the reflection
// metadata for its types and members has no names. Types and members from this assembly keep their names.
internal static class Program
{
    private static int Main()
    {
        TestNamesOfStrippedTypes.Run();
        TestNamesOfStrippedMembers.Run();
        TestNamesOfRetainedTypes.Run();
        TestRetainedMemberNames.Run();
        TestTypeIdentity.Run();
        TestGenericVirtualMethods.Run();
        TestLookupByName.Run();
        TestAttributes.Run();
        TestStackTrace.Run();
        TestNamesAreNotInTheImage.Run();

        return 100;
    }

    private static Type Keep([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type) => type;

    private static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string message = null)
    {
        if (!condition)
            throw new Exception(message);
    }

    // Prevents the names from being constant folded into the program, which would defeat the check for the image contents.
    private static string Join(params string[] parts) => string.Concat(parts);

    private static class TestNamesOfStrippedTypes
    {
        public static void Run()
        {
            Type widget = Keep(typeof(Widget));

            Assert(widget.Name == string.Empty);
            Assert(widget.FullName is null);
            Assert(widget.Namespace is null);
            Assert(widget.AssemblyQualifiedName is null);
            Assert(widget.ToString() == string.Empty);
            Assert(widget.Assembly == typeof(Base).Assembly);

            Type nested = Keep(typeof(Widget.Nested));
            Assert(nested.Name == string.Empty);
            Assert(nested.FullName is null);
            Assert(nested.DeclaringType == widget);

            Assert(typeof(Widget[]).FullName is null);
            Assert(typeof(Widget).MakeByRefType().FullName is null);
            Assert(typeof(Widget).MakePointerType().FullName is null);

            // Constructed generic types have no full name if any of their components has no name
            Assert(typeof(GenericHolder<>).FullName is null);
            Assert(typeof(GenericHolder<Widget>).FullName is null);
            Assert(typeof(GenericHolder<string>).FullName is null);
            Assert(typeof(List<Widget>).FullName is null);
            Assert(typeof(List<Widget>).AssemblyQualifiedName is null);
            Assert(typeof(List<Widget>).ToString() is not null);
            Assert(typeof(List<Widget>).GetGenericArguments()[0] == typeof(Widget));

            // ... but they keep it if they are not involved
            Assert(typeof(List<string>).FullName is not null);
            Assert(typeof(List<string>).FullName.StartsWith("System.Collections.Generic.List`1[[System.String", StringComparison.Ordinal));

            Assert(typeof(Color).Name == string.Empty);
            Assert(typeof(Callback).Name == string.Empty);
        }
    }

    private static class TestNamesOfStrippedMembers
    {
        public static void Run()
        {
            Type widget = Keep(typeof(Widget));
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // Methods that have the same shape must stay distinct without their names
            MethodInfo[] methods = widget.GetMethods(flags);
            Assert(methods.Length == 7);
            Assert(methods.All(m => m.Name == string.Empty));
            Assert(methods.All(m => m.DeclaringType == widget));

            Widget instance = new Widget();
            List<string> results = new();
            foreach (MethodInfo method in methods)
            {
                if (method.GetParameters().Length == 0 && method.ReturnType == typeof(string))
                    results.Add((string)method.Invoke(instance, null));
            }
            results.Sort(StringComparer.Ordinal);
            Assert(results.Count == 2);
            Assert(results[0] == "first");
            Assert(results[1] == "second");

            FieldInfo[] fields = widget.GetFields(flags);
            Assert(fields.Length == 1);
            Assert(fields[0].Name == string.Empty);
            fields[0].SetValue(instance, 42);
            Assert(instance.Field == 42);

            PropertyInfo[] properties = widget.GetProperties(flags);
            Assert(properties.Length == 1);
            Assert(properties[0].Name == string.Empty);
            properties[0].SetValue(instance, 7);
            Assert((int)properties[0].GetValue(instance) == 7);

            EventInfo[] events = widget.GetEvents(flags);
            Assert(events.Length == 1);
            Assert(events[0].Name == string.Empty);

            Assert(widget.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Length == 1);
        }
    }

    private static class TestNamesOfRetainedTypes
    {
        public static void Run()
        {
            Type control = Keep(typeof(ControlNamespace.ControlType));

            Assert(control.Name == "ControlType");
            Assert(control.Namespace == "ControlNamespace");
            Assert(control.FullName == "ControlNamespace.ControlType");
            Assert(control.ToString() == "ControlNamespace.ControlType");
            Assert(control.AssemblyQualifiedName.StartsWith("ControlNamespace.ControlType, ", StringComparison.Ordinal));

            Assert(typeof(ControlNamespace.ControlType.NestedType).FullName == "ControlNamespace.ControlType+NestedType");
            Assert(typeof(ControlNamespace.ControlGeneric<int>).FullName.StartsWith("ControlNamespace.ControlGeneric`1[[System.Int32", StringComparison.Ordinal));
            Assert(typeof(ControlNamespace.ControlType[]).FullName == "ControlNamespace.ControlType[]");

            Assert(control.GetMethod(Join("Control", "Method")) is not null);
            Assert(control.GetField(Join("Control", "Field")) is not null);
            Assert(control.GetProperty(Join("Control", "Property")) is not null);
            Assert(Type.GetType(Join("ControlNamespace.", "ControlType")) == control);
        }
    }

    private static class TestRetainedMemberNames
    {
        public static void Run()
        {
            // Constructors are found by name
            Type widget = Keep(typeof(Widget));
            ConstructorInfo ctor = widget.GetConstructor(Type.EmptyTypes);
            Assert(ctor is not null);
            Assert(ctor.Name == ".ctor");
            Assert(ctor.Invoke(null) is Widget);
            Assert(Activator.CreateInstance(widget) is Widget);

            // Enum members are retained
            Assert(Color.Green.ToString() == "Green");
            Assert(Enum.Parse<Color>("Red") == Color.Red);
            Assert(string.Join(",", Enum.GetNames<Color>()) == "Red,Green");
            Assert(Color.Red.ToString("G") == "Red");

            // The Invoke method of delegates is retained
            Callback callback = x => x * 2;
            Assert(callback(2) == 4);
            Assert((int)callback.DynamicInvoke(3) == 6);
            Assert(Keep(typeof(Callback)).GetMethod("Invoke") is not null);
        }
    }

    private static class TestTypeIdentity
    {
        public static void Run()
        {
            // Types need to stay identifiable regardless of whether they are reached from a type handle or from the metadata
            Type derived = Keep(typeof(Derived));
            Type baseType = derived.BaseType;
            Assert(baseType == typeof(Base));
            Assert(baseType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m.DeclaringType == typeof(Base)));

            Type[] types = typeof(Base).Assembly.GetTypes();
            Assert(Array.IndexOf(types, typeof(Base)) >= 0);
            Assert(Array.IndexOf(types, typeof(Derived)) >= 0);
            Assert(Array.IndexOf(types, typeof(Widget.Nested)) >= 0);

            Type marked = types.Single(t => t.IsDefined(typeof(MarkerAttribute)));
            Assert(marked == Keep(typeof(Marked)));
            Assert(Activator.CreateInstance(marked).GetType() == marked);

            Base instance = new Derived();
            Assert(instance.GetType() == derived);
            Assert(instance.GetType().BaseType == baseType);
            Assert(instance.Hello() == "Derived");
        }
    }

    private static class TestGenericVirtualMethods
    {
        public static void Run()
        {
            // Generic virtual methods are matched by name and signature, so they keep their names
            GvmBase gvm = new GvmDerived();
            Assert(gvm.Describe<object>() == "GvmDerived");
            Assert(gvm.Other<object>() == "GvmDerived.Other");
            Assert(gvm.Describe<Widget>() == "GvmDerived");
            Assert(gvm.Other<Widget>() == "GvmDerived.Other");

            Type gvmType = Keep(typeof(GvmBase));
            MethodInfo describe = gvmType.GetMethod("Describe").MakeGenericMethod(typeof(Marked));
            Assert((string)describe.Invoke(gvm, null) == "GvmDerived");
            MethodInfo other = gvmType.GetMethod("Other").MakeGenericMethod(typeof(Marked));
            Assert((string)other.Invoke(gvm, null) == "GvmDerived.Other");

            IConverter<int> converter = new IntConverter();
            Assert(converter.Convert<int>(1) == 2);
            Assert(converter.Convert<object>(1).Equals(2));
        }
    }

    private static class TestLookupByName
    {
        public static void Run()
        {
            string ns = "StrippedNamesLibrary.";
            Assert(Type.GetType(Join(ns, "Widget", ", StrippedNamesLibrary")) is null);
            Assert(Type.GetType(Join(ns, "Widget", ", StrippedNamesLibrary"), throwOnError: false, ignoreCase: true) is null);
            Assert(Type.GetType(Join(ns, "Derived")) is null);
            Assert(typeof(Base).Assembly.GetType(Join(ns, "Base")) is null);
            Assert(typeof(Base).Assembly.GetType(Join(ns, "Base"), throwOnError: false, ignoreCase: true) is null);

            Type widget = Keep(typeof(Widget));
            Assert(widget.GetMethod(Join("First", "Method")) is null);
            Assert(widget.GetMethod(Join("first", "method"), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null);
            Assert(widget.GetField(Join("Fie", "ld")) is null);
            Assert(widget.GetProperty(Join("Co", "unt")) is null);
            Assert(widget.GetEvent(Join("Chan", "ged")) is null);
            Assert(widget.GetNestedType(Join("Nes", "ted")) is null);
        }
    }

    private static class TestAttributes
    {
        public static void Run()
        {
            // Named arguments are resolved by name, so the members of attribute types are retained
            MarkerAttribute attribute = typeof(Marked).GetCustomAttribute<MarkerAttribute>();
            Assert(attribute is not null);
            Assert(attribute.Label == "label");
            Assert(attribute.Number == 42);

            CustomAttributeData data = typeof(Marked).CustomAttributes.Single();
            Assert(data.AttributeType == typeof(MarkerAttribute));
            Assert(data.NamedArguments.Select(a => a.MemberName).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(new[] { "Label", "Number" }));

            Assert(new Marked() is not null);
        }
    }

    private static class TestStackTrace
    {
        public static void Run()
        {
            string stackTrace = null;
            try
            {
                UniqueStrippedTypeNameMarker.UniqueStrippedThrowingMethodMarker();
            }
            catch (InvalidOperationException ex)
            {
                stackTrace = ex.StackTrace;
                _ = ex.ToString();
            }

            // The names are only available in the symbol files
            Assert(!string.IsNullOrEmpty(stackTrace));
            Assert(!stackTrace.Contains(Join("Unique", "Stripped")));
        }
    }

    private static class TestNamesAreNotInTheImage
    {
        public static void Run()
        {
            // The debug information of other platforms is part of the image
            if (!OperatingSystem.IsWindows())
                return;

            byte[] image = File.ReadAllBytes(Environment.ProcessPath);

            // Check that the same lookup finds the names that are expected to be there
            Assert(Contains(image, Join("ControlType")));
            Assert(Contains(image, Join("Control", "Method")));

            Assert(!Contains(image, Join("Unique", "StrippedTypeNameMarker")));
            Assert(!Contains(image, Join("Unique", "StrippedThrowingMethodMarker")));
            Assert(!Contains(image, Join("First", "Method")));
            Assert(!Contains(image, Join("Second", "Method")));
            Assert(!Contains(image, Join("Rai", "seChanged")));
            Assert(!Contains(image, Join("Gvm", "Derived")));
            Assert(!Contains(image, Join("Int", "Converter")));
        }

        private static bool Contains(byte[] image, string text) => image.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0;
    }
}

namespace ControlNamespace
{
    public class ControlType
    {
        public int ControlField;
        public int ControlProperty { get; set; }
        public void ControlMethod() { }

        public class NestedType { }
    }

    public class ControlGeneric<T> { }
}
