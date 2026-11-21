// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// The keys are looked up with string literals (which are frozen strings in the image) or with strings that are built at run time.
// Every key that is looked for in the image contains a '-': the names of the symbols of the string literals (that unstripped executables have) replace it, so they don't contain the UTF-8 text of the key.
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("reused-frozen-key-marker-0123456789", typeof(TypeMapFrozenKeys.Target1))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("plain-utf8-key-marker-0123456789", typeof(TypeMapFrozenKeys.Target2))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("ключ-ünïcode-キー-reused", typeof(TypeMapFrozenKeys.Target3))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("ключ-plain-ünïcode-キー", typeof(TypeMapFrozenKeys.Target4))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("emoji-key-😀-surrogate-pair-reused", typeof(TypeMapFrozenKeys.Target5))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("", typeof(TypeMapFrozenKeys.Target6))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("ü-キー", typeof(TypeMapFrozenKeys.Target7))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("seven-b", typeof(TypeMapFrozenKeys.Target8))]
[assembly: TypeMap<TypeMapFrozenKeys.Keys>("eight-by", typeof(TypeMapFrozenKeys.Target9))]

// The same key in another group, which is only used with TryGetValue.
[assembly: TypeMap<TypeMapFrozenKeys.OtherKeys>("reused-frozen-key-marker-0123456789", typeof(TypeMapFrozenKeys.Target10))]

// A group that only has the mapped types (no typed values).
[assembly: TypeMap<TypeMapFrozenKeys.TypesOnlyKeys>("types-only-frozen-key-marker-0123456789", typeof(TypeMapFrozenKeys.Target11))]

class TypeMapFrozenKeys
{
    public class Keys;
    public class OtherKeys;
    public class TypesOnlyKeys;

    public struct Number
    {
        public int Id;
    }

    public class Target1 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 1 }; }
    public class Target2 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 2 }; }
    public class Target3 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 3 }; }
    public class Target4 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 4 }; }
    public class Target5 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 5 }; }
    public class Target6 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 6 }; }
    public class Target7 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 7 }; }
    public class Target8 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 8 }; }
    public class Target9 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 9 }; }
    public class Target10 : ITypeMapValue<Number> { public static Number Value => new Number { Id = 10 }; }
    public class Target11;

    // A string that is not a literal of the program (concatenating the results of calls is not folded), so it is not in the frozen segment.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Hide(string s) => s;

    private static string NonLiteral(string first, string second) => string.Concat(Hide(first), Hide(second));

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    // Both readers give the same results for a key, whatever the way the key is stored in the image:
    // the one of TryGetValue and the one of the dictionary of the mapped types (it skips the descriptors of the typed values).
    private static void CheckKey(string key, int id, Type type)
    {
        Check(TypeMapping.TryGetValue<Keys, Number>(key, out Number number) && number.Id == id, $"value: {key}");
        Check(TypeMapping.GetOrCreateExternalTypeMapping<Keys>().TryGetValue(key, out Type found) && found == type, $"type: {key}");
    }

    public static int Run()
    {
        // The keys that are literals in the program (also the ones that are not stored in the image as UTF-8 strings).
        CheckKey("reused-frozen-key-marker-0123456789", 1, typeof(Target1));
        CheckKey("ключ-ünïcode-キー-reused", 3, typeof(Target3));
        CheckKey("emoji-key-😀-surrogate-pair-reused", 5, typeof(Target5));
        CheckKey("", 6, typeof(Target6));
        CheckKey("ü-キー", 7, typeof(Target7));
        CheckKey("seven-b", 8, typeof(Target8));
        CheckKey("eight-by", 9, typeof(Target9));

        // The same keys as strings that are not the literal: the characters are compared.
        CheckKey(NonLiteral("reused-frozen-key-", "marker-0123456789"), 1, typeof(Target1));
        CheckKey(NonLiteral("ключ-ünïcode-", "キー-reused"), 3, typeof(Target3));
        CheckKey(NonLiteral("emoji-key-😀-", "surrogate-pair-reused"), 5, typeof(Target5));
        CheckKey(NonLiteral("eight-", "by"), 9, typeof(Target9));

        // The keys that are not.
        string plain = NonLiteral("plain-utf8-key-", "marker-0123456789");
        string nonAsciiPlain = NonLiteral("ключ-plain-", "ünïcode-キー");
        CheckKey(plain, 2, typeof(Target2));
        CheckKey(nonAsciiPlain, 4, typeof(Target4));

        // A group with the mapped types only.
        Check(TypeMapping.GetOrCreateExternalTypeMapping<TypesOnlyKeys>().TryGetValue("types-only-frozen-key-marker-0123456789", out Type typesOnly) && typesOnly == typeof(Target11), "types only group");

        // The same string in another group, and keys that are not in the map. The lookup only compares with the entries of the bucket of the key
        // with the same low byte of the hash code, so the two literals that have the same bucket and low byte as keys that are references are the ones that
        // compare with the frozen string and fail.
        Check(TypeMapping.TryGetValue<OtherKeys, Number>("reused-frozen-key-marker-0123456789", out Number other) && other.Id == 10, "other group");
        Check(!TypeMapping.TryGetValue<OtherKeys, Number>(plain, out _), "other group: key of another group");
        Check(!TypeMapping.TryGetValue<Keys, Number>(NonLiteral("reused-frozen-key-marker-0123456789", "-other"), out _), "longer key");
        Check(!TypeMapping.TryGetValue<Keys, Number>("collide-126978", out _), "same bucket and low byte as an ASCII reference");
        Check(!TypeMapping.TryGetValue<Keys, Number>("collide-138649", out _), "same bucket and low byte as a non ASCII reference");
        Check(!TypeMapping.TryGetValue<Keys, Number>(NonLiteral("missing-", "key"), out _), "missing key");

        CheckImage(plain, nonAsciiPlain);

        return 100;
    }

    // A key that is already a string in the image is not stored again: the string only appears as the UTF-16 string, not as UTF-8 in the type map.
    // A key that isn't is stored as UTF-8 and is not added to the image as a string object.
    private static void CheckImage(string plain, string nonAsciiPlain)
    {
        // The executable is not the program on these.
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
        {
            return;
        }

        string? path = Environment.ProcessPath;
        if (path is null || !File.Exists(path))
        {
            Console.WriteLine("Skipping the check of the image: the executable is not available.");
            return;
        }

        byte[] image = File.ReadAllBytes(path);

        foreach (string key in new[] { "reused-frozen-key-marker-0123456789", "ключ-ünïcode-キー-reused", "emoji-key-😀-surrogate-pair-reused", "ü-キー", "eight-by", "types-only-frozen-key-marker-0123456789" })
        {
            Check(Count(image, Encoding.UTF8.GetBytes(key)) == 0, $"image: UTF-8 copy of {key}");
            Check(CountString(image, key) == 1, $"image: string object of {key}");
        }

        foreach (string key in new[] { plain, nonAsciiPlain, "seven-b" })
        {
            Check(Count(image, Encoding.UTF8.GetBytes(key)) == 1, $"image: UTF-8 copy of {key}");
        }

        foreach (string key in new[] { plain, nonAsciiPlain })
        {
            Check(CountString(image, key) == 0, $"image: string object of {key}");
        }
    }

    private static int Count(byte[] image, ReadOnlySpan<byte> value) => image.AsSpan().Count(value);

    // The zero bytes at the end of a string object are not looked for: with dehydration (the default except on Windows) they are part of a run of zeros that is not stored as is.
    private static int CountString(byte[] image, string value) => Count(image, MemoryMarshal.AsBytes(value.AsSpan()).Trim((byte)0));
}
