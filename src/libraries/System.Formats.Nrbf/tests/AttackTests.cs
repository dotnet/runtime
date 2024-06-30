using System.IO;
using System.Text;
using System.Reflection;
using Xunit;
using System.Linq;
using System.Reflection.Emit;

namespace System.Formats.Nrbf.Tests;

public class AttackTests : ReadTests
{
    [Serializable]
    public class WithCyclicReference
    {
        public string? Name;
        public WithCyclicReference? ReferenceToSelf;
    }

    [Fact]
    public void CyclicReferencesInClassesDoNotCauseStackOverflow()
    {
        WithCyclicReference input = new();
        input.Name = "hello";
        input.ReferenceToSelf = input;

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Assert.Same(classRecord, classRecord.GetClassRecord(nameof(WithCyclicReference.ReferenceToSelf)));
        Assert.Equal(input.Name, classRecord.GetString(nameof(WithCyclicReference.Name)));
    }

    [Fact]
    public void CyclicReferencesInSystemClassesDoNotCauseStackOverflow()
    {
        // CoreLib types are represented using a different record, that is why we need a dedicated test
        Exception input = new("hello");

        // set a reference to self by using private field
        typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(input, input);

        using MemoryStream stream = Serialize(input);

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(stream);

        Assert.Same(classRecord, classRecord.GetClassRecord(nameof(Exception.InnerException)));
        Assert.Equal(input.Message, classRecord.GetString(nameof(Exception.Message)));
    }

    [Fact]
    public void CyclicReferencesInArraysOfObjectsDoNotCauseStackOverflow()
    {
        object[] input = new object[2];
        input[0] = "not an array";
        input[1] = input;

        ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
        object?[] output = ((SZArrayRecord<object>)arrayRecord).GetArray();

        Assert.Equal(input[0], output[0]);
        Assert.Same(input, input[1]);
        Assert.Same(output, output[1]);
    }

    [Serializable]
    public class WithCyclicReferenceInArrayOfObjects
    {
        public string? Name;
        public object?[]? ArrayWithReferenceToSelf;
    }

    [Fact]
    public void CyclicClassReferencesInArraysOfObjectsDoNotCauseStackOverflow()
    {
        WithCyclicReferenceInArrayOfObjects input = new();
        input.Name = "hello";
        input.ArrayWithReferenceToSelf = [input];

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.Equal(input.Name, classRecord.GetString(nameof(WithCyclicReferenceInArrayOfObjects.Name)));
        SZArrayRecord<object> arrayRecord = (SZArrayRecord<object>)classRecord.GetSerializationRecord(nameof(WithCyclicReferenceInArrayOfObjects.ArrayWithReferenceToSelf))!;
        object?[] array = arrayRecord.GetArray();
        Assert.Same(classRecord, array.Single());
    }

    [Serializable]
    public class WithCyclicReferenceInArrayOfT
    {
        public string? Name;
        public WithCyclicReferenceInArrayOfT?[]? ArrayWithReferenceToSelf;
    }

    [Fact]
    public void CyclicClassReferencesInArraysOfTDoNotCauseStackOverflow()
    {
        WithCyclicReferenceInArrayOfT input = new();
        input.Name = "hello";
        input.ArrayWithReferenceToSelf = [input];

        ClassRecord classRecord = NrbfDecoder.DecodeClassRecord(Serialize(input));

        Assert.Equal(input.Name, classRecord.GetString(nameof(WithCyclicReferenceInArrayOfT.Name)));
        SZArrayRecord<ClassRecord> classRecords = (SZArrayRecord<ClassRecord>)classRecord.GetSerializationRecord(nameof(WithCyclicReferenceInArrayOfT.ArrayWithReferenceToSelf))!;
        Assert.Same(classRecord, classRecords.GetArray().Single());
    }

#if !NETFRAMEWORK
    // The tests need to ensure that 2GB+ does not get pre-allocated.
    // 200k is enough to get the job done and avoid getting false positives.
    const long AllocationThreshold = 200_000;

