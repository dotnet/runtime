// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

#pragma warning disable CS0169, CS0414, CS0649, CS0067, IDE0051, IDE0052

[assembly: DisablePrivateReflection]
[assembly: InternalsVisibleTo("DisablePrivateReflection")]
[assembly: InternalsVisibleTo("TrimmingBehaviors")]
[assembly: TypeMap<PrivateReflectionUniverse>("Private", typeof(PrivateClass), typeof(PrivateClass))]

public sealed class PrivateReflectionUniverse;

[AttributeUsage(AttributeTargets.All)]
public sealed class MarkerAttribute : Attribute;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class GenericMarkerAttribute<T> : Attribute;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class TypeMarkerAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;

    public Type Other { get; set; }

    private static int Helper() => 1;
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class PrivateMarkerAttribute : Attribute;

internal static class PrivateContainer
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class NestedMarkerAttribute : Attribute;
}

public interface IPublicInterface;

public class PublicClass : IPublicInterface
{
    private int _privateField = 1;
    public int PublicField = 2;

    private static int PrivateMethod() => 3;

    [Marker]
    public static int PublicMethod() => PrivateMethod();

    // The attributes of a top-level attribute class are available, even if the class is not public (the compiler embeds some in
    // every assembly), the ones of a nested type that is not reflectable are not.
    [PrivateMarker]
    public void PrivateMarked() { }

    [PrivateContainer.NestedMarker]
    public void NestedMarked() { }

    public class PublicNested { }
    internal class InternalNested { }
    private class PrivateNested { }

    internal static Type PrivateNestedType => typeof(PrivateNested);
}

public interface IMemberInterface
{
    void Explicit();
    int Implicit();
}

// A public type with members of every accessibility: only the public ones are available to reflection
public class PublicMembers : IMemberInterface
{
    public int PublicField;
    internal int InternalField;
    protected int ProtectedField;
    protected internal int ProtectedInternalField;
    private protected int PrivateProtectedField;
    private int _privateField = 5;
    public static int PublicStaticField;
    internal static int InternalStaticField;
    private static int s_privateStaticField = 6;
    public const int PublicConstant = 1;
    internal const int InternalConstant = 2;

    public PublicMembers() { }
    internal PublicMembers(int value) { _privateField = value; }
    private PublicMembers(string value) { _privateField = value.Length; }
    protected PublicMembers(long value) { }

    public int PublicMethod() => 1;
    internal int InternalMethod() => 2;
    protected int ProtectedMethod() => 3;
    protected internal int ProtectedInternalMethod() => 4;
    private protected int PrivateProtectedMethod() => 5;
    private int PrivateMethod() => 6;
    public static int PublicStaticMethod() => 7;
    internal static int InternalStaticMethod() => 8;
    private static int PrivateStaticMethod() => 9;
    public virtual int PublicVirtualMethod() => 10;
    internal virtual int InternalVirtualMethod() => 11;
    protected virtual int ProtectedVirtualMethod() => 12;
    internal T InternalGenericMethod<T>(T value) => value;
    void IMemberInterface.Explicit() { }
    public int Implicit() => 13;

    public int PublicProperty { get; set; }
    public int PublicWithPrivateSetter { get; private set; }
    public int PublicWithInternalSetter { get; internal set; }
    internal int InternalProperty { get; set; }
    private int PrivateProperty { get; set; }
    protected int ProtectedProperty { get; set; }
    public int this[int index] => index;
    internal int this[string name] => name.Length;

    public event Action PublicEvent;
    internal event Action InternalEvent;
    private event Action PrivateEvent;

    public class PublicNestedType;
    internal class InternalNestedType;
}

public class PublicWithPrivateConstructor
{
    private PublicWithPrivateConstructor() { }
}

public struct PublicStructWithPrivateFields
{
    private int _a;
    private string _s;

