// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GenericGetDefaultMembersTest
    {
        public static void TryGetDefaultMembers(string AssemblyQualifiedNameOfTypeToGet, string[] expectedDefaultMembers)
        {
            Type typeToCheck;
            //Run tests
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MemberInfo[] defaultMembersReturned = typeToCheck.GetDefaultMembers();
            Assert.Equal(defaultMembersReturned.Length, expectedDefaultMembers.Length);
            int foundIndex;
            Array.Sort(expectedDefaultMembers);
            for (int i = 0; i < defaultMembersReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(expectedDefaultMembers, defaultMembersReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected member " + defaultMembersReturned[i].ToString() + " was returned");
            }
        }

        public static string ArrayToCommaList(string[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0];
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i];
                }
            }
            return returnString;
        }

        public static string ArrayToCommaList(MemberInfo[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }


        [Fact]
        public void Test1()
        {
            TryGetDefaultMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1[System.String]", new string[] { "System.String Item [Int32]" });
        }

        [Fact]
        public void Test2()
        {
            TryGetDefaultMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1", new string[] { "T Item [Int32]" });
        }

        [Fact]
        public void Test3()
        {
            //Test003
            TryGetDefaultMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClass`1", new string[] { "T ReturnAndSetField(T)" });
        }

        [Fact]
        public void Test4()
        {
            //Test004
            TryGetDefaultMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClass`1[System.Int32]", new string[] { "Int32 ReturnAndSetField(Int32)" });
        }
    }

    // build warnings about unused fields are not applicable to
    // reflection test cases