    // GC.GetAllocatedBytesForCurrentThread() is not available on Full Framework.
    // AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize is available,
    // but it reports allocations for all threads. Using this API would require
    // ensuring that it's the only test that is being run at a time.
    [Fact]
    public void ArraysOfStringsAreNotBeingPreAllocated()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.ArraySingleString);
        writer.Write(1); // object ID
        writer.Write(Array.MaxLength); // length
        writer.Write((byte)SerializationRecordType.ObjectNullMultiple);
        writer.Write(Array.MaxLength); // null count
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;

        long before = GetAllocatedByteCount();

        SerializationRecord serializationRecord = NrbfDecoder.Decode(stream);

        long after = GetAllocatedByteCount();

        Assert.InRange(after, before, before + AllocationThreshold);
        Assert.Equal(SerializationRecordType.ArraySingleString, serializationRecord.RecordType);
    }

    [Fact]
    public void ArraysOfBytesAreNotBeingPreAllocated()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.ArraySinglePrimitive);
        writer.Write(1); // object ID
        writer.Write(Array.MaxLength); // length
        writer.Write((byte)2); // PrimitiveType.Byte
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;

        long before = GetAllocatedByteCount();

        Assert.Throws<EndOfStreamException>(() => NrbfDecoder.Decode(stream));

        long after = GetAllocatedByteCount();

        Assert.InRange(after, before, before + AllocationThreshold);
    }

    private static long GetAllocatedByteCount()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return GC.GetAllocatedBytesForCurrentThread();
    }
#endif

    [Fact]
    public void UnboundedRecursion_NestedTypes_ActualBinaryFormatterInput()
    {
        Type[] ctorTypes = [typeof(string), typeof(Exception)];
        ConstructorInfo baseCtor = typeof(Exception).GetConstructor(ctorTypes)!;

        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new("Name"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("PlentyOfExceptions");

        Exception previous = new("Some message");
        for (int i = 0; i <= 10_000; i++)
        {
            Exception nested = CreateNewExceptionTypeAndInstantiateIt(ctorTypes, baseCtor, module, previous, i);
            previous = nested;
        }

        using MemoryStream stream = Serialize(previous);

        SerializationRecord serializationRecord = NrbfDecoder.Decode(stream);

        static Exception CreateNewExceptionTypeAndInstantiateIt(Type[] ctorTypes, ConstructorInfo baseCtor,
            ModuleBuilder module, Exception previous, int i)
        {
#pragma warning disable SYSLIB0050 // Type or member is obsolete (I know!)
            const TypeAttributes publicSerializable = TypeAttributes.Public | TypeAttributes.Serializable;
#pragma warning restore SYSLIB0050

            var typeBuilder = module.DefineType($"Exception{i}", publicSerializable, typeof(Exception));
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorTypes);

            // generate a ctor that simply passes (string message, Exception innerException) to base type (Exception)
            ILGenerator ilGenerator = ctorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0); // push "this"
            ilGenerator.Emit(OpCodes.Ldarg_1); // push "message"
            ilGenerator.Emit(OpCodes.Ldarg_2); // push "innerException"
            ilGenerator.Emit(OpCodes.Call, baseCtor);
            ilGenerator.Emit(OpCodes.Ret);

            Type newExceptionType = typeBuilder.CreateType();

            ConstructorInfo constructorInfo = newExceptionType.GetConstructor(ctorTypes)!;

            return (Exception)constructorInfo.Invoke([i.ToString(), previous]);
        }
    }

    [Theory]
    [InlineData(SerializationRecordType.ClassWithMembersAndTypes)]
    [InlineData(SerializationRecordType.SystemClassWithMembersAndTypes)]
    public void UnboundedRecursion_NestedClasses_FakeButValidInput(SerializationRecordType recordType)
    {
        const int ClassesCount = 10_000;
        const int LibraryId = ClassesCount + 1;

        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, "LibraryName");

        for (int i = 1; i <= ClassesCount;)
        {
            // ClassInfo (always present)
            writer.Write((byte)recordType);
            writer.Write(i); // object ID
            writer.Write($"Class{i}"); // type name
            bool isLast = i++ == ClassesCount;
            writer.Write(isLast ? 0 : 1); // member count (the last one has 0)

            if (!isLast)
            {
                writer.Write("memberName");
                // MemberTypeInfo (if needed)
                if (recordType is SerializationRecordType.ClassWithMembersAndTypes or SerializationRecordType.SystemClassWithMembersAndTypes)
                {
                    byte memberType = recordType is SerializationRecordType.SystemClassWithMembersAndTypes
                        ? (byte)3  // BinaryType.SystemClass
                        : (byte)4; // BinaryType.Class;

                    writer.Write(memberType);
                    writer.Write($"Class{i}"); // member type name

                    if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
                    {
                        writer.Write(LibraryId);
                    }
                }
            }
            // LibraryId (if needed)
            if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
            {
                writer.Write(LibraryId);
            }
        }

        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;

        SerializationRecord serializationRecord = NrbfDecoder.Decode(stream);
    }

    [Fact]
    public void UndoTruncatedTypeNamesIsSetToFalseByDefault()
        => Assert.False(new PayloadOptions().UndoTruncatedTypeNames);
}