    public PublicStructWithPrivateFields(int a, string s)
    {
        _a = a;
        _s = s;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PublicBlittableWithPrivateFields
{
    private int _a;
    private int _b;
}

// A virtual method that is generic is identified by the runtime through its metadata, but it is hidden from reflection all the same
public class PublicWithHiddenGenericVirtualMethod
{
    internal virtual T Get<T>(T value) => default!;
}

internal class PrivateOverridesHiddenGenericVirtualMethod : PublicWithHiddenGenericVirtualMethod
{
    internal override T Get<T>(T value) => value;
}

// Generic methods that are hidden from reflection but that the runtime type loader has to know about
public class PublicWithHiddenGenericMethod
{
    internal static T Echo<T>(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T CallEcho<T>(T value) => Echo(value);

    // The handle of the generic method is used in shared generic code
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object ExpressionOverEcho<T>() where T : class
    {
        System.Linq.Expressions.Expression<Func<T, T>> expression = value => Echo(value);
        return expression;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MethodInfo DelegateMethodOfEcho<T>() where T : class
    {
        Func<T, T> echo = Echo;
        return echo.Method;
    }

    // A local function of a generic method is a private generic method that the compiler generates
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T CallLocal<T>(T value)
    {
        return Local(value);

        static T Local(T x) => x;
    }
}

public interface IGenericMember
{
    T Echo<T>(T value);
}

public class PublicWithExplicitGenericImplementation : IGenericMember
{
    T IGenericMember.Echo<T>(T value) => value;
}

// Types with members that only [UnsafeAccessor] can get to
internal class PrivateWithSecrets
{
    private int _secret = 11;
    private static int s_secret = 12;

    public PrivateWithSecrets() { }
    private PrivateWithSecrets(int secret) { _secret = secret; }

    private int Secret() => _secret;
    private static int StaticSecret() => s_secret;
    private T GenericSecret<T>(T value) => value;
}

public class PublicWithSecrets
{
    private int _secret = 21;
    private static int s_secret = 22;

    public PublicWithSecrets() { }
    private PublicWithSecrets(int secret) { _secret = secret; }

    private int Secret() => _secret;
    private static int StaticSecret() => s_secret;
    internal T GenericSecret<T>(T value) => value;
}

public class PublicDerivedMembers : PublicMembers;

public static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int Secret(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "StaticSecret")]
    private static extern int StaticSecret(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_secret")]
    private static extern ref int SecretField(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_secret")]
    private static extern ref int StaticSecretField(PublicWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern PublicWithSecrets CreatePublicWithSecret(int secret);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GenericSecret")]
    private static extern T GenericSecret<T>(PublicWithSecrets instance, T value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int Secret(PrivateWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "StaticSecret")]
    private static extern int StaticSecret(PrivateWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_secret")]
    private static extern ref int SecretField(PrivateWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_secret")]
    private static extern ref int StaticSecretField(PrivateWithSecrets instance);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    private static extern PrivateWithSecrets CreatePrivateWithSecret(int secret);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GenericSecret")]
    private static extern T GenericSecret<T>(PrivateWithSecrets instance, T value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Secret")]
    private static extern int SecretByName([UnsafeAccessorType("PrivateWithSecrets")] object instance);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    [return: UnsafeAccessorType("PrivateWithSecrets")]
    private static extern object CreateByName(int secret);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_size")]
    private static extern ref int ListSize(List<PrivateWithSecrets> list);

    // A generic accessor that is called from shared code, so the runtime needs to know about the method
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T CallGenericSecret<T>(PrivateWithSecrets instance, T value) => GenericSecret(instance, value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T CallGenericSecret<T>(PublicWithSecrets instance, T value) => GenericSecret(instance, value);

    public static void Check()
    {
        PublicWithSecrets publicInstance = new();
        DisablePrivateReflectionTests.Check(Secret(publicInstance) == 21 && StaticSecret(publicInstance) == 22, "UnsafeAccessor methods of a public type");
        DisablePrivateReflectionTests.Check(SecretField(publicInstance) == 21 && StaticSecretField(publicInstance) == 22, "UnsafeAccessor fields of a public type");
        SecretField(publicInstance) = 23;
        DisablePrivateReflectionTests.Check(Secret(publicInstance) == 23, "UnsafeAccessor field write of a public type");
        DisablePrivateReflectionTests.Check(Secret(CreatePublicWithSecret(24)) == 24, "UnsafeAccessor constructor of a public type");
        DisablePrivateReflectionTests.Check(GenericSecret(publicInstance, 5) == 5 && CallGenericSecret(publicInstance, "a") == "a" && CallGenericSecret(publicInstance, new object()) is not null,
            "UnsafeAccessor generic method of a public type");

        PrivateWithSecrets privateInstance = new();
        DisablePrivateReflectionTests.Check(Secret(privateInstance) == 11 && StaticSecret(privateInstance) == 12, "UnsafeAccessor methods of a private type");
        DisablePrivateReflectionTests.Check(SecretField(privateInstance) == 11 && StaticSecretField(privateInstance) == 12, "UnsafeAccessor fields of a private type");
        SecretField(privateInstance) = 13;
        DisablePrivateReflectionTests.Check(Secret(privateInstance) == 13, "UnsafeAccessor field write of a private type");
        DisablePrivateReflectionTests.Check(Secret(CreatePrivateWithSecret(14)) == 14, "UnsafeAccessor constructor of a private type");
        DisablePrivateReflectionTests.Check(GenericSecret(privateInstance, 5) == 5 && CallGenericSecret(privateInstance, "a") == "a" && CallGenericSecret(privateInstance, new object()) is not null,
            "UnsafeAccessor generic method of a private type");
        DisablePrivateReflectionTests.Check(SecretByName(privateInstance) == 13 && SecretByName(CreateByName(15)) == 15, "UnsafeAccessor with the name of a private type");

        List<PrivateWithSecrets> list = [privateInstance, new()];
        DisablePrivateReflectionTests.Check(ListSize(list) == 2, "UnsafeAccessor on a private field of a generic type over a private type");
    }
}

public class PublicGeneric<T> where T : class;

internal static class PrivateThrower
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Throw(PrivateClass value, int count) => throw new InvalidOperationException("private");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetEnvironmentStackTrace() => Environment.StackTrace;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetStackTraceText() => new StackTrace().ToString();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowPrivateException() => throw new PrivateException();
}

internal sealed class PrivateException : Exception;

public class PublicThrower
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PrivateThrow(string text) => throw new InvalidOperationException(text);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal int InternalThrow() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallPrivate() => PrivateThrow("a");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInternal() => new PublicThrower().InternalThrow();

    internal static async Task PrivateAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }

    public static Task CallAsync() => PrivateAsync();
}

public class PublicOuter
{
    public class PublicMiddle
    {
        public class PublicInner { }
    }

    internal class InternalMiddle
    {
        public class PublicInner { }
    }
}

internal static class PrivateStatic
{
    internal static int Counter;
    internal static void Hit() => Counter++;
    internal static void GenericHit<T>(T value) => Counter++;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static MethodBase GetCurrentFrameMethod() => new StackTrace().GetFrame(0)?.GetMethod();
}

internal interface IPrivateInterface;

internal class PrivateClass : IPrivateInterface
{
    public int Value = 42;
    public PrivateClass() { }
    public override int GetHashCode() => Value;
}

internal struct PrivateStruct
{
    public int Value;
}

internal struct PrivateStructWithReference
{
    public string Text;
}

internal abstract class PrivateAbstractClass;

internal class PrivateFromAbstract : PrivateAbstractClass;

internal sealed class PrivateSealedClass;

internal ref struct PrivateRefStruct
{
    public int Value;
}

// Nested types of different accessibility and kinds, hidden because the type that contains them is
internal class PrivateNestedKinds
{
    private class NestedPrivate;
    protected class NestedFamily;
    private protected class NestedFamAndAssembly;
    protected internal class NestedFamOrAssembly;
    private abstract class NestedAbstract;
    private sealed class NestedSealed;
    private static class NestedStatic;
    public struct NestedStruct;
    protected interface INestedInterface;

    public static Type[] All =>
    [
        typeof(NestedPrivate), typeof(NestedFamily), typeof(NestedFamAndAssembly), typeof(NestedFamOrAssembly),
        typeof(NestedAbstract), typeof(NestedSealed), typeof(NestedStatic), typeof(NestedStruct), typeof(INestedInterface),
    ];
}

[Marker]
public class PublicMarked;

internal class PrivateDerivedFromMarked : PublicMarked;

internal interface IRunner
{
    static abstract void Run();
}

internal class PrivateRunner : IRunner
{
    public static void Run() => PrivateStatic.Hit();
}

internal static class UsesStaticAbstract
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Call<T>() where T : IRunner => T.Run();
}


public class PublicVirtualBase
{
    [Marker]
    public virtual int Method() => 1;

    public virtual int Property => 1;
}

internal class PrivateVirtualMiddle : PublicVirtualBase
{
    public override int Method() => 2;

    public override int Property => 2;
}

// A generic type is reflectable, and it can derive from a type that is not
internal class PrivateGenericLeaf<T> : PrivateVirtualMiddle
{
    public override int Method() => 3;
}

internal class PrivateWithNested
{
    public class PublicNestedInPrivate { public int Value = 7; }
}

internal class PrivateBase;

internal class PrivateDerived : PrivateBase;

// Types that can't have their metadata removed
internal enum PrivateEnum { A, B }

internal delegate int PrivateCallback(int value);

internal class PrivateGeneric<T> { public T Value; }

internal sealed class PrivateAttributeWithNestedGeneric : Attribute
{
    public class Nested<T>;
}

internal class PrivateWithNestedEnum
{
    public enum Mode { On, Off }
}

internal class PrivateWithGenericVirtualMethod
{
    public virtual T Get<T>(T value) => value;
}

internal class PrivateWithClosure
{
    public static Type GetClosureType<T>(T value)
    {
        Func<T> closure = () => value;
        return closure.Target.GetType();
    }
}

internal interface IDynamicTarget { int Get(); }

// Attributes that mention private types as arguments (a generic argument, typeof, a nested generic argument, an array)
[GenericMarker<PrivateClass>]
[GenericMarker<PrivateStruct>]
[GenericMarker<List<PrivateClass>>]
[GenericMarker<PrivateClass[]>]
[TypeMarker(typeof(PrivateClass))]
[TypeMarker(typeof(PrivateStruct))]
[TypeMarker(typeof(List<PrivateClass>))]
[TypeMarker(typeof(PrivateClass[]))]
public class PublicWithPrivateArguments;

// The attributes of a private type are not available, whatever they are
[Marker]
[GenericMarker<PrivateClass>]
[TypeMarker(typeof(PrivateClass))]
internal class PrivateWithAttributes;

// Nothing but the attributes of a member uses these types, so they only have a MethodTable because the attributes mention them
internal sealed class PrivateOnlyInGenericArgument;
internal sealed class PrivateOnlyInTypeof;
internal sealed class PrivateOnlyInNamedArgument;

public class PublicWithAttributedMember
{
    [GenericMarker<PrivateOnlyInGenericArgument>]
    [TypeMarker(typeof(PrivateOnlyInTypeof), Other = typeof(PrivateOnlyInNamedArgument))]
    public void Method() { }
}

// A generic type nested in a private type is private too (like the closures that the compiler generates)
internal class PrivateWithGenericNested
{
    public class Box<T> { public T Value; }
    public abstract class AbstractBox<T>;
    public sealed class SealedBox<T>;
}

// Attributes that mention instances of a private generic type
[GenericMarker<PrivateWithGenericNested.Box<int>>]
[GenericMarker<PrivateWithGenericNested.Box<string>>]
[GenericMarker<List<PrivateWithGenericNested.Box<int>>>]
[GenericMarker<PrivateWithGenericNested.Box<PrivateClass>[]>]
[TypeMarker(typeof(PrivateWithGenericNested.Box<int>))]
[TypeMarker(typeof(PrivateWithGenericNested.Box<string>))]
[TypeMarker(typeof(List<PrivateWithGenericNested.Box<int>>))]
[TypeMarker(typeof(PrivateWithGenericNested.Box<PrivateClass>[]))]
public class PublicWithPrivateGenericArguments;

// Nothing but these attributes uses these instances of the private generic type, so they only have a MethodTable because the attributes mention them
[GenericMarker<PrivateWithGenericNested.Box<long>>]
[TypeMarker(typeof(PrivateWithGenericNested.Box<double>), Other = typeof(PrivateWithGenericNested.Box<float>))]
public class PublicWithPrivateGenericOnlyInAttributes;

// The generic type definition of a private generic type
[TypeMarker(typeof(PrivateWithGenericNested.Box<>))]
public class PublicWithPrivateGenericDefinition;

internal sealed unsafe class PrivateExposedDetails : IComExposedClass
{
    public static ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count)
    {
        count = 7;
        return null;
    }
}

// The shape that the COM source generators use: a generic attribute of the runtime libraries over a private type, on a public type
[ComExposedClass<PrivateExposedDetails>]
public class PublicComClass;

[DynamicInterfaceCastableImplementation]
internal interface IPrivateDynamicImpl : IDynamicTarget
{
    int IDynamicTarget.Get() => 42;
}

internal sealed class DynamicCastable : IDynamicInterfaceCastable
{
    public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) => interfaceType.Equals(typeof(IDynamicTarget).TypeHandle);
    public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType) => typeof(IPrivateDynamicImpl).TypeHandle;
}

public static class DisablePrivateReflectionTests
{
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Make(object o) => o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Hide(Type t) => t;

    internal static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Expect<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new Exception(message + " did not throw " + typeof(TException).Name);
    }

    private static void CheckPlaceholder(Type type, string message)
    {
        Check(type.Name.StartsWith("<PrivateType", StringComparison.Ordinal), message + ": Name");
        Check(type.FullName == type.Name && type.ToString() == type.Name, message + ": FullName and ToString");
        Check(type.Namespace is null && type.DeclaringType is null, message + ": Namespace and DeclaringType");
        Expect<NotSupportedException>(() => _ = type.GUID, message + ": GUID");
    }

    private static void CheckNoMembers(Type type, string message)
    {
        Check(type.GetMethods(AllDeclared).Length == 0, message + ": methods");
        Check(type.GetConstructors(AllDeclared & ~BindingFlags.Static).Length == 0, message + ": constructors");
        Check(type.GetFields(AllDeclared).Length == 0, message + ": fields");
        Check(type.GetProperties(AllDeclared).Length == 0, message + ": properties");
        Check(type.GetEvents(AllDeclared).Length == 0, message + ": events");
        Check(type.GetNestedTypes(AllDeclared).Length == 0, message + ": nested types");
        Check(type.GetCustomAttributes(inherit: false).Length == 0, message + ": custom attributes");
        Check(type.GetMethod("Missing", AllDeclared) is null, message + ": method by name");
    }

    private static Type GetPrivateType(Type type)
        => type.HasElementType ? type.GetElementType() : type.IsConstructedGenericType ? type.GetGenericArguments()[0] : type;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int GetComInterfaceCount(IComExposedDetails details)
    {
        details.GetComInterfaceEntries(out int count);
        return count;
    }

    // Checks that a type has the custom attributes of PublicWithPrivateArguments, and that the private types that they mention
    // are surfaced as private types. Public so that a type of another assembly (that can see the private types) can be checked as well.
    public static void CheckAttributesWithPrivateArguments(Type type, string message)
    {
        object[] attributes = type.GetCustomAttributes(inherit: false);
        Type[][] argumentSets =
        [
            [.. attributes.Where(a => a.GetType().IsGenericType).Select(a => a.GetType().GetGenericArguments()[0])],
            [.. attributes.OfType<TypeMarkerAttribute>().Select(a => a.Type)],
            [.. type.CustomAttributes.Where(a => a.AttributeType.IsGenericType).Select(a => a.AttributeType.GetGenericArguments()[0])],
            [.. type.CustomAttributes.Where(a => a.AttributeType == typeof(TypeMarkerAttribute)).Select(a => (Type)a.ConstructorArguments[0].Value)],
        ];
        Type[] expected = [typeof(PrivateClass), typeof(PrivateStruct), typeof(List<PrivateClass>), typeof(PrivateClass[])];

        Check(attributes.Length == 8 && type.CustomAttributes.Count() == 8, message + ": number of custom attributes");
        foreach (Type[] arguments in argumentSets)
        {
            Check(arguments.Length == 4 && expected.All(e => arguments.Contains(e)), message + ": attribute arguments");

            foreach (Type argument in arguments)
            {
                Type privateType = GetPrivateType(argument);
                Check(privateType == typeof(PrivateClass) || privateType == typeof(PrivateStruct), message + ": private type of an attribute argument");
                CheckPlaceholder(privateType, message + ": private type of an attribute argument");
                CheckNoMembers(privateType, message + ": private type of an attribute argument");
                Check(!privateType.IsVisible && !privateType.Name.Contains(nameof(PrivateClass)) && !privateType.Name.Contains(nameof(PrivateStruct)), message + ": name of a private type of an attribute argument");
            }
        }

        Check(type.IsDefined(typeof(GenericMarkerAttribute<PrivateClass>), inherit: false) &&
            type.GetCustomAttribute<GenericMarkerAttribute<PrivateStruct>>() is not null &&
            type.GetCustomAttributes(typeof(GenericMarkerAttribute<List<PrivateClass>>), inherit: false).Length == 1, message + ": lookup of an attribute that mentions a private type");
    }

    private static Type GetPrivateGenericInstance(Type type)
    {
        if (type.HasElementType)
            return GetPrivateGenericInstance(type.GetElementType());

        return type == typeof(List<PrivateWithGenericNested.Box<int>>) ? type.GetGenericArguments()[0] : type;
    }

    // Checks that a type has the custom attributes of PublicWithPrivateGenericArguments, and that the instances of the private generic type
    // that they mention are surfaced as private types. Public so that a type of another assembly (that can see the private types) can be checked as well.
    public static void CheckAttributesWithPrivateGenericArguments(Type type, string message)
    {
        object[] attributes = type.GetCustomAttributes(inherit: false);
        Type[][] argumentSets =
        [
            [.. attributes.Where(a => a.GetType().IsGenericType).Select(a => a.GetType().GetGenericArguments()[0])],
            [.. attributes.OfType<TypeMarkerAttribute>().Select(a => a.Type)],
            [.. type.CustomAttributes.Where(a => a.AttributeType.IsGenericType).Select(a => a.AttributeType.GetGenericArguments()[0])],
            [.. type.CustomAttributes.Where(a => a.AttributeType == typeof(TypeMarkerAttribute)).Select(a => (Type)a.ConstructorArguments[0].Value)],
        ];
        Type[] expected = [typeof(PrivateWithGenericNested.Box<int>), typeof(PrivateWithGenericNested.Box<string>), typeof(List<PrivateWithGenericNested.Box<int>>), typeof(PrivateWithGenericNested.Box<PrivateClass>[])];

        Check(attributes.Length == 8 && type.CustomAttributes.Count() == 8, message + ": number of custom attributes");
        foreach (Type[] arguments in argumentSets)
        {
            Check(arguments.Length == 4 && expected.All(e => arguments.Contains(e)), message + ": attribute arguments");

            foreach (Type argument in arguments)
            {
                Type instance = GetPrivateGenericInstance(argument);
                CheckPlaceholder(instance, message + ": private generic type of an attribute argument");
                CheckNoMembers(instance, message + ": private generic type of an attribute argument");
                Check(instance.IsGenericType && !instance.IsVisible && instance.GetGenericArguments().Length == 1, message + ": generic arguments of a private generic type of an attribute argument");
                Check(!instance.Name.Contains("Box") && !instance.Name.Contains(nameof(PrivateWithGenericNested)), message + ": name of a private generic type of an attribute argument");
                Expect<Exception>(() => Activator.CreateInstance(instance), message + ": Activator on a private generic type of an attribute argument");
            }
        }

        Check(GetPrivateGenericInstance(expected[0]).Name != GetPrivateGenericInstance(expected[1]).Name, message + ": placeholder names of different instances of a private generic type differ");
        Check(GetPrivateGenericInstance(expected[0]).GetGenericArguments()[0] == typeof(int) && GetPrivateGenericInstance(expected[1]).GetGenericArguments()[0] == typeof(string) &&
            GetPrivateGenericInstance(expected[3]).GetGenericArguments()[0] == typeof(PrivateClass), message + ": generic arguments of private generic types");

        // The names of the attribute types do not fail (the assembly of a private type is not available).
        Check(attributes.Where(a => a.GetType().IsGenericType).All(a => a.GetType().FullName is not null && a.GetType().AssemblyQualifiedName is not null && a.GetType().ToString() is not null),
            message + ": name of an attribute type that mentions a private generic type");

        Check(type.IsDefined(typeof(GenericMarkerAttribute<PrivateWithGenericNested.Box<int>>), inherit: false) &&
            type.GetCustomAttribute<GenericMarkerAttribute<PrivateWithGenericNested.Box<string>>>() is not null &&
            type.GetCustomAttributes(typeof(GenericMarkerAttribute<List<PrivateWithGenericNested.Box<int>>>), inherit: false).Length == 1, message + ": lookup of an attribute that mentions a private generic type");
    }

    // The kind of type, abstract and sealed of a private type are the real ones. NativeAOT does not keep the other attributes of its declaration
    // (the accessibility of a nested type, the layout, serializable, ...), so they are only checked for their default values, which the types of the table have.
    private static string Describe(Type type)
    {
        string kind = type.IsInterface ? "interface" : type.IsByRefLike ? "refstruct" : type.IsValueType ? "struct" : "class";

        return kind + (type.IsAbstract ? " abstract" : "") + (type.IsSealed ? " sealed" : "");
    }

    private static void CheckTypeProperties()
    {
        (Type Type, string Expected)[] shapes =
        [
            (typeof(PrivateClass), "class"),
            (typeof(PrivateAbstractClass), "class abstract"),
            (typeof(PrivateSealedClass), "class sealed"),
            (typeof(PrivateStatic), "class abstract sealed"),
            (typeof(IPrivateInterface), "interface abstract"),
            (typeof(PrivateStruct), "struct sealed"),
            (typeof(PrivateRefStruct), "refstruct sealed"),
            (typeof(PublicClass.InternalNested), "class"),
            (PublicClass.PrivateNestedType, "class"),
            (typeof(PublicOuter.InternalMiddle.PublicInner), "class"),
            (typeof(PrivateWithNested.PublicNestedInPrivate), "class"),
            (PrivateNestedKinds.All[0], "class"),
            (PrivateNestedKinds.All[1], "class"),
            (PrivateNestedKinds.All[2], "class"),
            (PrivateNestedKinds.All[3], "class"),
            (PrivateNestedKinds.All[4], "class abstract"),
            (PrivateNestedKinds.All[5], "class sealed"),
            (PrivateNestedKinds.All[6], "class abstract sealed"),
            (PrivateNestedKinds.All[7], "struct sealed"),
            (PrivateNestedKinds.All[8], "interface abstract"),
        ];

        for (int i = 0; i < shapes.Length; i++)
        {
            (Type type, string expected) = shapes[i];
            string message = $"private type {i} ({expected})";
            Check(Describe(type) == expected, message + ": described as " + Describe(type));
            CheckPlaceholder(type, message);
            Check(!type.IsPublic && !type.IsVisible && !type.IsNested && !type.IsGenericType && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters && !type.IsGenericParameter, message + ": public and generic flags");
            Check(!type.IsEnum && !type.IsPrimitive && !type.IsArray && !type.IsSZArray && !type.IsPointer && !type.IsByRef && !type.IsFunctionPointer && !type.IsCOMObject && !type.IsSpecialName && !type.IsImport, message + ": kind flags");
            Check(!type.HasElementType && type.GetElementType() is null && Type.GetTypeCode(type) == TypeCode.Object, message + ": element type and type code");
            Check(type.IsClass == !(type.IsInterface || type.IsValueType) && type.IsAnsiClass && !type.IsUnicodeClass && !type.IsAutoClass, message + ": class and string format flags");
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
            Check(!type.IsSerializable, message + ": IsSerializable");
#pragma warning restore SYSLIB0050
            Check(type.GetInterfaces().Length == (type == typeof(PrivateClass) ? 1 : 0), message + ": interfaces");
            Type expectedBase = type.IsInterface ? null : type.IsValueType ? typeof(ValueType) : typeof(object);
            Check(type.BaseType == expectedBase, message + ": base type");
            Expect<ArgumentException>(() => type.GetEnumUnderlyingType(), message + ": GetEnumUnderlyingType");
        }

        // Assignability is answered from the type hierarchy.
        Check(typeof(object).IsAssignableFrom(typeof(PrivateClass)) && typeof(IPrivateInterface).IsAssignableFrom(typeof(PrivateClass)) && !typeof(PrivateClass).IsAssignableFrom(typeof(object)), "IsAssignableFrom with a private class");
        Check(typeof(PrivateBase).IsAssignableFrom(typeof(PrivateDerived)) && typeof(PrivateDerived).IsSubclassOf(typeof(PrivateBase)) && !typeof(PrivateBase).IsSubclassOf(typeof(PrivateBase)), "IsSubclassOf and IsAssignableFrom between private classes");
        Check(typeof(ValueType).IsAssignableFrom(typeof(PrivateStruct)) && typeof(PrivateStruct).IsSubclassOf(typeof(ValueType)) && !typeof(IPrivateInterface).IsAssignableFrom(typeof(PrivateStruct)), "IsAssignableFrom with a private struct");
        Check(typeof(PrivateClass).IsInstanceOfType(Make(new PrivateClass())) && typeof(IPrivateInterface).IsInstanceOfType(Make(new PrivateClass())) && !typeof(PrivateBase).IsInstanceOfType(Make(new PrivateClass())), "IsInstanceOfType with private types");
        Check(typeof(PrivateStruct).IsInstanceOfType(Make(new PrivateStruct())) && !typeof(PrivateStruct).IsInstanceOfType(Make(new PrivateClass())), "IsInstanceOfType with a private struct");

        // Constructed types over a private type
        Type privateClass = Hide(typeof(PrivateClass));
        Type szArray = privateClass.MakeArrayType();
        Type multiDimensional = privateClass.MakeArrayType(2);
        Type pointer = privateClass.MakePointerType();
        Type byRef = privateClass.MakeByRefType();
        Check(szArray == typeof(PrivateClass[]) && szArray.IsArray && szArray.IsSZArray && szArray.GetArrayRank() == 1 && szArray.GetElementType() == privateClass && szArray.HasElementType && !szArray.IsVisible, "array of a private type");
        Check(multiDimensional.IsArray && !multiDimensional.IsSZArray && multiDimensional.GetArrayRank() == 2 && multiDimensional.GetElementType() == privateClass && !multiDimensional.IsVisible, "multidimensional array of a private type");
        Check(pointer.IsPointer && !pointer.IsArray && pointer.GetElementType() == privateClass && !pointer.IsVisible, "pointer to a private type");
        Check(byRef.IsByRef && !byRef.IsArray && byRef.GetElementType() == privateClass && !byRef.IsVisible, "by ref to a private type");
        CheckPlaceholder(szArray.GetElementType(), "element type of an array of a private type");

        // Instances and the definition of a private generic type
        Type genericInstance = typeof(PrivateWithGenericNested.Box<int>);
        Type genericDefinition = Hide(typeof(PrivateWithGenericNested.Box<>));
        Check(genericInstance.IsGenericType && !genericInstance.IsGenericTypeDefinition && !genericInstance.ContainsGenericParameters && !genericInstance.IsVisible && genericInstance.IsClass, "instance of a private generic type");
        Check(genericDefinition.IsGenericType && genericDefinition.IsGenericTypeDefinition && genericDefinition.ContainsGenericParameters && !genericDefinition.IsVisible && genericDefinition.IsClass, "private generic type definition");

        (Type Type, string Expected)[] genericShapes =
        [
            (typeof(PrivateWithGenericNested.AbstractBox<int>), "class abstract"),
            (typeof(PrivateWithGenericNested.SealedBox<string>), "class sealed"),
            (Hide(typeof(PrivateWithGenericNested.AbstractBox<>)), "class abstract"),
            (Hide(typeof(PrivateWithGenericNested.SealedBox<>)), "class sealed"),
            (PrivateWithClosure.GetClosureType(3), "class sealed"),
        ];
        foreach ((Type type, string expected) in genericShapes)
        {
            Check(type.IsGenericType && Describe(type) == expected, "private generic type " + expected);
        }

        // A type that nothing allocates (a sealed class here) can still be compared to a type that comes from somewhere else.
        int sealedCount = 0;
        foreach ((Type type, _) in shapes)
        {
            if (type == typeof(PrivateSealedClass))
                sealedCount++;
        }
        Check(sealedCount == 1, "comparison with a private type that nothing allocates");

        // An abstract class can't be allocated even if the program has a vtable for it (because of a class that derives from it).
        Check(Make(new PrivateFromAbstract()) is PrivateAbstractClass, "class that derives from an abstract private class");
        Expect<MemberAccessException>(() => RuntimeHelpers.GetUninitializedObject(Hide(typeof(PrivateAbstractClass))), "GetUninitializedObject of an abstract private class");
    }

    private static string Names<T>(IEnumerable<T> members) where T : MemberInfo
        => string.Join(",", members.Select(m => m.Name).Order(StringComparer.Ordinal));

    private static void CheckNames<T>(IEnumerable<T> members, string expected, string message) where T : MemberInfo
        => Check(Names(members) == expected, message + ": [" + Names(members) + "] instead of [" + expected + "]");

    // Only the public members of a type that is externally visible are available to reflection. The non public ones (including the
    // protected ones, the explicit interface implementations, the backing fields and the hidden accessors) are not, but they work for the program.
    internal static void CheckMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string message)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // The members that the type declares, or all of them if it is a derived type
        bool declaredOnly = type == typeof(PublicMembers);
        if (declaredOnly)
        {
            CheckNames(type.GetFields(AllDeclared), "PublicConstant,PublicField,PublicStaticField", message + ": fields");
            CheckNames(type.GetMethods(AllDeclared),
                "Implicit,PublicMethod,PublicStaticMethod,PublicVirtualMethod,add_PublicEvent,get_Item,get_PublicProperty,get_PublicWithInternalSetter,get_PublicWithPrivateSetter,remove_PublicEvent,set_PublicProperty",
                message + ": methods");
            CheckNames(type.GetConstructors(AllDeclared), ".ctor", message + ": constructors");
            CheckNames(type.GetProperties(AllDeclared), "Item,PublicProperty,PublicWithInternalSetter,PublicWithPrivateSetter", message + ": properties");
            CheckNames(type.GetEvents(AllDeclared), "PublicEvent", message + ": events");
            CheckNames(type.GetNestedTypes(AllDeclared), "PublicNestedType", message + ": nested types");
        }