#pragma warning disable 0169
#pragma warning disable 0067
    #region Generics helper classes
    public class NonGenericClassString
    {
        public T method1<T, M>(T p)
        {
            return p;
        }
        public void method2(int p)
        {
            return;
        }
    }
    [DefaultMember("ReturnAndSetField")]
    public class GenericClass<T>
    {
        public T field;

        public GenericClass(T a)
        {
            field = a;
        }

        public T ReturnAndSetField(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public struct GenericStruct<T>
    {
        public T field;

        public T ReturnAndSetField(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public class GenericClass2TP<T, W>
    {
        public T field;
        public W field2;

        public T ReturnAndSetField1(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }

        public W ReturnAndSetField2(W newFieldValue)
        {
            field2 = newFieldValue;
            return field2;
        }
    }

    public struct GenericStruct2TP<T, W>
    {
        public T field;
        public W field2;

        public T ReturnAndSetField1(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }

        public W ReturnAndSetField2(W newFieldValue)
        {
            field2 = newFieldValue;
            return field2;
        }
    }

    public class GenericClassWithInterface<T> : IGenericInterface<T>
    {
        public T field;

        public GenericClassWithInterface(T a)
        {
            field = a;
        }

        public T ReturnAndSetFieldZero(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }

        public W GenericMethod<W>(W a)
        {
            return a;
        }
    }

    public class NonGenericClassWithGenericInterface : IGenericInterface<int>
    {
        public int field;

        public int ReturnAndSetFieldZero(int newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }


    public struct GenericStructWithInterface<T> : IGenericInterface<T>
    {
        public T field;
        public int field2;

        public GenericStructWithInterface(T a)
        {
            field = a;
            field2 = 0;
        }

        public GenericStructWithInterface(T a, int b)
        {
            field = a;
            field2 = b;
        }

        public T ReturnAndSetFieldZero(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public interface NonGenericInterface
    {
        void SayHello();
    }

    public interface IGenericInterface<T>
    {
        T ReturnAndSetFieldZero(T newFieldValue);
    }

    public interface IGenericInterface2<T, W>
    {
        void SetFieldOne(T newFieldValue);
        void SetFieldTwo(W newFieldValue);
    }

    public interface IGenericInterfaceInherits<U, V> : IGenericInterface<U>, IGenericInterface2<V, U>
    {
        V ReturnAndSetFieldThree(V newFieldValue);
    }

    public class GenericClassUsingNestedInterfaces<X, Y> : IGenericInterfaceInherits<X, Y>
    {
        public X FieldZero;
        public X FieldOne;
        public Y FieldTwo;
        public Y FieldThree;

        public GenericClassUsingNestedInterfaces(X a, X b, Y c, Y d)
        {
            FieldZero = a;
            FieldOne = b;
            FieldTwo = c;
            FieldThree = d;
        }

        public X ReturnAndSetFieldZero(X newFieldValue)
        {
            FieldZero = newFieldValue;
            return FieldZero;
        }

        public void SetFieldOne(Y newFieldValue)
        {
            FieldTwo = newFieldValue;
        }

        public void SetFieldTwo(X newFieldValue)
        {
            FieldOne = newFieldValue;
        }

        public Y ReturnAndSetFieldThree(Y newFieldValue)
        {
            FieldThree = newFieldValue;
            return FieldThree;
        }
    }

    public class GenericClassWithVarArgMethod<T>
    {
        public T field;

        public T publicField
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
            }
        }

        public T ReturnAndSetField(T newFieldValue, params T[] moreFieldValues)
        {
            field = newFieldValue;

            for (int i = 0; i <= moreFieldValues.Length - 1; i++)
            {
                field = moreFieldValues[i];
            }

            return field;
        }
    }

    public class ClassWithVarArgMethod
    {
        public int field;

        public int publicField
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
            }
        }

        public int ReturnAndSetField(int newFieldValue, params int[] moreFieldValues)
        {
            field = newFieldValue;

            for (int i = 0; i <= moreFieldValues.Length - 1; i++)
            {
                field = moreFieldValues[i];
            }

            return field;
        }
    }


    public class NonGenericClassWithVarArgGenericMethod
    {
        public T ReturnAndSetField<T>(T newFieldValue, params T[] moreFieldValues)
        {
            T field;

            field = newFieldValue;

            for (int i = 0; i <= moreFieldValues.Length - 1; i++)
            {
                field = moreFieldValues[i];
            }

            return field;
        }
    }

    public interface IConsume
    {
        object[] StuffConsumed
        {
            get;
        }

        void Eat(object ThingEaten);

        object[] Puke(int Amount);
    }

    public class PackOfCarnivores<T> where T : IConsume
    {
        public T[] pPack;
    }

    public class Cat<C> : IConsume
    {
        private List<object> _pStuffConsumed = new List<object>();

        public event EventHandler WeightChanged;

        private event EventHandler WeightStayedTheSame;

        private static EventHandler s_catDisappeared;

        public object[] StuffConsumed
        {
            get
            {
                return _pStuffConsumed.ToArray();
            }
        }

        public void Eat(object ThingEaten)
        {
            _pStuffConsumed.Add(ThingEaten);
        }

        public object[] Puke(int Amount)
        {
            object[] vomit;
            if (_pStuffConsumed.Count < Amount)
            {
                Amount = _pStuffConsumed.Count;
            }
            vomit = _pStuffConsumed.GetRange(_pStuffConsumed.Count - Amount, Amount).ToArray();
            _pStuffConsumed.RemoveRange(_pStuffConsumed.Count - Amount, Amount);
            return vomit;
        }
    }

    public class GenericArrayWrapperClass<T>
    {
        private T[] _field;
        private int _field1;

        public int myProperty
        {
            get
            {
                return 0;
            }
            set
            {
                _field1 = value;
            }
        }

        public GenericArrayWrapperClass(T[] fieldValues)
        {
            int size = fieldValues.Length;
            _field = new T[size];
            for (int i = 0; i < _field.Length; i++)
            {
                _field[i] = fieldValues[i];
            }
        }

        public T this[int index]
        {
            get
            {
                return _field[index];
            }
            set
            {
                _field[index] = value;
            }
        }
    }

    public class GenericOuterClass<T>
    {
        public T field;

        public class GenericNestedClass<W>
        {
            public T field;
            public W field2;
        }

        public class NestedClass
        {
            public T field;
            public int field2;
        }
    }

    public class OuterClass
    {
        public int field;

        public class GenericNestedClass<W>
        {
            public int field;
            public W field2;
        }

        public class NestedClass
        {
            public string field;
            public int field2;
        }
    }
    #endregion
#pragma warning restore 0067
#pragma warning restore 0169
}
