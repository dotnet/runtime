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

using ControlNamespace;
using StrippedNamesLibrary;

// The StrippedNamesLibrary assembly is listed in IlcStripNamesAssembly (see StrippedNames.csproj), so the reflection
// metadata for its types has no names. Types from this assembly keep their names, and members keep their names everywhere.
internal static class Program
{
    private static int Main()
    {
        TestNamesOfStrippedTypes.Run();
        TestMembersOfStrippedTypes.Run();
        TestNamesOfRetainedTypes.Run();
        TestMemberNames.Run();
        TestOverrides.Run();
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

    private static void TakesWidget(Widget value) { }

    private static void AssertNameIsNotAvailable(Func<object> getName, [CallerArgumentExpression(nameof(getName))] string message = null)
    {
        try
        {
            getName();
        }
        catch (NotSupportedException)
        {
            return;
        }

        throw new Exception(message);
    }

    private static class TestNamesOfStrippedTypes
    {
        public static void Run()
        {
            Type widget = Keep(typeof(Widget));

            // Getting any name of the type fails
            AssertNameIsNotAvailable(() => widget.Name);
            AssertNameIsNotAvailable(() => widget.Namespace);
            AssertNameIsNotAvailable(() => widget.FullName);
            AssertNameIsNotAvailable(() => widget.AssemblyQualifiedName);
            AssertNameIsNotAvailable(() => widget.ToString());
            AssertNameIsNotAvailable(() => new Widget().ToString());
            AssertNameIsNotAvailable(() => default(Point).ToString());
            AssertNameIsNotAvailable(() => Keep(typeof(Widget.Nested)).Name);
            AssertNameIsNotAvailable(() => Keep(typeof(Widget.Nested)).FullName);
            AssertNameIsNotAvailable(() => typeof(Color).Name);
            AssertNameIsNotAvailable(() => typeof(Callback).Name);
            AssertNameIsNotAvailable(() => typeof(GenericHolder<>).Name);
            AssertNameIsNotAvailable(() => typeof(GenericHolder<>).ToString());

            // ... also if the type is a part of another type
            AssertNameIsNotAvailable(() => typeof(Widget[]).Name);
            AssertNameIsNotAvailable(() => typeof(Widget[]).FullName);
            AssertNameIsNotAvailable(() => typeof(Widget).MakeByRefType().ToString());
            AssertNameIsNotAvailable(() => typeof(Widget).MakePointerType().AssemblyQualifiedName);
            AssertNameIsNotAvailable(() => typeof(GenericHolder<int>).Name);
            AssertNameIsNotAvailable(() => typeof(GenericHolder<int>).FullName);
            AssertNameIsNotAvailable(() => typeof(GenericHolder<Widget>).ToString());
            AssertNameIsNotAvailable(() => typeof(List<Widget>).FullName);
            AssertNameIsNotAvailable(() => typeof(List<Widget>).AssemblyQualifiedName);
            AssertNameIsNotAvailable(() => typeof(List<Widget>).ToString());

            // The error message mentions how to restore the names
            try
            {
                _ = widget.Name;
            }
            catch (NotSupportedException ex)
            {
                Assert(ex.Message.Contains("IlcStripNamesAssembly", StringComparison.Ordinal));
            }

            // Everything that doesn't need the name works
            Assert(widget.Assembly == typeof(Base).Assembly);
            Assert(Keep(typeof(Widget.Nested)).DeclaringType == widget);
            Assert(typeof(List<Widget>).GetGenericArguments()[0] == typeof(Widget));
            Assert(typeof(Widget[]).GetElementType() == widget);
            Assert(typeof(GenericHolder<int>).GetGenericTypeDefinition() == typeof(GenericHolder<>));
            Assert(typeof(GenericHolder<>).GetGenericArguments()[0].Name == "T");
            Assert(widget.IsClass && !widget.IsValueType && typeof(Point).IsValueType && typeof(Color).IsEnum);

            // ... and the types that are not involved are not affected
            Assert(typeof(List<string>).FullName.StartsWith("System.Collections.Generic.List`1[[System.String", StringComparison.Ordinal));
            Assert(typeof(string[]).Name == "String[]");

            // Exceptions of the assembly have no name to print unless they have a message, and the stack trace doesn't need the name
            AssertNameIsNotAvailable(() => new StrippedException().Message);
            AssertNameIsNotAvailable(() => new StrippedException("message").ToString());
            Assert(new StrippedException("message").Message == "message");
            try
            {
                throw new StrippedException("message");
            }
            catch (StrippedException ex)
            {
                Assert(!string.IsNullOrEmpty(ex.StackTrace));
            }

            // The names of enum members work as usual
            Assert((Options.First | Options.Second).ToString() == "First, Second");

            // Types of the assembly are returned from the reflection API of other types
            MethodInfo takesWidget = Keep(typeof(Program)).GetMethod(nameof(TakesWidget), BindingFlags.NonPublic | BindingFlags.Static);
            Assert(takesWidget.GetParameters()[0].ParameterType == widget);
            Assert(takesWidget.GetParameters()[0].Name == "value");
            AssertNameIsNotAvailable(() => takesWidget.GetParameters()[0].ParameterType.Name);
        }
    }

    private static class TestMembersOfStrippedTypes
    {
        public static void Run()
        {
            Type widget = Keep(typeof(Widget));
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // Members keep their names, even though the type that declares them has none
            MethodInfo[] methods = widget.GetMethods(flags);
            Assert(methods.Length == 8);
            Assert(methods.All(m => m.DeclaringType == widget));
            Assert(methods.Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(
                new[] { "Echo", "FirstMethod", "RaiseChanged", "SecondMethod", "add_Changed", "get_Count", "remove_Changed", "set_Count" }));

            Widget instance = new Widget();
            Assert((string)widget.GetMethod("FirstMethod").Invoke(instance, null) == "first");
            Assert((string)widget.GetMethod("SecondMethod").Invoke(instance, null) == "second");

            MethodInfo echo = widget.GetMethod("Echo");
            Assert(echo.GetParameters().Single().Name == "text");
            Assert((string)echo.Invoke(instance, new object[] { "hello" }) == "hello");

            FieldInfo field = widget.GetField("Field");
            Assert(field is not null);
            field.SetValue(instance, 42);
            Assert(instance.Field == 42);

            PropertyInfo property = widget.GetProperty("Count");
            Assert(property is not null);
            property.SetValue(instance, 7);
            Assert((int)property.GetValue(instance) == 7);

            Assert(widget.GetEvent("Changed") is not null);

            // Members are matched by their metadata definition, even when the type has no name
            FieldInfo definitionField = Keep(typeof(GenericHolder<>)).GetField("Value");
            Type constructed = Keep(typeof(GenericHolder<Marked>));
            MemberInfo projected = constructed.GetMemberWithSameMetadataDefinitionAs(definitionField);
            Assert(projected is FieldInfo { Name: "Value" } projectedField && projectedField.DeclaringType == constructed);
            Assert(widget.GetMemberWithSameMetadataDefinitionAs(field) == field);

            // Nested types have no name, so they can't be filtered by it, but they are still found by their definition
            Type nested = Keep(typeof(Widget.Nested));
            Assert(widget.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Single() == nested);
            Assert(widget.GetMemberWithSameMetadataDefinitionAs(nested) == nested);
            Assert(widget.GetNestedType(string.Empty) is null);
            Assert(widget.GetNestedType(string.Empty, BindingFlags.Public | BindingFlags.IgnoreCase) is null);
            Assert(widget.GetMember(string.Empty, MemberTypes.NestedType, BindingFlags.Public | BindingFlags.NonPublic).Length == 0);
            Assert(widget.GetMember(string.Empty).Length == 0);

            // ... including when the nested type is reached through a constructed generic type
            Type innerDefinition = Keep(typeof(GenericHolder<>)).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Single();
            Assert(constructed.GetMemberWithSameMetadataDefinitionAs(innerDefinition) == innerDefinition);
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

    private static class TestMemberNames
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

            // Enum members work as usual
            Assert(Color.Green.ToString() == "Green");
            Assert(Enum.Parse<Color>("Red") == Color.Red);
            Assert(string.Join(",", Enum.GetNames<Color>()) == "Red,Green");
            Assert(Color.Red.ToString("G") == "Red");

            // The Invoke method of delegates works as usual
            Callback callback = x => x * 2;
            Assert(callback(2) == 4);
            Assert((int)callback.DynamicInvoke(3) == 6);
            Assert(Keep(typeof(Callback)).GetMethod("Invoke") is not null);
        }
    }

    private static class TestOverrides
    {
        public static void Run()
        {
            // Overrides are matched with the members they override by name, so this needs to work across stripped and not stripped types
            MethodInfo baseMethod = Keep(typeof(Base)).GetMethod("Hello");
            Assert(baseMethod.DeclaringType == typeof(Base));

            MethodInfo derivedMethod = Keep(typeof(Derived)).GetMethod("Hello", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert(derivedMethod.DeclaringType == typeof(Derived));
            Assert(derivedMethod.GetBaseDefinition() == baseMethod);
            Assert(Keep(typeof(Derived)).GetMethods(BindingFlags.Public | BindingFlags.Instance).Count(m => m.Name == "Hello") == 1);

            MethodInfo mainMethod = Keep(typeof(MainDerived)).GetMethod("Hello", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert(mainMethod.GetBaseDefinition() == baseMethod);
            Assert(Keep(typeof(MainDerived)).GetMethods(BindingFlags.Public | BindingFlags.Instance).Count(m => m.Name == "Hello") == 1);
            Assert(new MainDerived().Hello() == "MainDerived");
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

            // Generic definitions reached through the metadata unify with the ones from the type handles
            Type holder = types.Single(t => t.IsGenericTypeDefinition && t == typeof(GenericHolder<>));
            Assert(holder == typeof(GenericHolder<>));
            Assert(holder.MakeGenericType(typeof(int)) == typeof(GenericHolder<int>));

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
            // Generic virtual methods are matched by name and signature
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

            // Generic methods are looked up by name at runtime too
            Assert(GenericHelpers.Describe<Marked>(null) == "helper:ref");
            Assert(GenericHelpers.Describe<Point>(default) == "helper:value");
            MethodInfo helper = Keep(typeof(GenericHelpers)).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single().MakeGenericMethod(typeof(Marked));
            Assert((string)helper.Invoke(null, new object[] { null }) == "helper:ref");
            helper = Keep(typeof(GenericHelpers)).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single().MakeGenericMethod(typeof(Point));
            Assert((string)helper.Invoke(null, new object[] { default(Point) }) == "helper:value");

            // GenericCaller<GenericHolder<Marked>> is created by the type loader at runtime, sharing the code of GenericCaller<__Canon>
            Assert(((ICaller)new GenericCaller<Marked>()).Call() == "helper:ref");
            Type caller = Keep(typeof(GenericCaller<>)).MakeGenericType(typeof(GenericHolder<>).MakeGenericType(typeof(Marked)));
            Assert(((ICaller)Activator.CreateInstance(caller)).Call() == "helper:ref");

            Assert(CallMaker<Maker>() == "made");
        }

        private static string CallMaker<T>() where T : IMaker => T.Make<Marked>();
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
            Assert(widget.GetMethod(Join("First", "Method")) is not null);
            Assert(widget.GetMethod(Join("first", "method"), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null);
            Assert(widget.GetField(Join("Fie", "ld")) is not null);
            Assert(widget.GetProperty(Join("Co", "unt")) is not null);
            Assert(widget.GetEvent(Join("Chan", "ged")) is not null);
            Assert(widget.GetNestedType(Join("Nes", "ted")) is null);

            // Interfaces are looked up by name without going through the member filters
            Type converter = Keep(typeof(IntConverter));
            Assert(converter.GetInterface(string.Empty) is null);
            Assert(converter.GetInterface(string.Empty, ignoreCase: true) is null);
            Assert(converter.GetInterface(Join("IConv", "erter`1")) is null);

            // GetInterface looks at the names of all interfaces of the type, which includes the ones that don't have a name
            Assert(converter.GetInterfaces().Length == 1);
            Assert(converter.GetInterface("IDisposable") is null);
            Assert(converter.GetMember(Join("Conv", "ert"), MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance).Length == 1);
        }
    }

    private static class TestAttributes
    {
        public static void Run()
        {
            // Named arguments are resolved by name
            MarkerAttribute attribute = typeof(Marked).GetCustomAttribute<MarkerAttribute>();
            Assert(attribute is not null);
            Assert(attribute.Label == "label");
            Assert(attribute.Number == 42);

            CustomAttributeData data = typeof(Marked).CustomAttributes.Single();
            Assert(data.AttributeType == typeof(MarkerAttribute));
            Assert(data.NamedArguments.Select(a => a.MemberName).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(new[] { "Label", "Number" }));

            Assert(new Marked() is not null);

            // Properties of attribute types are matched with the ones they override by name
            DerivedMarkerAttribute derived = typeof(MarkedWithOverride).GetCustomAttribute<DerivedMarkerAttribute>();
            Assert(derived is not null);
            Assert(derived.Value == "overridden");
            Assert(typeof(MarkedWithOverride).CustomAttributes.Single().NamedArguments.Single().MemberName == "Value");
            Assert(new MarkedWithOverride() is not null);
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
            Assert(stackTrace.Contains("+0x"));
            Assert(!stackTrace.Contains(Join("Unique", "Stripped")));

            // Methods of stripped types have no stack trace data, even if they are reflectable, but [StackTraceHidden] still applies
            Keep(typeof(HiddenFrameHolder));
            string hiddenStackTrace = null;
            try
            {
                HiddenFrameHolder.ThrowFromHiddenFrame();
            }
            catch (InvalidOperationException ex)
            {
                hiddenStackTrace = ex.StackTrace;
            }

            Assert(hiddenStackTrace is not null);
            Assert(!hiddenStackTrace.Contains(Join("Hidden", "FrameHolder")));
            Assert(!hiddenStackTrace.Contains("+0x"));
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
            Assert(Contains(image, Join("First", "Method")));

            Assert(!Contains(image, Join("Unique", "StrippedTypeNameMarker")));
            Assert(!Contains(image, Join("Unique", "StrippedThrowingMethodMarker")));
            Assert(!Contains(image, Join("Gvm", "Derived")));
            Assert(!Contains(image, Join("Int", "Converter")));
        }

        private static bool Contains(byte[] image, string text) => image.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0;
    }
}

namespace ControlNamespace
{
    public class MainDerived : Base
    {
        public override string Hello() => "MainDerived";
    }

    public class ControlType
    {
        public int ControlField;
        public int ControlProperty { get; set; }
        public void ControlMethod() { }

        public class NestedType { }
    }

    public class ControlGeneric<T> { }
}