        foreach (string name in new[]
        {
            "InternalMethod", "ProtectedMethod", "ProtectedInternalMethod", "PrivateProtectedMethod", "PrivateMethod", "InternalStaticMethod", "PrivateStaticMethod",
            "InternalVirtualMethod", "ProtectedVirtualMethod", "InternalGenericMethod", "get_InternalProperty", "set_PrivateProperty", "get_ProtectedProperty", "set_PublicWithPrivateSetter",
            "set_PublicWithInternalSetter", "add_InternalEvent", "remove_PrivateEvent",
        })
        {
            Check(type.GetMethod(name, all) is null && type.GetMember(name, all).Length == 0, message + ": method by name " + name);
        }

        foreach (string name in new[] { "InternalField", "ProtectedField", "ProtectedInternalField", "PrivateProtectedField", "_privateField", "InternalStaticField", "s_privateStaticField", "InternalConstant", "<PublicProperty>k__BackingField" })
            Check(type.GetField(name, all) is null && type.GetMember(name, all).Length == 0, message + ": field by name " + name);

        foreach (string name in new[] { "InternalProperty", "PrivateProperty", "ProtectedProperty" })
            Check(type.GetProperty(name, all) is null, message + ": property by name " + name);

        foreach (string name in new[] { "InternalEvent", "PrivateEvent" })
            Check(type.GetEvent(name, all) is null, message + ": event by name " + name);

