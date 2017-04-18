// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class Annotations
    {
        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddStringAnnotation(XObject xo)
        {
            const string expected = "test string";
            xo.AddAnnotation(expected);
            ValidateAnnotations(xo, new string[] { expected });
            Assert.Equal(expected, xo.Annotation<string>());
            Assert.Equal(expected, (string)xo.Annotation(typeof(string)));
            Assert.Equal(expected, xo.Annotation(typeof(object)));
            Assert.Equal(expected, xo.Annotation<object>());
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddIntAnnotation(XObject xo)
        {
            const int expected = 123456;
            xo.AddAnnotation(expected);
            ValidateAnnotations(xo, new object[] { expected });
            Assert.Equal(expected, xo.Annotation<object>());
            Assert.Equal(expected, (int)xo.Annotation(typeof(int)));
            Assert.Equal(expected, xo.Annotation(typeof(object)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddIntAndStringAnnotation(XObject xo)
        {
            const string expectedStr = "<!@@63784sgdh111>";
            const int expectedNum = 123456;
            xo.AddAnnotation(expectedStr);
            xo.AddAnnotation(expectedNum);
            ValidateAnnotations(xo, new object[] { expectedStr, expectedNum });
            ValidateAnnotations(xo, new string[] { expectedStr });
            Assert.Equal(expectedNum, (int)xo.Annotation(typeof(int)));
            Assert.Equal(expectedStr, xo.Annotation<string>());
            Assert.Equal(expectedStr, (string)xo.Annotation(typeof(string)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddRemoveGetAnnotation(XObject xo)
        {
            string str1 = "<!@@63784sgdh111>";
            string str2 = "<!@@63784sgdh222>";
            int num = 123456;

            xo.AddAnnotation(str1);
            xo.AddAnnotation(str2);
            xo.AddAnnotation(num);
            ValidateAnnotations(xo, new object[] { str1, str2, num });
            ValidateAnnotations(xo, new string[] { str1, str2 });

            xo.RemoveAnnotations<string>();
            ValidateAnnotations(xo, new object[] { num });

            xo.RemoveAnnotations(typeof(int));
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveAnnotationWithoutAdding(XObject xo)
        {
            xo.RemoveAnnotations<string>();
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));

            xo.RemoveAnnotations(typeof(int));
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddIntRemoveStringGetIntAnnotation(XObject xo)
        {
            const int num = 123456;

            xo.AddAnnotation(num);
            xo.RemoveAnnotations<string>();
            ValidateAnnotations(xo, new object[] { num });
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddMultipleStringAnnotations(XObject xo)
        {
            const string str1 = "<!@@63784sgdh111>";
            const string str2 = "sdjverqjbe4 kjvweh342$$% ";

            xo.AddAnnotation(str1);
            xo.AddAnnotation(str2);
            ValidateAnnotations(xo, new string[] { str1, str2 });
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveAnnotationTwice(XObject xo)
        {
            xo.RemoveAnnotations<object>();
            xo.RemoveAnnotations<object>();
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddGenericAnnotation(XObject xo)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            xo.AddAnnotation(d);
            ValidateAnnotations(xo, new Dictionary<string, string>[] { d });

            Assert.Equal(d, xo.Annotation<Dictionary<string, string>>());
            Assert.Equal(d, (Dictionary<string, string>)xo.Annotation(typeof(Dictionary<string, string>)));
            Assert.Equal(d, xo.Annotation<object>());
            Assert.Equal(d, xo.Annotation(typeof(object)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveGenericAnnotation(XObject xo)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            xo.AddAnnotation(d);
            xo.RemoveAnnotations<Dictionary<string, string>>();
            Assert.Equal(expected: 0, actual: CountAnnotations<Dictionary<string, string>>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddInheritedAnnotation(XObject xo)
        {
            A a = new A();
            B b = new B();

            xo.AddAnnotation(a);
            xo.AddAnnotation(b);

            ValidateAnnotations(xo, new A[] { a, b });
            ValidateAnnotations(xo, new B[] { b });
            Assert.Equal(b, xo.Annotation<B>());
            Assert.Equal(b, (B)xo.Annotation(typeof(B)));
            Assert.Equal(a, xo.Annotation<A>());
            Assert.Equal(a, (A)xo.Annotation(typeof(A)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveInheritedAnnotation(XObject xo)
        {
            A a = new A();
            B b = new B();

            xo.AddAnnotation(a);
            xo.AddAnnotation(b);
            xo.RemoveAnnotations<B>();

            ValidateAnnotations(xo, new A[] { a });
            Assert.Equal(a, xo.Annotation<A>());
            Assert.Equal(a, (A)xo.Annotation(typeof(A)));
            Assert.Equal(a, xo.Annotation<object>());
            Assert.Equal(a, xo.Annotation(typeof(object)));

            Assert.Equal(0, CountAnnotations<B>(xo));

            xo.RemoveAnnotations(typeof(A));
            Assert.Equal(0, CountAnnotations<A>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddNull(XObject xo)
        {
            AssertExtensions.Throws<ArgumentNullException>("annotation", () => xo.AddAnnotation(null));
            Assert.Null(xo.Annotation<object>());
            AssertExtensions.Throws<ArgumentNullException>("annotation", () => xo.AddAnnotation(null));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveNull(XObject xo)
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.RemoveAnnotations(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.RemoveAnnotations(null));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void GetAllNull(XObject xo)
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.Annotations(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.Annotations(null));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void GetOneNull(XObject xo)
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.Annotation(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => xo.Annotation(null));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddNullString(XObject xo)
        {
            AssertExtensions.Throws<ArgumentNullException>("annotation", () => xo.AddAnnotation((string)null));
            Assert.Null(xo.Annotation<object>());
            AssertExtensions.Throws<ArgumentNullException>("annotation", () => xo.AddAnnotation((string)null));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddAnnotationWithSameClassNameButDifferentNamespace(XObject xo)
        {
            DifferentNamespace.A a1 = new DifferentNamespace.A();
            A a2 = new A();

            xo.AddAnnotation(a1);
            xo.AddAnnotation(a2);

            ValidateAnnotations(xo, new DifferentNamespace.A[] { a1 });
            ValidateAnnotations(xo, new A[] { a2 });

            Assert.Equal(a1, xo.Annotation<DifferentNamespace.A>());
            Assert.Equal(a1, (DifferentNamespace.A)xo.Annotation(typeof(DifferentNamespace.A)));
            Assert.Equal(a2, xo.Annotation<A>());
            Assert.Equal(a2, (A)xo.Annotation(typeof(A)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveAnnotationWithSameClassNameButDifferentNamespace(XObject xo)
        {
            DifferentNamespace.A a1 = new DifferentNamespace.A();
            A a2 = new A();

            xo.AddAnnotation(a1);
            xo.AddAnnotation(a2);
            xo.RemoveAnnotations<DifferentNamespace.A>();

            Assert.Equal(expected: 0, actual: CountAnnotations<DifferentNamespace.A>(xo));
            ValidateAnnotations<A>(xo, new A[] { a2 });
            Assert.Equal(a2, xo.Annotation<A>());
            Assert.Equal(a2, (A)xo.Annotation(typeof(A)));

            xo.RemoveAnnotations(typeof(A));
            Assert.Equal(expected: 0, actual: CountAnnotations<A>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveAnnotationsOfDifferentTypesAndDifferentXObjects(XObject xo)
        {
            AddAnnotation(xo);
            foreach (Type type in GetTypes())
            {
                RemoveAnnotations(xo, type);
            }

            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveTwiceAnnotationsOfDifferentTypesAndDifferentXObjects(XObject xo)
        {
            foreach (Type type in GetTypes())
            {
                RemoveAnnotations(xo, type);
            }
            foreach (Type type in GetTypes())
            {
                RemoveAnnotations(xo, type);
            }
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddTwiceRemoveOnceAnnotationsOfDifferentTypesAndDifferentXObjects(XObject xo)
        {
            AddAnnotation(xo);
            int count = CountAnnotations<object>(xo) * 2;

            AddAnnotation(xo);
            Assert.Equal(expected: count, actual: CountAnnotations<object>(xo));

            foreach (Type type in GetTypes())
            {
                RemoveAnnotations(xo, type);
            }
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Fact]
        public void AddAnnotationAndClone()
        {
            // Add annotation to XElement, and clone this element to another subtree, get null annotation
            const string expected = "element1111";

            XElement element1 = new XElement("e", new XAttribute("a", "value"));
            element1.AddAnnotation(expected);
            XElement element2 = new XElement(element1);

            ValidateAnnotations(element1, new string[] { expected });
            Assert.Equal(expected, element1.Annotation<string>());
            Assert.Equal(expected, element1.Annotation(typeof(string)));

            Assert.Equal(0, CountAnnotations<string>(element2));
        }

        [Fact]
        public void AddAnnotationXElementRemoveAndGet()
        {
            // Add annotation to XElement, and remove this element, get annotation
            const string expected = "element1111";
            XElement root = new XElement("root");
            XElement element = new XElement("elem1");
            root.Add(element);
            element.AddAnnotation(expected);
            element.Remove();

            ValidateAnnotations(element, new string[] { expected });
            Assert.Equal(expected, element.Annotation<string>());
            Assert.Equal(expected, element.Annotation(typeof(string)));
        }

        [Fact]
        public void AddAnnotationToParentAndChildAndValIdate()
        {
              // Add annotation to parent and child, valIdate annotations for each XObjects
            string str1 = "root 1111";
            string str2 = "element 1111";

            XElement root = new XElement("root");
            XElement element = new XElement("elem1");
            root.Add(element);
            root.AddAnnotation(str1);
            element.AddAnnotation(str2);

            ValidateAnnotations(root, new string[] { str1 });
            Assert.Equal(str1, root.Annotation<string>());
            Assert.Equal(str1, root.Annotation(typeof(string)));

            ValidateAnnotations(element, new string[] { str2 });
            Assert.Equal(str2, element.Annotation<string>());
            Assert.Equal(str2, element.Annotation(typeof(string)));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void AddAnnotationsAndRemoveOfTypeObject(XObject xo)
        {
            AddAnnotation(xo);
            RemoveAnnotations(xo, typeof(object));
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void EnumerateAnnotationsWithoutAdding(XObject xo)
        {
            Assert.Null(xo.Annotation(typeof(object)));
            Assert.Null(xo.Annotation<object>());

            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
            Assert.Equal(expected: 0, actual: CountAnnotations<string>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveAnnotationsUsingTypeObject(XObject xo)
        {
            AddAnnotation(xo);
            RemoveAnnotations<object>(xo);
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        [Theory]
        [MemberData(nameof(GetXObjects))]
        public void RemoveTwiceAnnotationsWithoutAddingUsingTypeObject(XObject xo)
        {
            RemoveAnnotations<object>(xo);
            RemoveAnnotations<object>(xo);
            Assert.Equal(expected: 0, actual: CountAnnotations<object>(xo));
        }

        //
        // helpers
        //

        public static IEnumerable<object[]> GetXObjects()
        {
            yield return new object[] { new XDocument() };
            yield return new object[] { new XAttribute("attr", "val") };
            yield return new object[] { new XElement("elem1") };
            yield return new object[] { new XText("text1") };
            yield return new object[] { new XComment("comment1") };
            yield return new object[] { new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1") };
            yield return new object[] { new XCData("cdata cdata") };
            yield return new object[] { new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1dtd1", "dtd1dtd1dtd1dtd1") };
        }

        public static object[] GetObjects()
        {
            object[] aObject = new object[]
            {
                new A(), new B(), new DifferentNamespace.A(), new DifferentNamespace.B(), "stringstring", 12345,
                new Dictionary<string, string>(), new XDocument(), new XAttribute("attr", "val"), new XElement("elem1"),
                new XText("text1 text1"), new XComment("comment1 comment1"),
                new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"), new XCData("cdata cdata"),
                new XDeclaration("234", "UTF-8", "yes"), XNamespace.Xmlns,
                //new XStreamingElement("elementSequence"), 
                new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1 dtd1", "dtd1 dtd1 dtd1 ")
            };

            return aObject;
        }

        private static void ValidateAnnotations<T>(XObject xo, T[] values) where T : class
        {
            //
            // use inefficient n^2 algorithm, which is OK for our testing purposes
            // assumes that all items are unique
            //

            int count = CountAnnotations<T>(xo);
            TestLog.Compare(count, values.Length, "unexpected number of annotations");

            foreach (T value in values)
            {
                //
                // use non-generics enum first
                //

                bool found = false;
                foreach (T annotation in xo.Annotations(typeof(T)))
                {
                    if (annotation.Equals(value))
                    {
                        found = true;
                        break;
                    }
                }
                TestLog.Compare(found, "didn't find value using non-generic enumeration");

                //
                // now double check with generics one
                //

                found = false;
                foreach (T annotation in xo.Annotations<T>())
                {
                    if (annotation.Equals(value))
                    {
                        found = true;
                        break;
                    }
                }

                TestLog.Compare(found, "didn't find value using generic enumeration");
            }
        }

        private static int CountAnnotations<T>(XObject xo) where T : class
        {
            int count = xo.Annotations(typeof(T)).Count();
            Assert.Equal(count, xo.Annotations<T>().Count());
            // Generics and non-generics annotations enumerations returned different number of objects

            return count;
        }

        private static void AddAnnotation(XObject xo)
        {
            foreach (object o in GetObjects())
            {
                xo.AddAnnotation(o);
            }
        }

        private static void RemoveAnnotations(XObject xo, Type type)
        {
            xo.RemoveAnnotations(type);
        }

        private static void RemoveAnnotations<T>(XObject xo) where T : class
        {
            xo.RemoveAnnotations<T>();
        }

        private static Type[] GetTypes()
        {
            Type[] types = new Type[]
            {
                typeof(string), typeof(int), typeof(Dictionary<string, string>), typeof(A), typeof(B),
                typeof(DifferentNamespace.A), typeof(DifferentNamespace.B), typeof(XAttribute), typeof(XElement),
                typeof(Extensions), typeof(XDocument), typeof(XText), typeof(XName), typeof(XComment),
                typeof(XProcessingInstruction), typeof(XCData), typeof(XDeclaration), typeof(XNamespace),
                //typeof(XStreamingElement), 
                typeof(XDocumentType)
            };

            return types;
        }

        public class A
        {
        }

        public class B : A
        {
        }
    }

    namespace DifferentNamespace
    {
        public class A { }
        public class B : A { }
    }
}
