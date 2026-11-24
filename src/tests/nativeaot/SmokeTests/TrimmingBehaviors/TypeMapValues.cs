// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Group only used with TypeMapping.TryGetValue: the mapped types must not be reflection visible, and the getters must not be part of the program.
[assembly: TypeMap<TypeMapValues.ValuesOnly>("pre", typeof(TypeMapValues.PreinitTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("pre-struct", typeof(TypeMapValues.PreinitStructTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("fallback", typeof(TypeMapValues.FallbackTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("not-implemented", typeof(TypeMapValues.NotImplementedTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("multi", typeof(TypeMapValues.MultiTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("static-reader", typeof(TypeMapValues.StaticReaderTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("nint", typeof(TypeMapValues.NintTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("simple", typeof(TypeMapValues.SimpleValuesTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("reference", typeof(TypeMapValues.ReferenceValuesTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("generic", typeof(TypeMapValues.GenericTarget<string>))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("unmanaged", typeof(TypeMapValues.UnmanagedFunctionsTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("alias", typeof(TypeMapValues.AliasTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("pointer-alias", typeof(TypeMapValues.PointerAliasTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("trim-used", typeof(TypeMapValues.TrimUsedTarget), typeof(TypeMapValues.TrimKeyUsed))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("trim-unused", typeof(TypeMapValues.TrimUnusedTarget), typeof(TypeMapValues.TrimKeyUnused))]
[assembly: TypeMapAssociation<TypeMapValues.ValuesOnly>(typeof(TypeMapValues.ProxySource), typeof(TypeMapValues.ProxyTarget))]
[assembly: TypeMapAssociation<TypeMapValues.ValuesOnly>(typeof(TypeMapValues.ProxySourceNotImplemented), typeof(TypeMapValues.NotImplementedTarget))]
[assembly: TypeMapAssociation<TypeMapValues.ValuesOnly>(typeof(TypeMapValues.ProxySourceUnused), typeof(TypeMapValues.ProxyUnusedTarget))]

// Getters that the compiler evaluates, and getters that it has to leave to the compiled code.
[assembly: TypeMap<TypeMapValues.ValuesOnly>("marker-folded", typeof(TypeMapValues.FoldedMarkerTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("marker-compiled", typeof(TypeMapValues.CompiledMarkerTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("dataflow", typeof(TypeMapValues.DataflowTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("float", typeof(TypeMapValues.FloatTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("layout", typeof(TypeMapValues.UnsupportedLayoutTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("field-address", typeof(TypeMapValues.FieldAddressTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("nested-statics", typeof(TypeMapValues.NestedStaticsTarget))]
[assembly: TypeMap<TypeMapValues.ValuesOnly>("interface", typeof(TypeMapValues.IProvider))]

// Group used with both kinds of APIs: one hash table serves both.
[assembly: TypeMap<TypeMapValues.TypesAndValues>("all", typeof(TypeMapValues.AllTarget))]
[assembly: TypeMap<TypeMapValues.TypesAndValues>("all-trim-unused", typeof(TypeMapValues.AllTrimUnusedTarget), typeof(TypeMapValues.AllTrimKeyUnused))]
[assembly: TypeMapAssociation<TypeMapValues.TypesAndValues>(typeof(TypeMapValues.AllProxySource), typeof(TypeMapValues.AllTarget))]
[assembly: TypeMapAssociation<TypeMapValues.TypesAndValues>(typeof(TypeMapValues.AllProxySourceUnused), typeof(TypeMapValues.AllTrimUnusedTarget))]

// Group that uses the type returning API for the external map and TryGetValue for the proxy map: the directions are independent.
[assembly: TypeMap<TypeMapValues.MixedDirections>("mixed-ext", typeof(TypeMapValues.MixedExternalTarget))]
[assembly: TypeMapAssociation<TypeMapValues.MixedDirections>(typeof(TypeMapValues.MixedProxySource), typeof(TypeMapValues.MixedProxyTarget))]

// Entries that are declared on holder types: HolderA is linked from the assembly and chains HolderB (that links HolderA back),
// HolderLocal is only reachable as an owner.
[assembly: TypeMap<TypeMapValues.HolderGroup>("assembly", typeof(TypeMapValues.HolderAssemblyTarget))]
[assembly: TypeMapTypeTarget<TypeMapValues.HolderGroup>(typeof(TypeMapValues.HolderA))]
[assembly: TypeMapTypeTarget<TypeMapValues.HolderTypesGroup>(typeof(TypeMapValues.HolderA))]

unsafe class TypeMapValues
{
    public class ValuesOnly;
    public class TypesAndValues;
    public class MixedDirections;
    public class NoMapAtAll;

    public struct NestedValue
    {
        public int A;
        public string B;
    }

    public struct MixedValue
    {
        public delegate*<int, int, int> Add;
        public nint Raw;
        public string Name;
        public Type Type;
        public Guid Id;
        public NestedValue Nested;
        public byte Small;
    }

    public struct SmallValue
    {
        public int X;
    }

    public struct FunctionsValue
    {
        public delegate*<string> GetName;
        public delegate*<int> GetNumber;
    }

    // The type of the typeof in the getters (this one is intentionally reflection visible because the value has a Type).
    // The types in the values are only mentioned by the getters: they are reflection visible because of them, not because of the test code.
    public class VisibleViaTypeOf;
    public class GetterOnlyType;

    static int Add(int a, int b) => a + b;
    static int Multiply(int a, int b) => a * b;
    static string GetName() => "name";
    static int GetNumber() => 42;

    public class PreinitTarget : ITypeMapValue<MixedValue>
    {
        // Evaluated by the compiler: this does not run in the program.
        public static MixedValue Value
        {
            get
            {
                return new MixedValue
                {
                    Add = &TypeMapValues.Add,
                    Raw = (nint)(delegate*<int, int, int>)&TypeMapValues.Multiply,
                    Name = "preinit",
                    Type = typeof(GetterOnlyType),
                    Id = new Guid(0x11111111, 0x2222, 0x3333, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB),
                    Nested = new NestedValue { A = 7, B = "nested" },
                    Small = 5,
                };
            }
        }
    }

    public struct PreinitStructTarget : ITypeMapValue<MixedValue>
    {
        public static MixedValue Value => new MixedValue { Name = "struct" };
    }

    // The compiler can't evaluate this (it calls a method that is not implemented in managed code), so the getter runs in the program.
    public class FallbackTarget : ITypeMapValue<MixedValue>
    {
        public static int Calls;

        public static MixedValue Value
        {
            get
            {
                Calls++;
                return new MixedValue { Name = GC.GetTotalMemory(false) > 0 ? "fallback" : "no" };
            }
        }
    }

    // Does not provide any value.
    public class NotImplementedTarget;

    // A type implements the interface for each value type it provides.
    public class MultiTarget : ITypeMapValue<MixedValue>, ITypeMapValue<SmallValue>, ITypeMapValue<FunctionsValue>
    {
        static MixedValue ITypeMapValue<MixedValue>.Value => new MixedValue { Name = "multi" };
        static SmallValue ITypeMapValue<SmallValue>.Value => new SmallValue { X = 99 };
        static FunctionsValue ITypeMapValue<FunctionsValue>.Value => new FunctionsValue { GetName = &TypeMapValues.GetName, GetNumber = &TypeMapValues.GetNumber };
    }

    public struct NintValue
    {
        public nint Pointer;
        public int X;
    }

    public class NintTarget : ITypeMapValue<NintValue>
    {
        public static NintValue Value => new NintValue { Pointer = (nint)(delegate*<int, int, int>)&TypeMapValues.Add, X = 3 };
    }

    public enum SmallEnum : byte
    {
        A = 1,
        B = 2,
    }

    // Values that are not structs of unmanaged fields: primitives, enums, nullable, generic structs with references.
    public class SimpleValuesTarget : ITypeMapValue<int>, ITypeMapValue<SmallEnum>, ITypeMapValue<int?>, ITypeMapValue<KeyValuePair<string, string>>
    {
        static int ITypeMapValue<int>.Value => 42;
        static SmallEnum ITypeMapValue<SmallEnum>.Value => SmallEnum.B;
        static int? ITypeMapValue<int?>.Value => 7;
        static KeyValuePair<string, string> ITypeMapValue<KeyValuePair<string, string>>.Value => new KeyValuePair<string, string>("key", "value");
    }

    public class GenericTarget<T> : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 55 };
    }

    public struct UnmanagedFunctionsValue
    {
        public delegate* unmanaged<int, int, int> Add;
    }

    [UnmanagedCallersOnly]
    static int UnmanagedAdd(int a, int b) => a + b;

    public class UnmanagedFunctionsTarget : ITypeMapValue<UnmanagedFunctionsValue>
    {
        public static UnmanagedFunctionsValue Value => new UnmanagedFunctionsValue { Add = &UnmanagedAdd };
    }

    public struct AliasValue
    {
        public string Text;
        public int Number;
    }

    // The getter overwrites the struct through a reference while it also has a reference to one of its fields.
    public class AliasTarget : ITypeMapValue<AliasValue>
    {
        static void Reset(ref int number, ref AliasValue value)
        {
            value = default;
            number = 7;
        }

        public static AliasValue Value
        {
            get
            {
                AliasValue value = new AliasValue { Text = "text", Number = 1 };
                Reset(ref value.Number, ref value);
                return value;
            }
        }
    }

    public struct PointerAliasValue
    {
        public nint First;
        public nint Second;
    }

    // The getter stores a method pointer in a field while it has a reference to the field.
    public class PointerAliasTarget : ITypeMapValue<PointerAliasValue>
    {
        public static PointerAliasValue Value
        {
            get
            {
                PointerAliasValue value = default;
                ref nint first = ref value.First;
                value.First = (nint)(delegate*<int, int, int>)&TypeMapValues.Add;
                value.Second = first;
                return value;
            }
        }
    }

    public sealed class Counted;

    // The values are references, so the getters always run in the program, and several flavors of the same type need their own stubs.
    public class ReferenceValuesTarget : ITypeMapValue<string>, ITypeMapValue<Type>, ITypeMapValue<Counted>
    {
        public static int Calls;

        static string ITypeMapValue<string>.Value => "reference";
        static Type ITypeMapValue<Type>.Value => typeof(VisibleViaTypeOf);
        static Counted ITypeMapValue<Counted>.Value
        {
            get
            {
                Calls++;
                return new Counted();
            }
        }
    }

    // The value depends on a static that the program changes, so it can't be evaluated by the compiler.
    public class StaticReaderTarget : ITypeMapValue<SmallValue>
    {
        public static int Setting;

        public static SmallValue Value => new SmallValue { X = Setting };
    }

    // Not a constant for the compiler that compiles code (the call is not inlined), but the one that evaluates the getter can.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool NeverTrue()
    {
        int x = 3;
        x *= 7;
        return x == 22;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Hide(string s) => s;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float Hide(float f) => f;

    // The compiler evaluates the call and never takes the branch. If it compiles the getter instead, the call stays and so does the string.
    public class FoldedMarkerTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value
        {
            get
            {
                if (NeverTrue())
                    Console.WriteLine("typemap-values-folded-getter-marker");

                return new SmallValue { X = 31 };
            }
        }
    }

    public class CompiledMarkerTarget : ITypeMapValue<MixedValue>
    {
        public static MixedValue Value
        {
            get
            {
                if (NeverTrue())
                    Console.WriteLine("typemap-values-compiled-getter-marker");

                return new MixedValue { Name = GC.GetTotalMemory(false) > 0 ? "compiled" : "no" };
            }
        }
    }

    public sealed class Secret
    {
        public Secret() { }
    }

    public class Holder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>;

    public struct TypeValue
    {
        public Type Type;
    }

    // Mentioning the type makes the compiler analyze the code: the members that the annotation of the generic parameter asks for have to be kept.
    public class DataflowTarget : ITypeMapValue<TypeValue>
    {
        public static TypeValue Value => new TypeValue { Type = typeof(Holder<Secret>) };
    }

    public struct FloatValue
    {
        public float X;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float Absorb(float x) => (x + 1f) - x;

    // The result depends on the rounding to single precision.
    public class FloatTarget : ITypeMapValue<FloatValue>
    {
        public static FloatValue Value => new FloatValue { X = Absorb(16777216f) };
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct SizedInner
    {
        public nint Pointer;
    }

    public struct LayoutValue
    {
        public string First;
        public SizedInner Second;
    }

    // A struct with a function pointer and a layout that can't be written field by field.
    public class UnsupportedLayoutTarget : ITypeMapValue<LayoutValue>
    {
        public static LayoutValue Value => new LayoutValue { First = "layout", Second = new SizedInner { Pointer = (nint)(delegate*<int, int, int>)&TypeMapValues.Add } };
    }

    static void InitName(out string name) => name = "field-address";

    public class FieldAddressTarget : ITypeMapValue<MixedValue>
    {
        public static MixedValue Value
        {
            get
            {
                MixedValue value = default;
                InitName(out value.Name);
                return value;
            }
        }
    }

    public static class NestedStatics
    {
        public static readonly SmallValue Small = new SmallValue { X = 123 };
    }

    public class NestedStaticsTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => NestedStatics.Small;
    }

    // Mapped interfaces don't provide values.
    public interface IProvider : ITypeMapValue<SmallValue>
    {
        static SmallValue ITypeMapValue<SmallValue>.Value => new SmallValue { X = 8 };
    }

    public class TrimKeyUsed;
    public class TrimKeyUnused;

    public class TrimUsedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 1 };
    }

    public class TrimUnusedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 2 };
    }

    public class ProxySource;
    public class ProxySourceNotImplemented;
    public class ProxySourceUnused;

    public class ProxyTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 3 };
    }

    public class ProxyUnusedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 4 };
    }

    public class AllTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 10 };
    }

    public class AllTrimKeyUnused;
    public class AllTrimUnusedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 11 };
    }

    public class AllProxySource;
    public class AllProxySourceUnused;

    public class MixedExternalTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 20 };
    }

    public class MixedProxySource;
    public class MixedProxyTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 21 };
    }

    public class HolderGroup;
    public class HolderTypesGroup;

    public class HolderAssemblyTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 1 };
    }

    // Evaluated by the compiler, so the marker is not in the program.
    public class HolderTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value
        {
            get
            {
                if (NeverTrue())
                    Console.WriteLine("typemap-values-holder-folded-getter-marker");

                return new SmallValue { X = 2 };
            }
        }
    }

    public class HolderTrimKeyUnused;

    public class HolderTrimUnusedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 3 };
    }

    public class HolderBTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 4 };
    }

    public class HolderLocalTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 5 };
    }

    public class HolderTypesTarget;
    public class HolderSource;
    public class HolderLocalSource;

    public class HolderChainedTarget : ITypeMapValue<SmallValue>
    {
        public static SmallValue Value => new SmallValue { X = 6 };
    }

    // Linked from the assembly. The entries of the group on the type, and the type that it links to.
    [TypeMap<HolderGroup>("a", typeof(HolderTarget))]
    [TypeMap<HolderGroup>("a-trim-unused", typeof(HolderTrimUnusedTarget), typeof(HolderTrimKeyUnused))]
    [TypeMapAssociation<HolderGroup>(typeof(HolderSource), typeof(HolderTarget))]
    [TypeMapTypeTarget<HolderGroup>(typeof(HolderB))]
    [TypeMap<HolderTypesGroup>("t", typeof(HolderTypesTarget))]
    // This entry is not part of the map of ValuesOnly: the type is not linked for that group.
    [TypeMap<ValuesOnly>("holder-other-group", typeof(HolderTarget))]
    public class HolderA;

    // Links the type that links it, and a type that is never used as an owner.
    [TypeMap<HolderGroup>("b", typeof(HolderBTarget))]
    [TypeMapTypeTarget<HolderGroup>(typeof(HolderA))]
    [TypeMapTypeTarget<HolderGroup>(typeof(HolderChained))]
    public class HolderB;

    [TypeMap<HolderGroup>("chained", typeof(HolderChainedTarget))]
    public class HolderChained;

    // Not linked from the assembly: only found by starting at it.
    [TypeMap<HolderGroup>("local", typeof(HolderLocalTarget))]
    [TypeMapAssociation<HolderGroup>(typeof(HolderLocalSource), typeof(HolderLocalTarget))]
    public class HolderLocal;

    public class HolderEmpty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(object o) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type Hide(Type t) => t;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "That's the point")]
    private static Type GetTypeSecretly(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static bool ThrowsArgumentNull(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentNullException)
        {
            return true;
        }

        return false;
    }

    public static int Run()
    {
        // Keys of the proxy maps need to be constructed to have entries.
        Consume(new ProxySource());
        Consume(new ProxySourceNotImplemented());
        Consume(new AllProxySource());
        Consume(new MixedProxySource());
        Consume(new HolderSource());
        Consume(new HolderLocalSource());
        // Trim target of an external entry that is used.
        Consume(new TrimKeyUsed());

        RunValuesOnly();
        RunMultipleFlavors();
        RunSimpleValues();
        RunReferenceValues();
        RunTypesAndValues();
        RunMixedDirections();
        RunHolders();
        RunEvaluation();
        RunNoMap();

        return 100;
    }

    static void RunHolders()
    {
        // The map of the group has the entries of the assembly and of the holder types that it links (transitively, also in a cycle). The type of the other group is not linked.
        Check(TypeMapping.TryGetValue<HolderGroup, SmallValue>("assembly", out SmallValue value) && value.X == 1, "holders: assembly entry");
        Check(TypeMapping.TryGetValue<HolderGroup, SmallValue>("a", out value) && value.X == 2, "holders: entry of the linked type");
        Check(TypeMapping.TryGetValue<HolderGroup, SmallValue>("b", out value) && value.X == 4, "holders: entry of the chained type");
        Check(TypeMapping.TryGetValue<HolderGroup, SmallValue>("chained", out value) && value.X == 6, "holders: entry of the type that is chained twice");
        Check(TypeMapping.TryGetValue<HolderGroup, SmallValue>(Hide(typeof(HolderSource)), out value) && value.X == 2, "holders: proxy entry");
        Check(!TypeMapping.TryGetValue<HolderGroup, SmallValue>("local", out _), "holders: the type that is not linked");
        Check(!TypeMapping.TryGetValue<HolderGroup, SmallValue>(Hide(typeof(HolderLocalSource)), out _), "holders: proxy entry of the type that is not linked");
        Check(!TypeMapping.TryGetValue<HolderGroup, SmallValue>("a-trim-unused", out _), "holders: unused trim target");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>("holder-other-group", out _), "holders: entry of another group");

        // The entries of the holder are also in the map of the type returning API.
        Check(TypeMapping.GetOrCreateExternalTypeMapping<HolderTypesGroup>().TryGetValue("t", out Type type) && type.Name == nameof(HolderTypesTarget), "holders: type");

        // The map that starts at an owner type has the entries of the type and of the types that it links, and nothing else.
        Check(TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("a", out value) && value.X == 2, "owner: own entry");
        Check(TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("b", out value) && value.X == 4, "owner: linked entry");
        Check(TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("chained", out value) && value.X == 6, "owner: entry of a type that is linked twice");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("assembly", out _), "owner: assembly entry");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("local", out _), "owner: other holder");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>("a-trim-unused", out _), "owner: unused trim target");
        Check(TypeMapping.TryGetValue<HolderGroup, HolderB, SmallValue>("a", out value) && value.X == 2, "owner: entry through the cycle");
        Check(TypeMapping.TryGetValue<HolderGroup, HolderLocal, SmallValue>("local", out value) && value.X == 5, "owner: type that is not linked");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderLocal, SmallValue>("a", out _), "owner: entry of the assembly holder");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderEmpty, SmallValue>("a", out _), "owner: type without entries");
        Check(TypeMapping.TryGetValue<ValuesOnly, HolderA, SmallValue>("holder-other-group", out value) && value.X == 2, "owner: entry of the type for another group");

        Check(TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>(Hide(typeof(HolderSource)), out value) && value.X == 2, "owner: proxy entry");
        Check(TypeMapping.TryGetValue<HolderGroup, HolderLocal, SmallValue>(Hide(typeof(HolderLocalSource)), out value) && value.X == 5, "owner: proxy entry of the type that is not linked");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>(Hide(typeof(HolderLocalSource)), out _), "owner: proxy entry of another holder");
        Check(!TypeMapping.TryGetValue<HolderGroup, HolderLocal, SmallValue>(Hide(typeof(HolderSource)), out _), "owner: proxy entry of the assembly holder");

        Check(ThrowsArgumentNull(() => TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>((string)null, out _)), "owner: null string key");
        Check(ThrowsArgumentNull(() => TypeMapping.TryGetValue<HolderGroup, HolderA, SmallValue>((Type)null, out _)), "owner: null type key");

        // The holder types that are only linked are containers of attributes: they are not reflection visible, nor are the mapped types.
        // The types that are used as owners are, because the lookup identifies its table with a type that is made of them.
        string visible = "";
        foreach (string name in new[] { nameof(HolderChained), nameof(HolderTarget), nameof(HolderBTarget), nameof(HolderLocalTarget), nameof(HolderChainedTarget) })
        {
            if (GetTypeSecretly(Hide(typeof(TypeMapValues)), name) != null)
                visible += name + " ";
        }

        Check(visible.Length == 0, "reflection visible: " + visible);
    }

    static void RunValuesOnly()
    {
        // (a) The value is evaluated at compile time: function pointers, strings, types, guids, nested structs, nint.
        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("pre", out MixedValue value), "pre: found");
        Check(value.Add(2, 3) == 5, "pre: function pointer");
        Check(((delegate*<int, int, int>)value.Raw)(2, 3) == 6, "pre: function pointer as nint");
        Check(value.Name == "preinit", "pre: string");
        Check(value.Type.Name == nameof(GetterOnlyType), "pre: type");
        Check(value.Id == new Guid(0x11111111, 0x2222, 0x3333, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB), "pre: guid");
        Check(value.Nested.A == 7 && value.Nested.B == "nested", "pre: nested struct");
        Check(value.Small == 5, "pre: byte");

        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("pre-struct", out MixedValue structValue), "pre: struct target found");
        Check(structValue.Name == "struct" && structValue.Add == null && structValue.Type == null, "pre: struct target");

        // Values of a proxy map, with Type keys.
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>(Hide(typeof(ProxySource)), out SmallValue proxyValue) && proxyValue.X == 3, "proxy value");

        // (b) The compiler can't evaluate this getter, so the program does it.
        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("fallback", out MixedValue fallbackValue) && fallbackValue.Name == "fallback", "fallback value");
        Check(FallbackTarget.Calls > 0, "fallback: the getter runs in the program");

        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("generic", out SmallValue genericValue) && genericValue.X == 55, "generic mapped type");

        Check(TypeMapping.TryGetValue<ValuesOnly, UnmanagedFunctionsValue>("unmanaged", out UnmanagedFunctionsValue unmanaged) && unmanaged.Add(4, 5) == 9, "unmanaged function pointer");

        Check(TypeMapping.TryGetValue<ValuesOnly, AliasValue>("alias", out AliasValue alias) && alias.Text is null && alias.Number == 7, "overwritten struct with a reference to its field");

        Check(TypeMapping.TryGetValue<ValuesOnly, PointerAliasValue>("pointer-alias", out PointerAliasValue pointerAlias) && pointerAlias.First != 0 && pointerAlias.Second == pointerAlias.First, "method pointer stored while a reference to the field is live");

        // A function pointer in an nint field.
        Check(TypeMapping.TryGetValue<ValuesOnly, NintValue>("nint", out NintValue nintValue), "nint found");
        Check(nintValue.X == 3 && ((delegate*<int, int, int>)nintValue.Pointer)(4, 5) == 9, "nint field");

        // The getter of a type that reads a static that is written by the program runs in the program.
        StaticReaderTarget.Setting = 77;
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("static-reader", out SmallValue staticReaderValue) && staticReaderValue.X == 77, "getter that reads a mutable static");

        // (c) Missing keys, keys whose condition is not met, and mapped types that don't provide the value.
        Check(!TypeMapping.TryGetValue<ValuesOnly, MixedValue>("missing", out _), "missing key");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>("trim-unused", out _), "unused trim target");
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("trim-used", out SmallValue trimUsed) && trimUsed.X == 1, "used trim target");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>(Hide(typeof(ProxySourceUnused)), out _), "unused proxy key");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>(Hide(typeof(string)), out _), "missing proxy key");
        Check(!TypeMapping.TryGetValue<ValuesOnly, MixedValue>("not-implemented", out _), "Try not implemented");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>(Hide(typeof(ProxySourceNotImplemented)), out _), "Try not implemented proxy");

        Check(ThrowsArgumentNull(() => TypeMapping.TryGetValue<ValuesOnly, SmallValue>((string)null, out _)), "null string key");
        Check(ThrowsArgumentNull(() => TypeMapping.TryGetValue<ValuesOnly, SmallValue>((Type)null, out _)), "null type key");

        // The mapped types are not reflection visible, only what the getters reference is.
        foreach (string name in new[] { nameof(PreinitTarget), nameof(PreinitStructTarget), nameof(NotImplementedTarget),
                                        nameof(MultiTarget), nameof(TrimUsedTarget), nameof(ProxyTarget), nameof(SimpleValuesTarget),
                                        nameof(ReferenceValuesTarget), "GenericTarget`1", nameof(UnmanagedFunctionsTarget), nameof(AliasTarget), nameof(PointerAliasTarget) })
        {
            Check(GetTypeSecretly(Hide(typeof(TypeMapValues)), name) == null, name + " is reflection visible");
        }

        Check(GetTypeSecretly(Hide(typeof(TypeMapValues)), nameof(GetterOnlyType)) != null, "the type in the value is reflection visible");
    }

    static void RunMultipleFlavors()
    {
        // (d) A type provides the values of several flavors, and the flavors of a group are independent.
        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("multi", out MixedValue first) && first.Name == "multi", "multi: first flavor");
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("multi", out SmallValue second) && second.X == 99, "multi: second flavor");
        Check(TypeMapping.TryGetValue<ValuesOnly, FunctionsValue>("multi", out FunctionsValue functions), "multi: third flavor");
        Check(functions.GetName() == "name" && functions.GetNumber() == 42, "multi: function pointers");

        // A type that only provides one of them
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>("pre", out _), "multi: other flavor not provided");
        Check(!TypeMapping.TryGetValue<ValuesOnly, FunctionsValue>("pre", out _), "multi: third flavor not provided");
    }

    static void RunTypesAndValues()
    {
        // (e) The group is used with both kinds of APIs: the entries have the types and the values.
        var externalMap = TypeMapping.GetOrCreateExternalTypeMapping<TypesAndValues>();
        Check(externalMap.TryGetValue("all", out Type external) && external.Name == nameof(AllTarget), "all: type");
        Check(GetTypeSecretly(Hide(typeof(TypeMapValues)), nameof(AllTarget)) != null, "all: the target is reflection visible");
        Check(TypeMapping.TryGetValue<TypesAndValues, SmallValue>("all", out SmallValue allValue) && allValue.X == 10, "all: value");

        var proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<TypesAndValues>();
        Check(proxyMap.TryGetValue(Hide(typeof(AllProxySource)), out Type proxy) && proxy.Name == nameof(AllTarget), "all: proxy type");
        Check(TypeMapping.TryGetValue<TypesAndValues, SmallValue>(Hide(typeof(AllProxySource)), out SmallValue allProxyValue) && allProxyValue.X == 10, "all: proxy value");

        // The entries are present under the same conditions for both.
        Check(!externalMap.TryGetValue("all-trim-unused", out _), "all: unused trim target type");
        Check(!TypeMapping.TryGetValue<TypesAndValues, SmallValue>("all-trim-unused", out _), "all: unused trim target value");
        Check(!proxyMap.TryGetValue(Hide(typeof(AllProxySourceUnused)), out _), "all: unused proxy key type");
        Check(!TypeMapping.TryGetValue<TypesAndValues, SmallValue>(Hide(typeof(AllProxySourceUnused)), out _), "all: unused proxy key value");
    }

    static void RunMixedDirections()
    {
        // (e2) The external map is used with the type returning API, the proxy map only with TryGetValue.
        Check(TypeMapping.GetOrCreateExternalTypeMapping<MixedDirections>().TryGetValue("mixed-ext", out Type external) && external.Name == nameof(MixedExternalTarget), "mixed: external type");
        Check(GetTypeSecretly(Hide(typeof(TypeMapValues)), nameof(MixedExternalTarget)) != null, "mixed: the external target is reflection visible");

        Check(TypeMapping.TryGetValue<MixedDirections, SmallValue>(Hide(typeof(MixedProxySource)), out SmallValue proxyValue) && proxyValue.X == 21, "mixed: proxy value");
        Check(GetTypeSecretly(Hide(typeof(TypeMapValues)), nameof(MixedProxyTarget)) == null, "mixed: the proxy target is reflection visible");
    }

    static void RunEvaluation()
    {
        // (h) The getters that the compiler has to leave to the compiled code still do what the code says.
        Check(TypeMapping.TryGetValue<ValuesOnly, TypeValue>("dataflow", out TypeValue holder) && holder.Type.Name == "Holder`1", "dataflow: type");
        Check(Activator.CreateInstance(holder.Type.GenericTypeArguments[0]) is Secret, "dataflow: the annotation of the generic parameter keeps the constructor");

        Check(TypeMapping.TryGetValue<ValuesOnly, FloatValue>("float", out FloatValue floatValue) && floatValue.X == Absorb(Hide(16777216f)), "float: single precision arithmetic");

        Check(TypeMapping.TryGetValue<ValuesOnly, LayoutValue>("layout", out LayoutValue layout), "layout: found");
        Check(layout.First == "layout" && ((delegate*<int, int, int>)layout.Second.Pointer)(2, 3) == 5, "layout: values");

        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("field-address", out MixedValue fieldAddress) && fieldAddress.Name == "field-address", "field address: reference field");
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("nested-statics", out SmallValue nested) && nested.X == 123, "nested statics");
        Check(!TypeMapping.TryGetValue<ValuesOnly, SmallValue>("interface", out _), "mapped interface");

        // (i) The getter that the compiler evaluates is not in the program, the one that it can't evaluate is. The markers are in the getters, after a branch that is never taken.
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallValue>("marker-folded", out SmallValue folded) && folded.X == 31, "marker: folded value");
        Check(TypeMapping.TryGetValue<ValuesOnly, MixedValue>("marker-compiled", out MixedValue compiled) && compiled.Name == "compiled", "marker: compiled value");
        CheckImage();
    }

    static void CheckImage()
    {
        // The executable is not the program on these.
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
        {
            return;
        }

        string path = Environment.ProcessPath;
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("Skipping the check of the image: the executable is not available.");
            return;
        }

        byte[] image = File.ReadAllBytes(path);

        // The markers are built here: only the getters have them as a whole.
        // The zero bytes at the end of a string object are not looked for: with dehydration (the default except on Windows) they are part of a run of zeros that is not stored as is.
        int Count(string marker) => image.AsSpan().Count(MemoryMarshal.AsBytes(marker.AsSpan()).Trim((byte)0));

        Check(Count(Hide("typemap-values-compiled-") + Hide("getter-marker")) > 0, "image: the getter that is not evaluated is compiled");
        Check(Count(Hide("typemap-values-folded-") + Hide("getter-marker")) == 0, "image: the getter that is evaluated is not compiled");
        Check(Count(Hide("typemap-values-holder-folded-") + Hide("getter-marker")) == 0, "image: the getter of the entry of a holder is not compiled");
    }

    static void RunSimpleValues()
    {
        // (f) Values that are not structs of unmanaged data.
        Check(TypeMapping.TryGetValue<ValuesOnly, int>("simple", out int number) && number == 42, "int");
        Check(TypeMapping.TryGetValue<ValuesOnly, SmallEnum>("simple", out SmallEnum smallEnum) && smallEnum == SmallEnum.B, "enum");
        Check(TypeMapping.TryGetValue<ValuesOnly, int?>("simple", out int? nullable) && nullable == 7, "nullable");
        Check(TypeMapping.TryGetValue<ValuesOnly, KeyValuePair<string, string>>("simple", out KeyValuePair<string, string> pair)
            && pair.Key == "key" && pair.Value == "value", "generic struct with references");
        Check(!TypeMapping.TryGetValue<ValuesOnly, long>("simple", out _), "value type that is not provided");
    }

    static void RunReferenceValues()
    {
        // (g) The values are references: the program calls the getters, and a type can provide several of them.
        Check(TypeMapping.TryGetValue<ValuesOnly, string>("reference", out string text) && text == "reference", "reference: string");
        Check(TypeMapping.TryGetValue<ValuesOnly, Type>("reference", out Type type) && type.Name == nameof(VisibleViaTypeOf), "reference: type");

        Check(TypeMapping.TryGetValue<ValuesOnly, Counted>("reference", out Counted counted) && counted is not null, "reference: object");
        Check(ReferenceValuesTarget.Calls > 0, "reference: the getter runs in the program");

        Check(!TypeMapping.TryGetValue<ValuesOnly, string>("pre", out string missing) && missing is null, "reference: not provided");
        Check(!TypeMapping.TryGetValue<ValuesOnly, string>("missing", out _), "reference: missing key");
    }

    static void RunNoMap()
    {
        // A group without any entries behaves as an empty map.
        Check(!TypeMapping.TryGetValue<NoMapAtAll, SmallValue>("anything", out _), "no map, external");
        Check(!TypeMapping.TryGetValue<NoMapAtAll, SmallValue>(Hide(typeof(string)), out _), "no map, proxy");
    }
}