        Check(type.GetProperties(all).Count(p => p.Name == "Item") == 1 && type.GetMethods(all).Count(m => m.Name == "get_Item") == 1, message + ": indexers");
        Check(type.GetConstructors(all).Length == 1 &&
            type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(int)], null) is null &&
            type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(string)], null) is null &&
            type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(long)], null) is null &&
            type.GetConstructor(Type.EmptyTypes) is not null, message + ": constructors by signature");
        Check(!type.GetMethods(all).Any(m => m.Name.Contains("Explicit")), message + ": explicit interface implementation");
        Check(type.GetMember("InternalNestedType", all).Length == 0, message + ": nested type by name");
        Check(type.GetMembers(all).All(m => !m.Name.StartsWith("Internal", StringComparison.Ordinal) && !m.Name.StartsWith("Private", StringComparison.Ordinal) &&
            !m.Name.StartsWith("Protected", StringComparison.Ordinal) && m.Name != "_privateField"), message + ": members");

        // Use of the public members
        object instance = Activator.CreateInstance(type);
        Check((int)type.GetMethod("PublicMethod").Invoke(instance, null) == 1 && (int)type.GetMethod("PublicStaticMethod", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Invoke(null, null) == 7, message + ": public methods");
        type.GetField("PublicField").SetValue(instance, 9);
        Check((int)type.GetField("PublicField").GetValue(instance) == 9, message + ": public field");
        type.GetProperty("PublicProperty").SetValue(instance, 4);
        Check((int)type.GetProperty("PublicProperty").GetValue(instance) == 4 && (int)type.GetProperty("Item").GetValue(instance, [5]) == 5, message + ": public properties");
        Check((int)type.InvokeMember("PublicMethod", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, instance, null) == 1, message + ": InvokeMember of a public method");
        Expect<MissingMemberException>(() => type.InvokeMember("InternalMethod", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, instance, null), message + ": InvokeMember of an internal method");
        Expect<MissingMemberException>(() => type.InvokeMember("_privateField", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, instance, null), message + ": InvokeMember of a private field");
        Check(Activator.CreateInstance(type, nonPublic: true) is not null, message + ": Activator with nonPublic");

        // A property with a hidden accessor
        PropertyInfo withPrivateSetter = type.GetProperty("PublicWithPrivateSetter");
        Check(withPrivateSetter.GetMethod is not null && withPrivateSetter.SetMethod is null && withPrivateSetter.GetSetMethod(nonPublic: true) is null &&
            withPrivateSetter.GetAccessors(nonPublic: true).Length == 1, message + ": property with a hidden setter");
        PropertyInfo withInternalSetter = type.GetProperty("PublicWithInternalSetter");
        Check(withInternalSetter.GetMethod is not null && withInternalSetter.GetSetMethod(nonPublic: true) is null && !withInternalSetter.CanWrite, message + ": property with an internal setter");

        // The explicit implementation is reachable through the interface
        typeof(IMemberInterface).GetMethod("Explicit").Invoke(instance, null);
        Check((int)typeof(IMemberInterface).GetMethod("Implicit").Invoke(instance, null) == 13, message + ": interface method");
    }

    private static void CheckMemberHandles()
    {
        Type type = typeof(PublicMembers);
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // The handle of a member that is hidden can't be converted to a MethodBase or FieldInfo (the C# compiler emits them for expression trees)
        Expect<NotSupportedException>(() => { System.Linq.Expressions.Expression<Func<PublicMembers, int>> e = m => m.InternalMethod(); GC.KeepAlive(e); }, "ldtoken of a hidden method");
        Expect<NotSupportedException>(() => { System.Linq.Expressions.Expression<Func<PublicMembers, int>> e = m => m.InternalField; GC.KeepAlive(e); }, "ldtoken of a hidden field");

        // Delegates to hidden methods work, but their MethodInfo is not available and they can't be found by name
        Func<int> hiddenMethod = PublicMembers.InternalStaticMethod;
        Check(hiddenMethod() == 8, "delegate to a hidden method");
        Expect<NotSupportedException>(() => _ = hiddenMethod.Method, "Delegate.Method of a hidden method");
        Expect<ArgumentException>(() => Delegate.CreateDelegate(typeof(Func<int>), type, "InternalStaticMethod"), "CreateDelegate by name of a hidden method");
        Check(((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), type, "PublicStaticMethod"))() == 7, "CreateDelegate by name of a public method");

        // The constructor of a type is hidden if it is not public
        Expect<MissingMethodException>(() => Activator.CreateInstance(typeof(PublicWithPrivateConstructor), nonPublic: true), "Activator with a hidden constructor");
        Expect<MissingMethodException>(() => Activator.CreateInstance(typeof(PublicWithPrivateConstructor)), "Activator with a hidden constructor, public only");
        Check(typeof(PublicWithPrivateConstructor).GetConstructors(all).Length == 0, "hidden constructor");
        Expect<NotSupportedException>(() => type.GetInterfaceMap(typeof(IMemberInterface)), "GetInterfaceMap with a hidden implementation");

        // Types that need their private state for the program work as always
        PublicStructWithPrivateFields a = new(1, "a");
        Check(Make(a).Equals(new PublicStructWithPrivateFields(1, "a")) && !Make(a).Equals(new PublicStructWithPrivateFields(2, "a")) &&
            Make(a).GetHashCode() == Make(new PublicStructWithPrivateFields(1, "a")).GetHashCode(), "ValueType.Equals and GetHashCode over hidden fields");
        Check((int)Marshal.OffsetOf<PublicBlittableWithPrivateFields>("_b") == 4, "Marshal.OffsetOf over a hidden field");

        // Generic methods that are hidden but that the runtime type loader needs
        PublicWithHiddenGenericVirtualMethod gvm = (PublicWithHiddenGenericVirtualMethod)Make(new PrivateOverridesHiddenGenericVirtualMethod());
        Check(gvm.Get(5) == 5 && gvm.Get("a") == "a" && gvm.Get<object>(gvm) == gvm, "hidden generic virtual method");
        Check(typeof(PublicWithHiddenGenericVirtualMethod).GetMethod("Get", all) is null, "hidden generic virtual method by name");
        Check(PublicWithHiddenGenericMethod.CallEcho("a") == "a" && PublicWithHiddenGenericMethod.CallEcho(3) == 3 && PublicWithHiddenGenericMethod.CallEcho(gvm) == gvm, "hidden generic method");
        Check(typeof(PublicWithHiddenGenericMethod).GetMethod("Echo", all) is null, "hidden generic method by name");
        Expect<NotSupportedException>(() => PublicWithHiddenGenericMethod.ExpressionOverEcho<string>(), "ldtoken of a hidden generic method in shared generic code");
        Expect<NotSupportedException>(() => PublicWithHiddenGenericMethod.DelegateMethodOfEcho<string>(), "Delegate.Method of a hidden generic method in shared generic code");
        Check(PublicWithHiddenGenericMethod.CallLocal("a") == "a" && PublicWithHiddenGenericMethod.CallLocal(3) == 3 && PublicWithHiddenGenericMethod.CallLocal(gvm) == gvm, "hidden generic local function");
        IGenericMember explicitGeneric = (IGenericMember)Make(new PublicWithExplicitGenericImplementation());
        Check(explicitGeneric.Echo("a") == "a" && explicitGeneric.Echo(5) == 5 && explicitGeneric.Echo<object>(explicitGeneric) == explicitGeneric, "hidden explicit implementation of a generic interface method");
    }

    private static string CaptureStackTrace(Action action, out MethodBase targetSite)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            targetSite = e.TargetSite;
            return e.ToString();
        }

        targetSite = null;
        return null;
    }

    // The stack traces are not affected by the attribute: the names of the types and methods that are not available to reflection are there
    private static void CheckStackTraces()
    {
        string trace = CaptureStackTrace(() => PrivateThrower.Throw(null, 1), out MethodBase site);
        Check(trace is not null && trace.Contains("PrivateThrower.Throw(") && trace.Contains("PrivateClass") && !trace.Contains("<PrivateType"), "stack trace of a method of a private type: " + trace);
        Check(site is null, "TargetSite of a method of a private type");

        // The name of the exception type is the real one where the runtime knows it (CoreCLR)
        trace = CaptureStackTrace(() => PrivateThrower.ThrowPrivateException(), out _);
        Check(trace is not null && trace.Contains("PrivateThrower.ThrowPrivateException(") && (!RuntimeFeature.IsDynamicCodeSupported || trace.StartsWith("PrivateException:")), "exception of a private type: " + trace);

        trace = CaptureStackTrace(() => PublicThrower.CallPrivate(), out site);
        Check(trace is not null && trace.Contains("PublicThrower.PrivateThrow(") && trace.Contains("PublicThrower.CallPrivate("), "stack trace of a private method of a public type: " + trace);
        Check(site is null, "TargetSite of a private method of a public type");

        trace = CaptureStackTrace(() => PublicThrower.CallInternal(), out _);
        Check(trace is not null && trace.Contains("PublicThrower.InternalThrow("), "stack trace of an internal method of a public type: " + trace);

        trace = CaptureStackTrace(() => PublicThrower.CallAsync().GetAwaiter().GetResult(), out _);
        // The state machine of the async method is resolved to the method only where the runtime can use reflection for that.
        Check(trace is not null && trace.Contains("PrivateAsync") &&
            (!RuntimeFeature.IsDynamicCodeSupported || (trace.Contains("PublicThrower.PrivateAsync(") && !trace.Contains("MoveNext"))), "stack trace of a private async method of a public type: " + trace);

        Check(PrivateThrower.GetEnvironmentStackTrace().Contains("PrivateThrower.GetEnvironmentStackTrace("), "Environment.StackTrace in a method of a private type");
        Check(PrivateThrower.GetStackTraceText().Contains("PrivateThrower.GetStackTraceText("), "StackTrace.ToString in a method of a private type");
        Check(PrivateStatic.GetCurrentFrameMethod() is null, "method of a stack frame of a private type");

        // A dynamic method owned by a type of the assembly is not a method of its metadata
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            System.Reflection.Emit.DynamicMethod dynamicMethod = new("DynamicThrower", typeof(void), Type.EmptyTypes, typeof(PublicThrower), skipVisibility: true);
            System.Reflection.Emit.ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldstr, "dynamic");
            il.Emit(System.Reflection.Emit.OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor([typeof(string)]));
            il.Emit(System.Reflection.Emit.OpCodes.Throw);
            Action thrower = (Action)dynamicMethod.CreateDelegate(typeof(Action));

            trace = CaptureStackTrace(thrower, out site);
            Check(trace is not null && trace.Contains("DynamicThrower"), "stack trace of a dynamic method owned by a type of the assembly: " + trace);
        }
    }

    public static int Run()
    {
        CheckMembers(typeof(PublicMembers), "public type");
        CheckMembers(typeof(PublicDerivedMembers), "type derived from a public type");
        CheckMemberHandles();
        UnsafeAccessors.Check();
        CheckStackTraces();

        // Public types are fully reflectable, but only over their public members.
        Type publicType = typeof(PublicClass);
        Check(publicType.Name == "PublicClass" && publicType.FullName == "PublicClass", "name of a public type");
        Check(publicType.GetFields(AllDeclared).Length == 1, "fields of a public type");
        Check(publicType.GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Static) is null, "private method of a public type");
        Check(publicType.GetMethod("PublicMethod").GetCustomAttributes(typeof(MarkerAttribute), inherit: false).Length == 1, "custom attribute on a method of a public type");
        Check(publicType.GetMethod("PrivateMarked").GetCustomAttributes(inherit: false).Length == 1, "custom attribute of an internal attribute class on a method of a public type");
        Check(publicType.GetMethod("PrivateMarked").CustomAttributes.Count() == 1, "custom attribute data of an internal attribute class on a method of a public type");
        Check(publicType.GetMethod("NestedMarked").GetCustomAttributes(inherit: false).Length == 0, "custom attribute of a private type on a method of a public type");
        Check(!publicType.GetMethod("NestedMarked").CustomAttributes.Any(), "custom attribute data of a private type on a method of a public type");
        Check(Activator.CreateInstance(publicType) is PublicClass, "Activator on a public type");

        // The attributes of a public type that mention private types are available, and the private types are surfaced as private types.
        // The attributes of a private type are not available.
        CheckAttributesWithPrivateArguments(typeof(PublicWithPrivateArguments), "public type");
        CheckAttributesWithPrivateGenericArguments(typeof(PublicWithPrivateGenericArguments), "public type, generic");
        Type genericDefinition = typeof(PublicWithPrivateGenericDefinition).GetCustomAttribute<TypeMarkerAttribute>().Type;
        CheckPlaceholder(genericDefinition, "private generic type definition of an attribute argument");
        CheckNoMembers(genericDefinition, "private generic type definition of an attribute argument");
        Check(!genericDefinition.IsVisible && genericDefinition == Hide(typeof(PrivateWithGenericNested.Box<>)), "private generic type definition of an attribute argument");
        Expect<NotSupportedException>(() => genericDefinition.MakeGenericType(typeof(int)), "MakeGenericType of a private generic type definition");
        Expect<NotSupportedException>(() => typeof(List<>).MakeGenericType(genericDefinition), "MakeGenericType over a private generic type definition");
        object[] onlyInAttributes = typeof(PublicWithPrivateGenericOnlyInAttributes).GetCustomAttributes(inherit: false);
        TypeMarkerAttribute onlyInAttributesMarker = onlyInAttributes.OfType<TypeMarkerAttribute>().Single();
        Type[] onlyInAttributesTypes = [onlyInAttributesMarker.Type, onlyInAttributesMarker.Other, onlyInAttributes.Single(a => a.GetType().IsGenericType).GetType().GetGenericArguments()[0]];
        Type[] onlyInAttributesArguments = [typeof(double), typeof(float), typeof(long)];
        Check(onlyInAttributes.Length == 2, "attributes that are the only mention of instances of a private generic type");
        for (int i = 0; i < onlyInAttributesTypes.Length; i++)
        {
            CheckPlaceholder(onlyInAttributesTypes[i], "private generic type that only attributes mention");
            CheckNoMembers(onlyInAttributesTypes[i], "private generic type that only attributes mention");
            Check(onlyInAttributesTypes[i].IsGenericType && onlyInAttributesTypes[i].GetGenericArguments()[0] == onlyInAttributesArguments[i] && onlyInAttributesTypes[i].GetGenericTypeDefinition() == genericDefinition,
                "generic arguments and generic type definition of a private generic type that only attributes mention");
            Check(onlyInAttributesTypes[i].HasSameMetadataDefinitionAs(onlyInAttributesTypes[i]) && onlyInAttributesTypes[i].HasSameMetadataDefinitionAs(genericDefinition) && genericDefinition.HasSameMetadataDefinitionAs(onlyInAttributesTypes[i]) &&
                !onlyInAttributesTypes[i].HasSameMetadataDefinitionAs(typeof(PrivateClass)), "HasSameMetadataDefinitionAs of a private generic type that only attributes mention");
        }

        Type privateWithAttributes = typeof(PrivateWithAttributes);
        Check(privateWithAttributes.GetCustomAttributes(inherit: true).Length == 0 && !privateWithAttributes.CustomAttributes.Any() &&
            !privateWithAttributes.IsDefined(typeof(MarkerAttribute), inherit: true) &&
            !privateWithAttributes.IsDefined(typeof(GenericMarkerAttribute<PrivateClass>), inherit: true) &&
            privateWithAttributes.GetCustomAttributes(typeof(TypeMarkerAttribute), inherit: true).Length == 0, "custom attributes of a private type");
        object comAttribute = typeof(PublicComClass).GetCustomAttribute(typeof(ComExposedClassAttribute<>));
        Check(comAttribute is IComExposedDetails && comAttribute.GetType().GetGenericArguments()[0] == typeof(PrivateExposedDetails) &&
            GetComInterfaceCount((IComExposedDetails)comAttribute) == 7, "attribute of the runtime libraries over a private type");

        object[] memberAttributes = typeof(PublicWithAttributedMember).GetMethod(nameof(PublicWithAttributedMember.Method)).GetCustomAttributes(inherit: false);
        TypeMarkerAttribute typeMarker = memberAttributes.OfType<TypeMarkerAttribute>().Single();
        Type[] mentionedTypes = [typeMarker.Type, typeMarker.Other, .. memberAttributes.Where(a => a.GetType().IsGenericType).Select(a => a.GetType().GetGenericArguments()[0])];
        Check(memberAttributes.Length == 2 && mentionedTypes.Length == 3 && mentionedTypes.Distinct().Count() == 3, "attributes of a member of a public type that mention a private type");
        foreach (Type mentionedType in mentionedTypes)
        {
            CheckPlaceholder(mentionedType, "private type that only attributes mention");
            CheckNoMembers(mentionedType, "private type that only attributes mention");
            Check(!mentionedType.IsVisible && !mentionedType.Name.Contains("PrivateOnlyIn"), "private type that only attributes mention");
        }

        // Nested types: the public ones of public types are reflectable, any other is not.
        Type[] nested = publicType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
        Check(nested.Length == 1 && nested[0] == typeof(PublicClass.PublicNested) && nested[0].FullName == "PublicClass+PublicNested", "nested types of a public type");
        Check(typeof(PublicOuter.PublicMiddle.PublicInner).FullName == "PublicOuter+PublicMiddle+PublicInner", "public type nested in public types");
        CheckPlaceholder(typeof(PublicClass.InternalNested), "internal type nested in a public type");
        CheckPlaceholder(PublicClass.PrivateNestedType, "private type nested in a public type");
        CheckPlaceholder(typeof(PublicOuter.InternalMiddle.PublicInner), "public type nested in an internal type");

        // The private types of the assembly are not reflectable.
        CheckPlaceholder(typeof(PrivateClass), "private class");
        CheckPlaceholder(typeof(PrivateStruct), "private struct");
        CheckPlaceholder(typeof(IPrivateInterface), "private interface");
        CheckPlaceholder(typeof(PrivateStatic), "private static class");
        CheckPlaceholder(typeof(PrivateWithNested.PublicNestedInPrivate), "public type nested in a private type");
        CheckNoMembers(typeof(PrivateClass), "private class");
        CheckNoMembers(typeof(PrivateStatic), "private static class");
        CheckNoMembers(typeof(PrivateStruct), "private struct");
        CheckNoMembers(typeof(PrivateWithNested.PublicNestedInPrivate), "public type nested in a private type");
        Check(Hide(typeof(PrivateClass)).Name != Hide(typeof(PrivateStruct)).Name, "placeholder names of different private types differ");
        Check(typeof(PrivateWithNested.PublicNestedInPrivate).Name != typeof(PrivateWithNested).Name, "placeholder names of a nested type");

        // Private types are not found by name and not listed.
        Assembly assembly = publicType.Assembly;
        Check(assembly.GetType("PublicClass") == publicType, "GetType by name finds a public type");
        Check(assembly.GetType("PrivateClass") is null, "GetType by name of a private type");
        Check(assembly.GetType("PrivateWithNested+PublicNestedInPrivate") is null, "GetType by name of a public type nested in a private type");
        Type[] allTypes = assembly.GetTypes();
        Check(allTypes.Contains(publicType) && !allTypes.Contains(typeof(PrivateClass)) && !allTypes.Contains(typeof(PrivateStatic)), "GetTypes");

        // The code of private types works, including instances, casts, GetType and static abstract members.
        PrivateStatic.Hit();
        UsesStaticAbstract.Call<PrivateRunner>();
        Check(PrivateStatic.Counter == 2, "static class and static abstract call");


        object privateInstance = Make(new PrivateClass());
        Check(privateInstance is IPrivateInterface, "interface cast of a private class");
        Check(((PrivateClass)privateInstance).Value == 42, "field of a private class");
        Check(privateInstance.GetHashCode() == 42, "virtual call on a private class");
        Check(privateInstance.GetType() == typeof(PrivateClass), "type identity");
        Check(privateInstance.GetType().BaseType == typeof(object), "base type of a private class");
        Check(privateInstance.GetType().IsClass && !privateInstance.GetType().IsValueType, "type flags of a private class");
        Check(Array.IndexOf(privateInstance.GetType().GetInterfaces(), typeof(IPrivateInterface)) >= 0 && privateInstance.GetType().GetInterface(nameof(IPrivateInterface)) is null, "interfaces of a private type");
        Check(privateInstance.ToString() == privateInstance.GetType().Name, "ToString of an instance of a private type");
        Check(typeof(PrivateDerived).BaseType == typeof(PrivateBase), "base type is a private type");
        Check(Make(new PrivateStruct { Value = 5 }) is PrivateStruct { Value: 5 } && typeof(PrivateStruct).IsValueType, "boxed private struct");
        Check(Make(new PrivateStructWithReference { Text = "a" }).Equals(new PrivateStructWithReference { Text = "a" }) &&
            !Make(new PrivateStructWithReference { Text = "a" }).Equals(new PrivateStructWithReference { Text = "b" }), "Equals of a private struct");
        Check(Make(new PrivateWithNested.PublicNestedInPrivate()) is PrivateWithNested.PublicNestedInPrivate { Value: 7 }, "type nested in a private type");
        Expect<Exception>(() => Activator.CreateInstance(Hide(typeof(PrivateClass))), "Activator on a private type");

        // A delegate to a method of a private type works, but its MethodInfo is not available.
        Action hit = PrivateStatic.Hit;
        hit();
        Expect<NotSupportedException>(() => _ = hit.Method, "Delegate.Method of a method of a private type");
        Action<int> genericHit = PrivateStatic.GenericHit<int>;
        Expect<NotSupportedException>(() => _ = genericHit.Method, "Delegate.Method of a generic method of a private type");
        Check(PrivateStatic.GetCurrentFrameMethod() is null, "method of a stack frame of a private type");

        // Private types can be used as generic arguments, also through the runtime type loader.
        Type made = Hide(typeof(PublicGeneric<>)).MakeGenericType(Hide(typeof(PrivateClass)));
        Check(made == typeof(PublicGeneric<PrivateClass>) && made.GetGenericArguments()[0] == typeof(PrivateClass), "MakeGenericType over a private type");
        Check(Make(new PrivateClass[1]).GetType().GetElementType() == typeof(PrivateClass), "array of a private type");
        Check(Make(new System.Collections.Generic.List<PrivateClass>()).GetType().GetGenericArguments()[0] == typeof(PrivateClass), "List of a private type");
        Check(Hide(typeof(PublicGeneric<>)).MakeGenericType(Hide(typeof(PrivateWithGenericNested.Box<int>))) == typeof(PublicGeneric<PrivateWithGenericNested.Box<int>>), "MakeGenericType over an instance of a private generic type");

        // The members that a type inherits through a type that is not reflectable are the ones of its base type that is.
        MethodInfo leafMethod = typeof(PrivateGenericLeaf<int>).GetMethod("Method");
        Check(leafMethod.GetBaseDefinition().DeclaringType == typeof(PublicVirtualBase) && leafMethod.IsDefined(typeof(MarkerAttribute), inherit: true) &&
            leafMethod.GetCustomAttributes(typeof(MarkerAttribute), inherit: true).Length == 1, "base definition and inherited attributes of a method that overrides a method of a private type");
        PropertyInfo[] leafProperties = typeof(PrivateGenericLeaf<int>).GetProperties();
        Check(leafProperties.Length == 1 && leafProperties[0].DeclaringType == typeof(PublicVirtualBase), "inherited property that a private type overrides");

        // The closure of a generic method of a private type is a generic type nested in it.
        Type closureType = PrivateWithClosure.GetClosureType(3);
        Check(closureType.IsGenericType, "closure of a generic method of a private type");
        CheckPlaceholder(closureType, "closure of a generic method of a private type");
        CheckNoMembers(closureType, "closure of a generic method of a private type");
        Check(closureType.GetGenericTypeDefinition().GetInterfaces().Length == 0, "generic type definition of the closure of a generic method of a private type");

        // The name of a generic type over a private type does not fail, and the assembly is still known to the runtime.
        Check(typeof(PublicGeneric<PrivateClass>).FullName is not null, "FullName of a generic type over a private type");
        Check(Assembly.GetExecutingAssembly() == assembly, "Assembly.GetExecutingAssembly");

        // A private type inherits the custom attributes of its public base type.
        Check(typeof(PrivateDerivedFromMarked).GetCustomAttributes(typeof(MarkerAttribute), inherit: true).Length == 1 &&
            typeof(PrivateDerivedFromMarked).IsDefined(typeof(MarkerAttribute), inherit: true), "inherited custom attribute of a private type");

        // Types that can't have their metadata removed stay reflectable: enums, delegates, generic types, types with generic virtual
        // methods, and the types that contain them.
        Check(typeof(PrivateEnum).Name == "PrivateEnum" && PrivateEnum.B.ToString() == "B" && Enum.GetNames(typeof(PrivateEnum)).Length == 2, "private enum");
        Check(typeof(PrivateCallback).Name == "PrivateCallback" && typeof(PrivateCallback).GetMethod("Invoke") is not null, "private delegate");
        Check(typeof(PrivateGeneric<>).Name == "PrivateGeneric`1" && typeof(PrivateGeneric<int>).GetField("Value") is null && typeof(PrivateGeneric<int>).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length == 0, "private generic type");
        Check(typeof(PrivateAttributeWithNestedGeneric).Name == "PrivateAttributeWithNestedGeneric" && typeof(PrivateAttributeWithNestedGeneric.Nested<int>).Name == "Nested`1", "generic type nested in a private attribute class");
        Check(typeof(TypeMarkerAttribute).GetMethod("Helper", BindingFlags.NonPublic | BindingFlags.Static) is null && typeof(TypeMarkerAttribute).GetProperty(nameof(TypeMarkerAttribute.Other)).GetSetMethod() is not null, "members of an attribute class");
        Check(typeof(PrivateWithNestedEnum).Name == "PrivateWithNestedEnum" && typeof(PrivateWithNestedEnum.Mode).Name == "Mode", "private type that contains an enum");
        Check(typeof(PrivateWithGenericVirtualMethod).Name == "PrivateWithGenericVirtualMethod" && new PrivateWithGenericVirtualMethod().Get(5) == 5, "private type with a generic virtual method");

        // IDynamicInterfaceCastable implementation types can be private.
        Check(((IDynamicTarget)Make(new DynamicCastable())).Get() == 42, "IDynamicInterfaceCastable implementation type that is private");

        CheckTypeProperties();

        return 100;
    }

    // Needs the type map of the assembly to be the one of the entry assembly (a TypeMapAssemblyTarget attribute on it, or the
    // TypeMappingEntryAssembly runtime configuration), so it is not part of Run.
    public static int RunTypeMap()
    {
        // A private type can be the key and the value of a type map entry.
        Check(TypeMapping.GetOrCreateExternalTypeMapping<PrivateReflectionUniverse>()["Private"] == typeof(PrivateClass), "private type as a type map value");

        return 100;
    }
}
