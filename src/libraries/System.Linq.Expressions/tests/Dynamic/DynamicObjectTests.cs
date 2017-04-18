// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class DynamicObjectTests
    {
        // We'll test trying to access fields dynamically, but the compiler won't see that
        // so disable CS0649.
#pragma warning disable 649
        private class TestDynamicBase : DynamicObject
        {
            private Dictionary<int, int> _dictionary = new Dictionary<int, int>();
            public int Field;
            public readonly int ReadonlyField;
            public int HideField;

            public int Property { get; set; }

            public int ReadonlyProperty => 0;

            public int Method(int argument) => argument;

            public virtual int VirtualProperty { get; set; }
            public virtual int VirtualReadonlyProperty => 1;

            public virtual int VirtualMethod(int argument) => argument;
            public int HideProperty { get; set; }

            public int HideMethod(int argument) => argument;

            public int this[int index]
            {
                get { return _dictionary[index]; }
                set { _dictionary[index] = value; }
            }
        }

        private class TestDynamic : TestDynamicBase
        {
            public new int HideField;
            public override int VirtualProperty { get; set; }

            public override int VirtualReadonlyProperty => 2;

            public override int VirtualMethod(int argument) => argument * 2;

            public new int HideProperty { get; set; }
            public new int HideMethod { get; set; }
        }
#pragma warning restore 649

        private class TestDynamicFagile : TestDynamic
        {
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                throw new Exception();
            }
        }

        private class TestDynamicNameReflective : TestDynamic
        {
            // Returns the name of the property back as the result, but is a stickler for .NET naming
            // conventions and returns false if the first character isn't uppercase
            // (Correct handling of Unicode category Lt omitted)
            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                string name = binder.Name;
                if (char.IsUpper(name[0]))
                {
                    result = binder.Name;
                    return true;
                }

                result = null;
                return false;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                string name = binder.Name;
                if (char.IsUpper(name[0]))
                {
                    // Allows set (as noop) if it means that it's being given the same value the get
                    // above would return anyway.
                    if (value as string != name)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    return true;
                }

                return false;
            }
        }

        private class TestDynamicNameReflectiveNotOverride : TestDynamic
        {
            // Like TestDynamicNameReflective but hides rather than overiding.
            // Because DynamicObject reflects upon itself to see if these methods are
            // overridden, we test this override-detection against finding hiding
            // methods.
            public new bool TryGetMember(GetMemberBinder binder, out object result)
            {
                string name = binder.Name;
                if (char.IsUpper(name[0]))
                {
                    result = binder.Name;
                    return true;
                }

                result = null;
                return false;
            }

            public new bool TrySetMember(SetMemberBinder binder, object value)
            {
                string name = binder.Name;
                if (char.IsUpper(name[0]))
                {
                    // Allows set (as noop) if it means that it's being given the same value the get
                    // above would return anyway.
                    if (value as string != name)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    return true;
                }

                return false;
            }
        }

        private class SetOnlyProperties : DynamicObject
        {
            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                return true;
            }
        }

        private class DynamicallyConvertable : DynamicObject
        {
            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                if (binder.ReturnType == typeof(string))
                {
                    result = nameof(DynamicallyConvertable);
                    return true;
                }

                if (binder.ReturnType == typeof(DateTime) && binder.Explicit)
                {
                    result = new DateTime(1991, 8, 6);
                    return true;
                }

                result = null;
                return false;
            }

            public static explicit operator DateTimeOffset(DynamicallyConvertable source) =>
                new DateTimeOffset(1991, 8, 6, 0, 0, 0, new TimeSpan(2, 0, 0));

            public static implicit operator Uri(DynamicallyConvertable source) =>
                new Uri("http://example.net/");
        }

        private class DynamicallyConvertableNotOverride : DynamicObject
        {
            public new bool TryConvert(ConvertBinder binder, out object result)
            {
                if (binder.ReturnType == typeof(string))
                {
                    result = nameof(DynamicallyConvertable);
                    return true;
                }

                if (binder.ReturnType == typeof(DateTime) && binder.Explicit)
                {
                    result = new DateTime(1991, 8, 6);
                    return true;
                }

                result = null;
                return false;
            }

            public static explicit operator DateTimeOffset(DynamicallyConvertableNotOverride source) =>
                new DateTimeOffset(1991, 8, 6, 0, 0, 0, new TimeSpan(2, 0, 0));

            public static implicit operator Uri(DynamicallyConvertableNotOverride source) =>
                new Uri("http://example.net/");
        }

        private class DynamicallyInvokableIntPower : DynamicObject
        {
            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                if (args.Length == 2)
                {
                    int x;
                    int y;
                    try
                    {
                        x = Convert.ToInt32(args[0]);
                        y = Convert.ToInt32(args[1]);
                    }
                    catch (Exception)
                    {
                        result = null;
                        return false;
                    }

                    result = checked((int)Math.Pow(x, y));
                    return true;
                }

                result = null;
                return false;
            }
        }

        private class DynamicallyInvokableIntPowerNotOverride : DynamicObject
        {
            public new bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                if (args.Length == 2)
                {
                    int x;
                    int y;
                    try
                    {
                        x = Convert.ToInt32(args[0]);
                        y = Convert.ToInt32(args[1]);
                    }
                    catch (Exception)
                    {
                        result = null;
                        return false;
                    }

                    result = checked((int)Math.Pow(x, y));
                    return true;
                }

                result = null;
                return false;
            }
        }

        private class DynamicallyInvokableIntPowerMember : DynamicObject
        {
            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                if (binder.Name.Equals(
                    "Power", binder.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                    && args.Length == 2)
                {
                    int x;
                    int y;
                    try
                    {
                        x = Convert.ToInt32(args[0]);
                        y = Convert.ToInt32(args[1]);
                    }
                    catch (Exception)
                    {
                        result = null;
                        return false;
                    }

                    result = checked((int)Math.Pow(x, y));
                    return true;
                }

                result = null;
                return false;
            }

            public int Modulo(int x, int y) => x % y;
        }

        private class Swapper : DynamicObject
        {
            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                result = null;
                if (args.Length == 2)
                {
                    object temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                    return true;
                }

                return false;
            }
        }

        private class IndexableObject : DynamicObject
        {
            Dictionary<int, int> _oneDimension = new Dictionary<int, int>();
            Dictionary<Tuple<int, int>, string> _twoDimensions = new Dictionary<Tuple<int, int>, string>();

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                switch (indexes.Length)
                {
                    case 1:
                        if (indexes[0] is int)
                        {
                            int value = _oneDimension[(int)indexes[0]];
                            result = value;
                            return true;
                        }

                        break;
                    case 2:
                        if (indexes[0] is int && indexes[1] is int)
                        {
                            string value = _twoDimensions[Tuple.Create((int)indexes[0], (int)indexes[1])];
                            result = value;
                            return true;
                        }

                        break;
                }

                result = null;
                return false;
            }

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                switch (indexes.Length)
                {
                    case 1:
                        if (indexes[0] is int && value is int)
                        {
                            _oneDimension[(int)indexes[0]] = (int)value;
                            return true;
                        }

                        break;
                    case 2:
                        if (indexes[0] is int && indexes[1] is int && value is string)
                        {
                            _twoDimensions[Tuple.Create((int)indexes[0], (int)indexes[1])] = (string)value;
                            return true;
                        }

                        break;
                }

                return false;
            }
        }

        private class NegatableNum : DynamicObject
        {
            public NegatableNum(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
            {
                if (binder.Operation == ExpressionType.Negate)
                {
                    result = new NegatableNum(unchecked(-Value));
                    return true;
                }

                result = null;
                return false;
            }
        }

        private class AddableNum : DynamicObject
        {
            public AddableNum(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
            {
                if (binder.Operation == ExpressionType.Add)
                {
                    AddableNum addend = arg as AddableNum;
                    if (addend != null)
                    {
                        result = new AddableNum(Value + addend.Value);
                        return true;
                    }
                }

                result = null;
                return false;
            }
        }

        private class Accumulator : DynamicObject
        {
            // Allows addition with itself on the lhs, but not with it on the rhs
            public Accumulator(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
            {
                if (binder.Operation == ExpressionType.Add)
                {
                    AddableNum addend = arg as AddableNum;
                    if (addend != null)
                    {
                        result = new Accumulator(Value + addend.Value);
                        return true;
                    }
                }

                result = null;
                return false;
            }
        }

        private class TraditionalDynamicObject : DynamicObject
        {
            private static readonly string[] Names =
            {
                "Foo", "Bar", "Baz", "Quux", "Quuux", "Quuuux", "Quuuuux",
                "Quuuuuux"
            };

            public override IEnumerable<string> GetDynamicMemberNames() => Names;

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                for (int idx = 0; idx != Names.Length; ++idx)
                {
                    if (binder.Name == Names[idx])
                    {
                        result = idx;
                        return true;
                    }
                }

                result = null;
                return false;
            }
        }

        [Fact]
        public void GetSetDefinedField()
        {
            var test = new TestDynamicFagile();
            test.Field = 43;
            dynamic d = test;
            Assert.Equal(43, d.Field);
            d.Field = 93;
            Assert.Equal(93, test.Field);
        }

        [Fact]
        public void GetDynamicProperty()
        {
            dynamic d = new TestDynamicNameReflective();
            Assert.Equal("DynProp", d.DynProp);
        }

        [Fact]
        public void FailToGetDynamicProperty()
        {
            dynamic d = new TestDynamicNameReflective();
            Assert.Throws<RuntimeBinderException>(() => d.notToBeFound);
        }

        [Fact]
        public void GetDynamicPropertyWithHidingTryGetMember()
        {
            dynamic d = new TestDynamicNameReflectiveNotOverride();
            Assert.Throws<RuntimeBinderException>(() => d.DynProp);
            Assert.Equal(2, d.VirtualReadonlyProperty);
        }

        [Fact]
        public void SetDynamicProperty()
        {
            dynamic d = new TestDynamicNameReflective();
            d.DynProp = nameof(d.DynProp);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => d.DynProp = "I wandered lonely as a cloud.");
        }

        [Fact]
        public void FailToSetDynamicProperty()
        {
            dynamic d = new TestDynamicNameReflective();
            Assert.Throws<RuntimeBinderException>(() => d.notToBeFound = nameof(d.notToBeFound));
        }

        [Fact]
        public void SetDynamicPropertyWithHidingTrySetMember()
        {
            dynamic d = new TestDynamicNameReflectiveNotOverride();
            Assert.Throws<RuntimeBinderException>(() => d.DynProp = nameof(d.DynProp));
        }

        [Fact]
        public void SetPropertyReturnsValueWithoutUsingGetter()
        {
            dynamic d = new SetOnlyProperties();
            object value = new object();
            object result = d.SomeProperty = value;
            Assert.Same(value, result);
        }

        [Fact]
        public void AttemptWriteReadonlyField()
        {
            TestDynamic td = new TestDynamic();
            dynamic d = td;
            Assert.Throws<RuntimeBinderException>(() => d.ReadonlyField = 3);
            Assert.Equal(0, td.ReadonlyField); // Confirm exception happened before a write, rather than after.
        }

        [Fact]
        public void ReadWriteHidingField()
        {
            TestDynamic td = new TestDynamic();
            dynamic d = td;
            td.HideField = 9;
            Assert.Equal(9, d.HideField);
            Assert.Equal(9, td.HideField);
            Assert.Equal(0, ((TestDynamicBase)td).HideField);
        }

        [Fact]
        public void MetaDynamicKnowsNamesFromDynamic()
        {
            var trad = new TraditionalDynamicObject();
            Assert.Same(trad.GetDynamicMemberNames(), trad.GetMetaObject(Expression.Parameter(typeof(object))).GetDynamicMemberNames());
        }

        [Fact]
        public void ConvertBuiltInImplicit()
        {
            dynamic d = new DynamicallyConvertable();
            Uri u = d;
            Assert.Equal(new Uri("http://example.net/"), u);
        }

        [Fact]
        public void ConvertBuiltInExplicit()
        {
            dynamic d = new DynamicallyConvertable();
            DateTimeOffset dto = (DateTimeOffset)d;
            Assert.Equal(new DateTimeOffset(1991, 8, 6, 0, 0, 0, new TimeSpan(2, 0, 0)), dto);
        }

        [Fact]
        public void ConvertFailImplicitWhenMatchingExplicit()
        {
            dynamic d = new DynamicallyConvertable();
            DateTimeOffset dto = default(DateTimeOffset);
            Assert.Throws<RuntimeBinderException>(() => dto = d);
        }

        [Fact]
        public void ConvertDynamicImplicit()
        {
            dynamic d = new DynamicallyConvertable();
            string name = d;
            Assert.Equal(nameof(DynamicallyConvertable), name);
        }

        [Fact]
        public void ConvertDynamicExplicit()
        {
            dynamic d = new DynamicallyConvertable();
            DateTime dt = (DateTime)d;
            Assert.Equal(new DateTime(1991, 8, 6), dt);
        }

        [Fact]
        public void ConvertFailImplicitOfferedDynamicallyExplicit()
        {
            dynamic d = new DynamicallyConvertable();
            DateTime dt = default(DateTime);
            Assert.Throws<RuntimeBinderException>(() => dt = d);
        }

        [Fact]
        public void ConvertFailNotOfferedConversion()
        {
            dynamic d = new DynamicallyConvertable();
            Expression ex = null;
            Assert.Throws<RuntimeBinderException>(() => ex = d);
            Assert.Throws<RuntimeBinderException>(() => ex = (Expression)d);
        }

        [Fact]
        public void ConvertHidingTryConvert()
        {
            dynamic d = new DynamicallyConvertableNotOverride();
            Uri u = d;
            Assert.Equal(new Uri("http://example.net/"), u);
            DateTimeOffset dto = (DateTimeOffset)d;
            Assert.Equal(new DateTimeOffset(1991, 8, 6, 0, 0, 0, new TimeSpan(2, 0, 0)), dto);
            DateTime dt = default(DateTime);
            string s = null;
            Assert.Throws<RuntimeBinderException>(() => dt = d);
            Assert.Throws<RuntimeBinderException>(() => dto = d);
            Assert.Throws<RuntimeBinderException>(() => s = d);
        }

        [Fact]
        public void DynamicInvoke()
        {
            dynamic d = new DynamicallyInvokableIntPower();
            int pow = d(8, 9);
            Assert.Equal(134217728, pow);
            Assert.Throws<OverflowException>(() => d(int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void DynamicInvokeMismatch()
        {
            dynamic d = new DynamicallyInvokableIntPower();
            Assert.Throws<RuntimeBinderException>(() => d(9));
            Assert.Throws<RuntimeBinderException>(() => d());
            Assert.Throws<RuntimeBinderException>(() => d(1, 2, 3));
            Assert.Throws<RuntimeBinderException>(() => d("eight", "nine"));
        }

        [Fact]
        public void DynamicInvokeNotOverride()
        {
            dynamic d = new DynamicallyInvokableIntPowerNotOverride();
            Assert.Throws<RuntimeBinderException>(() => d(8, 9));
            Assert.Throws<RuntimeBinderException>(() => d(int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void DynamicInvokeMember()
        {
            dynamic d = new DynamicallyInvokableIntPowerMember();
            int pow = d.Power(8, 9);
            Assert.Equal(134217728, pow);
            Assert.Throws<OverflowException>(() => d.Power(int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void DynamicInvokeMemberMismatch()
        {
            dynamic d = new DynamicallyInvokableIntPowerMember();
            Assert.Throws<RuntimeBinderException>(() => d.Power(9));
            Assert.Throws<RuntimeBinderException>(() => d.Power());
            Assert.Throws<RuntimeBinderException>(() => d.Power(1, 2, 3));
            Assert.Throws<RuntimeBinderException>(() => d.Power("eight", "nine"));
            Assert.Throws<RuntimeBinderException>(() => d.power(8, 9));
        }

        [Fact]
        public void DynamicInvokeMemberStaticMember()
        {
            dynamic d = new DynamicallyInvokableIntPowerMember();
            int mod = d.Modulo(233, 12);
            Assert.Equal(5, mod);
            Assert.Throws<RuntimeBinderException>(() => d.Modulo(2));
            Assert.Throws<RuntimeBinderException>(() => d.modulo(233, 12));
            Assert.Throws<RuntimeBinderException>(() => d.Modulo());
            Assert.Throws<RuntimeBinderException>(() => d.Modulo(233, 12, 9));
            Assert.Throws<RuntimeBinderException>(() => d.Modulo("two hundred and thirty-three", "twelve"));
        }

        [Fact]
        public void DynamicUnaryOperation()
        {
            dynamic d = new NegatableNum(23);
            dynamic r = -d;
            Assert.Equal(-23, r.Value);
            d = new NegatableNum(int.MinValue);
            r = -d;
            Assert.Equal(int.MinValue, r.Value);
        }

        [Fact]
        public void DynamicUnaryOperationNotSupported()
        {
            dynamic d = new NegatableNum(23);
            Assert.Throws<RuntimeBinderException>(() => ~d);
        }

        [Fact]
        public void DynamicUnaryOperationNoOverrides()
        {
            dynamic d = new TestDynamic();
            Assert.Throws<RuntimeBinderException>(() => -d);
        }

        [Fact]
        public void DynamicAddition()
        {
            dynamic x = new AddableNum(23);
            dynamic y = new AddableNum(42);
            dynamic r = x + y;
            Assert.Equal(23 + 42, r.Value);
        }

        [Fact]
        public void DynamicBinaryUnsupported()
        {
            dynamic x = new AddableNum(23);
            dynamic y = new AddableNum(42);
            Assert.Throws<RuntimeBinderException>(() => x * y);
        }

        [Fact]
        public void DynamicBinaryUnidirectional()
        {
            dynamic x = new Accumulator(23);
            dynamic y = new AddableNum(42);
            dynamic r = x + y;
            Assert.Equal(23 + 42, r.Value);
            Assert.Throws<RuntimeBinderException>(() => y + x);
        }

        [Fact]
        public void DynamicBinaryNoOverride()
        {
            dynamic x = new TestDynamic();
            dynamic y = new TestDynamic();
            Assert.Throws<RuntimeBinderException>(() => x + y);
        }

        [Fact]
        public void ByRefInvoke()
        {
            dynamic d = new Swapper();
            int x = 23;
            int y = 42;
            d(ref x, ref y);
            Assert.Equal(42, x);
            Assert.Equal(23, y);
        }

        [Fact]
        public void ByRefMismatch()
        {
            dynamic d = new Swapper();
            long x = 23;
            int y = 42;
            d(x, y); // Okay because no write-back.
            Assert.Throws<InvalidCastException>(() => d(ref x, ref y));
        }

        [Fact]
        public void IndexGetAndRetrieve()
        {
            dynamic d = new IndexableObject();
            d[23] = 42;
            Assert.Equal(42, d[23]);
            d[1, 2] = "Hello";
            Assert.Equal("Hello", d[1, 2]);
            Assert.Throws<KeyNotFoundException>(() => d[1]);
            Assert.Throws<KeyNotFoundException>(() => d[2, 3]);
            Assert.Throws<RuntimeBinderException>(() => d[2] = "Test");
            Assert.Throws<RuntimeBinderException>(() => d[2, 4] = 1);
            Assert.Throws<RuntimeBinderException>(() => d["index"]);
            Assert.Throws<RuntimeBinderException>(() => d["index"] = 2);
            Assert.Throws<RuntimeBinderException>(() =>
            {
                string val = d[23];
            });
            Assert.Throws<RuntimeBinderException>(() =>
            {
                int val = d[1, 2];
            });
            Assert.Throws<RuntimeBinderException>(() => d[1, 2, 3]);
        }

        [Fact]
        public void IndexingFromStaticMember()
        {
            dynamic d = new TestDynamic();
            d[0] = 1;
            Assert.Equal(1, d[0]);
            Assert.Throws<RuntimeBinderException>(() => d[0] = "One");
            Assert.Throws<RuntimeBinderException>(() =>
            {
                string val = d[0];
            });
            Assert.Throws<RuntimeBinderException>(() => d["index"]);
        }
    }
}
