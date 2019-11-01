// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

[Serializable]
public struct TypeWithoutNamespace { }

namespace System.Runtime.Serialization.Formatters.Tests
{
    [Serializable]
    public sealed class SealedObjectWithIntStringFields
    {
        public int Member1;
        public string Member2;
        public string Member3;

        public override bool Equals(object obj)
        {
            var o = obj as SealedObjectWithIntStringFields;
            if (o == null)
                return false;
            return
                EqualityComparer<int>.Default.Equals(Member1, o.Member1) &&
                EqualityComparer<string>.Default.Equals(Member2, o.Member2) &&
                EqualityComparer<string>.Default.Equals(Member3, o.Member3);
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class ObjectWithIntStringUShortUIntULongAndCustomObjectFields
    {
        public int Member1;
        public string Member2;
        public string _member3;
        public SealedObjectWithIntStringFields Member4;
        public SealedObjectWithIntStringFields Member4shared;
        public SealedObjectWithIntStringFields Member5;
        public string Member6;
        public string str1;
        public string str2;
        public string str3;
        public string str4;
        public ushort u16;
        public uint u32;
        public ulong u64;

        public override bool Equals(object obj)
        {
            var o = obj as ObjectWithIntStringUShortUIntULongAndCustomObjectFields;
            if (o == null)
                return false;

            return
                EqualityComparer<int>.Default.Equals(Member1, o.Member1) &&
                EqualityComparer<string>.Default.Equals(Member2, o.Member2) &&
                EqualityComparer<string>.Default.Equals(_member3, o._member3) &&
                EqualityComparer<SealedObjectWithIntStringFields>.Default.Equals(Member4, o.Member4) &&
                EqualityComparer<SealedObjectWithIntStringFields>.Default.Equals(Member4shared, o.Member4shared) &&
                EqualityComparer<SealedObjectWithIntStringFields>.Default.Equals(Member5, o.Member5) &&
                EqualityComparer<string>.Default.Equals(Member6, o.Member6) &&
                EqualityComparer<string>.Default.Equals(str1, o.str1) &&
                EqualityComparer<string>.Default.Equals(str2, o.str2) &&
                EqualityComparer<string>.Default.Equals(str3, o.str3) &&
                EqualityComparer<string>.Default.Equals(str4, o.str4) &&
                EqualityComparer<ushort>.Default.Equals(u16, o.u16) &&
                EqualityComparer<uint>.Default.Equals(u16, o.u16) &&
                EqualityComparer<ulong>.Default.Equals(u64, o.u64) &&
                // make sure shared members are the same object
                ReferenceEquals(Member4, Member4shared) &&
                ReferenceEquals(o.Member4, o.Member4shared);
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class Point : IComparable<Point>, IEquatable<Point>
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as Point);
        }

        public int CompareTo(Point other)
        {
            return other == null ? 1 : 0;
        }

        public override bool Equals(object obj) => Equals(obj as Point);

        public bool Equals(Point other)
        {
            return other != null &&
                X == other.X &&
                Y == other.Y;
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class Tree<T>
    {
        public Tree(T value, Tree<T> left, Tree<T> right)
        {
            Value = value;
            Left = left;
            Right = right;
        }

        public T Value { get; }
        public Tree<T> Left { get; }
        public Tree<T> Right { get; }

        public override bool Equals(object obj)
        {
            Tree<T> o = obj as Tree<T>;
            if (o == null)
                return false;

            return
                EqualityComparer<T>.Default.Equals(Value, o.Value) &&
                EqualityComparer<Tree<T>>.Default.Equals(Left, o.Left) &&
                EqualityComparer<Tree<T>>.Default.Equals(Right, o.Right) &&
                // make sure the branches aren't actually the exact same object
                (Left == null || !ReferenceEquals(Left, o.Left)) &&
                (Right == null || !ReferenceEquals(Right, o.Right));
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class Graph<T>
    {
        public T Value;
        public Graph<T>[] Links;

        public override bool Equals(object obj)
        {
            Graph<T> o = obj as Graph<T>;
            if (o == null)
                return false;

            var toExplore = new Stack<KeyValuePair<Graph<T>, Graph<T>>>();
            toExplore.Push(new KeyValuePair<Graph<T>, Graph<T>>(this, o));
            var seen1 = new HashSet<Graph<T>>(new ObjectReferenceEqualityComparer());
            while (toExplore.Count > 0)
            {
                var cur = toExplore.Pop();
                if (!seen1.Add(cur.Key))
                {
                    continue;
                }

                if (!EqualityComparer<T>.Default.Equals(cur.Key.Value, cur.Value.Value))
                {
                    return false;
                }

                if (Links == null || o.Links == null)
                {
                    if (Links != o.Links)
                        return false;
                    continue;
                }

                if (Links.Length != o.Links.Length)
                {
                    return false;
                }

                for (int i = 0; i < Links.Length; i++)
                {
                    toExplore.Push(new KeyValuePair<Graph<T>, Graph<T>>(Links[i], o.Links[i]));
                }
            }

            return true;
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public sealed class ObjectWithArrays
    {
        public int[] IntArray;
        public string[] StringArray;
        public Tree<int>[] TreeArray;
        public byte[] ByteArray;
        public int[][] JaggedArray;
        public int[,] MultiDimensionalArray;

        public override bool Equals(object obj)
        {
            ObjectWithArrays o = obj as ObjectWithArrays;
            if (o == null)
                return false;

            return
                EqualityHelpers.ArraysAreEqual(IntArray, o.IntArray) &&
                EqualityHelpers.ArraysAreEqual(StringArray, o.StringArray) &&
                EqualityHelpers.ArraysAreEqual(TreeArray, o.TreeArray) &&
                EqualityHelpers.ArraysAreEqual(ByteArray, o.ByteArray) &&
                EqualityHelpers.ArraysAreEqual(JaggedArray, o.JaggedArray) &&
                EqualityHelpers.ArraysAreEqual(MultiDimensionalArray, o.MultiDimensionalArray);
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public enum Colors
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple
    }

    [Serializable] public enum ByteEnum : byte { }
    [Serializable] public enum SByteEnum : sbyte { }
    [Serializable] public enum Int16Enum : short { }
    [Serializable] public enum UInt16Enum : ushort { }
    [Serializable] public enum Int32Enum : int { }
    [Serializable] public enum UInt32Enum : uint { }
    [Serializable] public enum Int64Enum : long { }
    [Serializable] public enum UInt64Enum : ulong { }

    public struct NonSerializableStruct
    {
        public int Value;
    }

    public class NonSerializableClass
    {
        public int Value;
    }

    [Serializable]
    public class SerializableClassDerivedFromNonSerializableClass : NonSerializableClass
    {
        public int AnotherValue;
    }

    [Serializable]
    public class SerializableClassWithBadField
    {
        public NonSerializableClass Value;
    }

    [Serializable]
    public struct EmptyStruct { }

    [Serializable]
    public struct StructWithIntField
    {
        public int X;
    }

    [Serializable]
    public struct StructWithStringFields
    {
        public string String1;
        public string String2;
    }

    [Serializable]
    public struct StructContainingOtherStructs
    {
        public StructWithStringFields Nested1;
        public StructWithStringFields Nested2;
    }

    [Serializable]
    public struct StructContainingArraysOfOtherStructs
    {
        public StructContainingOtherStructs[] Nested;

        public override bool Equals(object obj)
        {
            if (!(obj is StructContainingArraysOfOtherStructs))
                return false;
            return EqualityHelpers.ArraysAreEqual(Nested, ((StructContainingArraysOfOtherStructs)obj).Nested);
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class BasicISerializableObject : ISerializable
    {
        private NonSerializablePair<int, string> _data;

        public BasicISerializableObject(int value1, string value2)
        {
            _data = new NonSerializablePair<int, string> { Value1 = value1, Value2 = value2 };
        }

        public BasicISerializableObject(SerializationInfo info, StreamingContext context)
        {
            _data = new NonSerializablePair<int, string> { Value1 = info.GetInt32("Value1"), Value2 = info.GetString("Value2") };
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value1", _data.Value1);
            info.AddValue("Value2", _data.Value2);
        }

        public override bool Equals(object obj)
        {
            var o = obj as BasicISerializableObject;
            if (o == null)
                return false;
            if (_data == null || o._data == null)
                return _data == o._data;
            return _data.Value1 == o._data.Value1 && _data.Value2 == o._data.Value2;
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public sealed class DerivedISerializableWithNonPublicDeserializationCtor : BasicISerializableObject
    {
        public DerivedISerializableWithNonPublicDeserializationCtor(int value1, string value2) : base(value1, value2) { }
        private DerivedISerializableWithNonPublicDeserializationCtor(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class IncrementCountsDuringRoundtrip
    {
        public int IncrementedDuringOnSerializingMethod;
        public int IncrementedDuringOnSerializedMethod;
        [NonSerialized] public int IncrementedDuringOnDeserializingMethod;
        public int IncrementedDuringOnDeserializedMethod;

        public IncrementCountsDuringRoundtrip(string ignored) { } // non-default ctor so that we can observe changes from OnDeserializing

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => IncrementedDuringOnSerializingMethod++;

        [OnSerialized]
        private void OnSerializedMethod(StreamingContext context) => IncrementedDuringOnSerializedMethod++;

        [OnDeserializing]
        private void OnDeserializingMethod(StreamingContext context) => IncrementedDuringOnDeserializingMethod++;

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => IncrementedDuringOnDeserializedMethod++;
    }

    [Serializable]
    public sealed class DerivedIncrementCountsDuringRoundtrip : IncrementCountsDuringRoundtrip
    {
        internal int DerivedIncrementedDuringOnSerializingMethod;
        internal int DerivedIncrementedDuringOnSerializedMethod;
        [NonSerialized] internal int DerivedIncrementedDuringOnDeserializingMethod;
        internal int DerivedIncrementedDuringOnDeserializedMethod;

        public DerivedIncrementCountsDuringRoundtrip(string ignored) : base(ignored) { }

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => DerivedIncrementedDuringOnSerializingMethod++;

        [OnSerialized]
        private void OnSerializedMethod(StreamingContext context) => DerivedIncrementedDuringOnSerializedMethod++;

        [OnDeserializing]
        private void OnDeserializingMethod(StreamingContext context) => DerivedIncrementedDuringOnDeserializingMethod++;

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => DerivedIncrementedDuringOnDeserializedMethod++;
    }

    [Serializable]
    public sealed class ObjRefReturnsObj : IObjectReference
    {
        public object Real;
        public object GetRealObject(StreamingContext context) => Real;
    }

    internal sealed class NonSerializablePair<T1, T2>
    {
        public T1 Value1;
        public T2 Value2;
    }

    internal sealed class NonSerializablePairSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var pair = (NonSerializablePair<int, string>)obj;
            info.AddValue("Value1", pair.Value1);
            info.AddValue("Value2", pair.Value2);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var pair = (NonSerializablePair<int, string>)obj;
            pair.Value1 = info.GetInt32("Value1");
            pair.Value2 = info.GetString("Value2");
            return pair;
        }
    }

    [Serializable]
    public class Version1ClassWithoutField
    {
    }

    [Serializable]
    public class Version2ClassWithoutOptionalField
    {
        public object Value;
    }

    [Serializable]
    public class Version2ClassWithOptionalField
    {
        [OptionalField(VersionAdded = 2)]
        public object Value;
    }

    [Serializable]
    public class ObjectWithStateAndMethod
    {
        public int State;
        public int GetState() => State;
    }

    internal sealed class ObjectReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    [Serializable]
    public sealed class PointEqualityComparer : IEqualityComparer<Point>
    {
        public bool Equals(Point x, Point y) => (x.X == y.X) && (x.Y == y.Y);

        public int GetHashCode(Point obj) => RuntimeHelpers.GetHashCode(obj);
    }

    [Serializable]
    public class SimpleKeyedCollection : System.Collections.ObjectModel.KeyedCollection<int, Point>
    {
        protected override int GetKeyForItem(Point item)
        {
            return item.Y;
        }
    }

    [Serializable]
    internal class GenericTypeWithArg<T>
    {
        public T Test;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var p = (GenericTypeWithArg<T>)obj;
            return Test.Equals(p.Test);
        }

        public override int GetHashCode()
        {
            return Test == null ? 0 : Test.GetHashCode();
        }
    }

    [Serializable]
    internal class SomeType
    {
        public int SomeField;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var p = (SomeType)obj;
            return SomeField.Equals(p.SomeField);
        }

        public override int GetHashCode()
        {
            return SomeField;
        }
    }

    internal static class EqualityHelpers
    {
        public static bool ArraysAreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == null || array2 == null)
                return array1 == array2;
            if (array1.Length != array2.Length)
                return false;
            for (int i = 0; i < array1.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(array1[i], array2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ArraysAreEqual(Array array1, Array array2)
        {
            if (array1 == null || array2 == null)
                return array1 == array2;
            if (array1.Length != array2.Length)
                return false;
            if (array1.Rank != array2.Rank)
                return false;

            for (int i = 0; i < array1.Rank; i++)
            {
                if (array1.GetLength(i) != array2.GetLength(i))
                    return false;
            }

            var e1 = array1.GetEnumerator();
            var e2 = array2.GetEnumerator();
            while (e1.MoveNext())
            {
                e2.MoveNext();
                if (!EqualityComparer<object>.Default.Equals(e1.Current, e2.Current))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ArraysAreEqual<T>(T[][] array1, T[][] array2)
        {
            if (array1 == null || array2 == null)
                return array1 == array2;
            if (array1.Length != array2.Length)
                return false;
            for (int i = 0; i < array1.Length; i++)
            {
                T[] sub1 = array1[i], sub2 = array2[i];
                if (sub1 == null || (sub2 == null && (sub1 != sub2)))
                    return false;
                if (sub1.Length != sub2.Length)
                    return false;
                for (int j = 0; j < sub1.Length; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(sub1[j], sub2[j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

#pragma warning disable 0618 // obsolete warning
    [Serializable]
    internal class HashCodeProvider : IHashCodeProvider
    {
        public int GetHashCode(object obj)
        {
            return 8;
        }
    }
#pragma warning restore 0618
}
