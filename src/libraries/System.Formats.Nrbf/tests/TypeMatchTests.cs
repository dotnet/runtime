using System.Collections.Generic;
using System.Linq;
using System.Formats.Nrbf.Utils;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class TypeMatchTests : ReadTests
{
    private readonly static HashSet<Type> PrimitiveTypes = new()
    {
        typeof(bool), typeof(char), typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(IntPtr), typeof(UIntPtr),
        typeof(float), typeof(double), typeof(decimal), typeof(DateTime),
        typeof(TimeSpan)
    };

    [Serializable]
    public class NonSystemClass
    {
    }

    [Serializable]
    public class GenericNonSystemClass<T>
    {
    }

    [Fact]
    public void CanRecognizeAllSupportedPrimitiveTypes()
    {
        Verify(true);
        Verify('c');
        Verify(byte.MaxValue);
        Verify(sbyte.MaxValue);
        Verify(short.MaxValue);
        Verify(ushort.MaxValue);
        Verify(int.MaxValue);
        Verify(uint.MaxValue);
#if !NETFRAMEWORK
        Verify(nint.MaxValue);
        Verify(nuint.MaxValue);
#endif
        Verify(long.MaxValue);
        Verify(ulong.MaxValue);
        Verify(float.MaxValue);
        Verify(double.MaxValue);
        Verify(decimal.MaxValue);
        Verify(TimeSpan.MaxValue);
        Verify(DateTime.Now);
        Verify("string");
        Verify(new object());
    }

    [Fact]
    public void CanRecognizeSystemTypes()
    {
        Verify(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeNonSystemTypes()
    {
        Verify(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeGenericSystemTypes()
    {
        Verify(new List<bool>());
        Verify(new List<List<int>>());
        Verify(new Dictionary<string, bool>());
        Verify(new Dictionary<string, List<ValueTuple<int, short>>>());
    }

    [Fact]
    public void TakesGenericTypeDefinitionIntoAccount()
    {
        List<int> input = new List<int>();

        SerializationRecord one = NrbfDecoder.Decode(Serialize(input));

        // The generic arguments match, the generic type definition does not.
        Assert.False(one.TypeNameMatches(typeof(Stack<int>)));
        Assert.True(one.TypeNameMatches(typeof(List<int>)));
    }

    [Fact]
    public void CanRecognizeGenericNonSystemTypes()
    {
        Verify(new GenericNonSystemClass<NonSystemClass>());
        Verify(new GenericNonSystemClass<GenericNonSystemClass<NonSystemClass>>());
    }

    [Fact]
    public void CanRecognizeSZArraysOfAllSupportedPrimitiveTypes()
    {
        VerifySZArray(true);
        VerifySZArray('c');
        VerifySZArray(byte.MaxValue);
        VerifySZArray(sbyte.MaxValue);
        VerifySZArray(short.MaxValue);
        VerifySZArray(ushort.MaxValue);
        VerifySZArray(int.MaxValue);
        VerifySZArray(uint.MaxValue);
        VerifySZArray(long.MaxValue);
        VerifySZArray(ulong.MaxValue);
        VerifySZArray(float.MaxValue);
        VerifySZArray(double.MaxValue);
        VerifySZArray(decimal.MaxValue);
        VerifySZArray(TimeSpan.MaxValue);
        VerifySZArray(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeSZArraysOfSystemTypes()
    {
        VerifySZArray(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeSZArraysOfNonSystemTypes()
    {
        VerifySZArray(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeSZArraysOfGenericSystemTypes()
    {
        VerifySZArray(new List<bool>());
        VerifySZArray(new List<List<int>>());
        VerifySZArray(new Dictionary<string, bool>());
        VerifySZArray(new Dictionary<string, List<ValueTuple<int, short>>>());
    }

    [Fact]
    public void CanRecognizeSZArraysOfGenericNonSystemTypes()
    {
        VerifySZArray(new GenericNonSystemClass<NonSystemClass>());
        VerifySZArray(new GenericNonSystemClass<GenericNonSystemClass<NonSystemClass>>());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyJaggedArray(true);
        VerifyJaggedArray('c');
        VerifyJaggedArray(byte.MaxValue);
        VerifyJaggedArray(sbyte.MaxValue);
        VerifyJaggedArray(short.MaxValue);
        VerifyJaggedArray(ushort.MaxValue);
        VerifyJaggedArray(int.MaxValue);
        VerifyJaggedArray(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyJaggedArray(nint.MaxValue);
        VerifyJaggedArray(nuint.MaxValue);
#endif
        VerifyJaggedArray(long.MaxValue);
        VerifyJaggedArray(ulong.MaxValue);
        VerifyJaggedArray(float.MaxValue);
        VerifyJaggedArray(double.MaxValue);
        VerifyJaggedArray(decimal.MaxValue);
        VerifyJaggedArray(TimeSpan.MaxValue);
        VerifyJaggedArray(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfSystemTypes()
    {
        VerifyJaggedArray(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfNonSystemTypes()
    {
        VerifyJaggedArray(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfGenericSystemTypes()
    {
        VerifyJaggedArray(new List<bool>());
        VerifyJaggedArray(new List<List<int>>());
        VerifyJaggedArray(new Dictionary<string, bool>());
        VerifyJaggedArray(new Dictionary<string, List<ValueTuple<int, short>>>());
    }

    [Fact]
    public void CanRecognizeJaggedArraysOfGenericNonSystemTypes()
    {
        VerifyJaggedArray(new GenericNonSystemClass<NonSystemClass>());
        VerifyJaggedArray(new GenericNonSystemClass<GenericNonSystemClass<NonSystemClass>>());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyRectangularArray_2D(true);
        VerifyRectangularArray_2D('c');
        VerifyRectangularArray_2D(byte.MaxValue);
        VerifyRectangularArray_2D(sbyte.MaxValue);
        VerifyRectangularArray_2D(short.MaxValue);
        VerifyRectangularArray_2D(ushort.MaxValue);
        VerifyRectangularArray_2D(int.MaxValue);
        VerifyRectangularArray_2D(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyRectangularArray_2D(nint.MaxValue);
        VerifyRectangularArray_2D(nuint.MaxValue);
#endif
        VerifyRectangularArray_2D(long.MaxValue);
        VerifyRectangularArray_2D(ulong.MaxValue);
        VerifyRectangularArray_2D(float.MaxValue);
        VerifyRectangularArray_2D(double.MaxValue);
        VerifyRectangularArray_2D(decimal.MaxValue);
        VerifyRectangularArray_2D(TimeSpan.MaxValue);
        VerifyRectangularArray_2D(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfSystemTypes()
    {
        VerifyRectangularArray_2D(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysNonOfSystemTypes()
    {
        VerifyRectangularArray_2D(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfGenericSystemTypes()
    {
        VerifyRectangularArray_2D(new List<bool>());
        VerifyRectangularArray_2D(new List<List<int>>());
        VerifyRectangularArray_2D(new Dictionary<string, bool>());
        VerifyRectangularArray_2D(new Dictionary<string, List<ValueTuple<int, short>>>());
    }

    [Fact]
    public void CanRecognizeRectangular2DArraysOfGenericNonSystemTypes()
    {
        VerifyRectangularArray_2D(new GenericNonSystemClass<NonSystemClass>());
        VerifyRectangularArray_2D(new GenericNonSystemClass<GenericNonSystemClass<NonSystemClass>>());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfAllSupportedPrimitiveTypes()
    {
        VerifyRectangularArray_5D(true);
        VerifyRectangularArray_5D('c');
        VerifyRectangularArray_5D(byte.MaxValue);
        VerifyRectangularArray_5D(sbyte.MaxValue);
        VerifyRectangularArray_5D(short.MaxValue);
        VerifyRectangularArray_5D(ushort.MaxValue);
        VerifyRectangularArray_5D(int.MaxValue);
        VerifyRectangularArray_5D(uint.MaxValue);
#if !NETFRAMEWORK
        VerifyRectangularArray_5D(nint.MaxValue);
        VerifyRectangularArray_5D(nuint.MaxValue);
#endif
        VerifyRectangularArray_5D(long.MaxValue);
        VerifyRectangularArray_5D(ulong.MaxValue);
        VerifyRectangularArray_5D(float.MaxValue);
        VerifyRectangularArray_5D(double.MaxValue);
        VerifyRectangularArray_5D(decimal.MaxValue);
        VerifyRectangularArray_5D(TimeSpan.MaxValue);
        VerifyRectangularArray_5D(DateTime.Now);
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfSystemTypes()
    {
        VerifyRectangularArray_5D(new NotSupportedException());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfNonSystemTypes()
    {
        VerifyRectangularArray_5D(new NonSystemClass());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfGenericSystemTypes()
    {
        VerifyRectangularArray_5D(new List<bool>());
        VerifyRectangularArray_5D(new List<List<int>>());
        VerifyRectangularArray_5D(new Dictionary<string, bool>());
        VerifyRectangularArray_5D(new Dictionary<string, List<ValueTuple<int, short>>>());
    }

    [Fact]
    public void CanRecognizeRectangular5DArraysOfGenericNonSystemTypes()
    {
        VerifyRectangularArray_5D(new GenericNonSystemClass<NonSystemClass>());
        VerifyRectangularArray_5D(new GenericNonSystemClass<GenericNonSystemClass<NonSystemClass>>());
    }

    private static void Verify<T>(T input) where T : notnull
    {
        SerializationRecord one = NrbfDecoder.Decode(Serialize(input));

        Assert.True(one.TypeNameMatches(typeof(T)));

        foreach (Type type in PrimitiveTypes)
        {
            Assert.Equal(typeof(T) == type, one.TypeNameMatches(type));
        }
    }

    private static void VerifySZArray<T>(T input) where T : notnull
    {
        T[] array = [input];

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(array));

        Assert.Equal(typeof(T[]).GetTypeFullNameIncludingTypeForwards(), arrayRecord.TypeName.FullName);

        string expectedAssemblyName = typeof(T).Assembly == typeof(object).Assembly
            ? "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
            : typeof(T).Assembly.FullName;

        Assert.Equal(expectedAssemblyName, arrayRecord.TypeName.AssemblyName!.FullName);

        if (PrimitiveTypes.Contains(typeof(T)))
        {
            Assert.True(arrayRecord is SZArrayRecord<T>, userMessage: typeof(T).Name);
        }
        else
        {
            Assert.True(arrayRecord is SZArrayRecord<ClassRecord>, userMessage: typeof(T).Name);
            Assert.True(arrayRecord.TypeNameMatches(typeof(T[])));
            Assert.Equal(arrayRecord.TypeName.GetElementType().AssemblyName.FullName, typeof(T).GetAssemblyNameIncludingTypeForwards());
        }

        foreach (Type type in PrimitiveTypes)
        {
            Assert.False(arrayRecord.TypeNameMatches(type));
            Assert.Equal(typeof(T) == type, arrayRecord.TypeNameMatches(type.MakeArrayType()));
        }

        if (PrimitiveTypes.Contains(typeof(T)))
        {
            Assert.Equal(array, arrayRecord.GetArray(typeof(T[])));
        }
    }

    private static void VerifyJaggedArray<T>(T input) where T : notnull
    {
        T[][] jaggedArray = [[input]];

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(jaggedArray));

        Assert.Equal(typeof(T[]).GetTypeFullNameIncludingTypeForwards(), arrayRecord.TypeName.GetElementType().FullName);

        Assert.False(arrayRecord.TypeNameMatches(typeof(T[])));
        Assert.True(arrayRecord.TypeNameMatches(typeof(T[][])));
        Assert.False(arrayRecord.TypeNameMatches(typeof(T[][][])));

        foreach (Type type in PrimitiveTypes)
        {
            Assert.False(arrayRecord.TypeNameMatches(type));
            Assert.False(arrayRecord.TypeNameMatches(type.MakeArrayType()));
            Assert.Equal(typeof(T) == type, arrayRecord.TypeNameMatches(type.MakeArrayType().MakeArrayType()));
            Assert.False(arrayRecord.TypeNameMatches(type.MakeArrayType().MakeArrayType().MakeArrayType()));
        }
    }

    private static void VerifyRectangularArray_2D<T>(T input) where T : notnull
    {
        T[,] rectangularArray = new T[1, 1];
        rectangularArray[0, 0] = input;

        VerifyRectangularArray<T>(rectangularArray);
    }

    private static void VerifyRectangularArray_5D<T>(T input) where T : notnull
    {
        T[,,,,] rectangularArray = new T[1, 1, 1, 1, 1];
        rectangularArray[0, 0, 0, 0, 0] = input;

        VerifyRectangularArray<T>(rectangularArray);
    }

    private static void VerifyRectangularArray<T>(Array array)
    {
        int arrayRank = array.GetType().GetArrayRank();
        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(array));

        Assert.Equal(typeof(T).GetTypeFullNameIncludingTypeForwards(), arrayRecord.TypeName.GetElementType().FullName);

        Assert.False(arrayRecord is SZArrayRecord<T>, userMessage: typeof(T).Name);
        Assert.True(arrayRecord.Rank > 1);

        foreach (Type type in PrimitiveTypes.Concat([typeof(T)]))
        {
            Assert.False(arrayRecord.TypeNameMatches(type));
            Assert.False(arrayRecord.TypeNameMatches(type.MakeArrayType(arrayRank - 1)));
            Assert.Equal(typeof(T) == type, arrayRecord.TypeNameMatches(type.MakeArrayType(arrayRank)));
            Assert.False(arrayRecord.TypeNameMatches(type.MakeArrayType(arrayRank + 1)));
        }
    }
}
