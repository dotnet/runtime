// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public partial class Annotations : XLinqTestCase
            {
                //[Variation(Priority = 0, Desc = "Add one string annotation")]
                public void Annotations_1()
                {
                    string str = "test string";
                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(str);
                        ValidateAnnotations<string>(xo, new string[] { str });
                        TestLog.Compare(xo.Annotation<string>(), str, "Validation failed");
                        TestLog.Compare((string)xo.Annotation(typeof(string)), str, "Validation failed");
                        TestLog.Compare(xo.Annotation(typeof(object)), str, "Validation failed");
                        TestLog.Compare(xo.Annotation<object>(), str, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Add int annotation")]
                public void Annotations_2()
                {
                    int num = 123456;

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(num);
                        ValidateAnnotations<object>(xo, new object[] { num });
                        TestLog.Compare(xo.Annotation<object>(), num, "Validation failed");
                        TestLog.Compare((int)xo.Annotation(typeof(int)), num, "Validation failed");
                        TestLog.Compare(xo.Annotation(typeof(object)), num, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Add int and string annotation")]
                public void Annotations_3()
                {
                    string str = "<!@@63784sgdh111>";
                    int num = 123456;

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(str);
                        xo.AddAnnotation(num);
                        ValidateAnnotations<object>(xo, new object[] { str, num });
                        ValidateAnnotations<string>(xo, new string[] { str });
                        TestLog.Compare((int)xo.Annotation(typeof(int)), num, "Validation failed");
                        TestLog.Compare(xo.Annotation<string>(), str, "Validation failed");
                        TestLog.Compare((string)xo.Annotation(typeof(string)), str, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Add, remove and get annotation")]
                public void Annotations_4()
                {
                    string str1 = "<!@@63784sgdh111>";
                    string str2 = "<!@@63784sgdh222>";
                    int num = 123456;

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(str1);
                        xo.AddAnnotation(str2);
                        xo.AddAnnotation(num);
                        ValidateAnnotations<object>(xo, new object[] { str1, str2, num });
                        ValidateAnnotations<string>(xo, new string[] { str1, str2 });

                        xo.RemoveAnnotations<string>();
                        ValidateAnnotations<object>(xo, new object[] { num });

                        xo.RemoveAnnotations(typeof(int));
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Remove annotation without adding")]
                public void Annotations_5()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        xo.RemoveAnnotations<string>();
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");

                        xo.RemoveAnnotations(typeof(int));
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Add int annotation, remove string annotation, get int annotation")]
                public void Annotations_6()
                {
                    int num = 123456;

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(num);
                        xo.RemoveAnnotations<string>();
                        ValidateAnnotations<object>(xo, new object[] { num });
                    }
                }

                //[Variation(Priority = 0, Desc = "Add 2 string annotations, get both of them")]
                public void Annotations_7()
                {
                    string str1 = "<!@@63784sgdh111>";
                    string str2 = "sdjverqjbe4 kjvweh342$$% ";

                    foreach (XObject xo in GetXObjects())
                    {
                        string expectedStr = str1;
                        xo.AddAnnotation(str1);
                        xo.AddAnnotation(str2);
                        ValidateAnnotations<string>(xo, new string[] { str1, str2 });
                    }
                }

                //[Variation(Priority = 2, Desc = "Remove annotation twice")]
                public void Annotations_8()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        xo.RemoveAnnotations<object>();
                        xo.RemoveAnnotations<object>();
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 0, Desc = "Add generic annotation")]
                public void Annotations_9()
                {
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(d);
                        ValidateAnnotations<Dictionary<string, string>>(xo, new Dictionary<string, string>[] { d });

                        TestLog.Compare(xo.Annotation<Dictionary<string, string>>(), d, "Validation failed");
                        TestLog.Compare((Dictionary<string, string>)xo.Annotation(typeof(Dictionary<string, string>)), d, "Validation failed");
                        TestLog.Compare(xo.Annotation<object>(), d, "Validation failed");
                        TestLog.Compare(xo.Annotation(typeof(object)), d, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Remove generic annotation")]
                public void Annotations_10()
                {
                    Dictionary<string, string> d = new Dictionary<string, string>();
                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(d);
                        xo.RemoveAnnotations<Dictionary<string, string>>();
                        TestLog.Compare(CountAnnotations<Dictionary<string, string>>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 0, Desc = "Add inherited annotation")]
                public void Annotations_11()
                {
                    A a = new A();
                    B b = new B();

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(a);
                        xo.AddAnnotation(b);

                        ValidateAnnotations<A>(xo, new A[] { a, b });
                        ValidateAnnotations<B>(xo, new B[] { b });
                        TestLog.Compare(xo.Annotation<B>(), b, "Validation failed");
                        TestLog.Compare((B)xo.Annotation(typeof(B)), b, "Validation failed");
                        TestLog.Compare(xo.Annotation<A>(), a, "Validation failed");
                        TestLog.Compare((A)xo.Annotation(typeof(A)), a, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Remove inherited annotation")]
                public void Annotations_12()
                {
                    A a = new A();
                    B b = new B();

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(a);
                        xo.AddAnnotation(b);
                        xo.RemoveAnnotations<B>();

                        ValidateAnnotations<A>(xo, new A[] { a });
                        TestLog.Compare(xo.Annotation<A>(), a, "Validation failed");
                        TestLog.Compare((A)xo.Annotation(typeof(A)), a, "Validation failed");
                        TestLog.Compare(xo.Annotation<object>(), a, "Validation failed");
                        TestLog.Compare(xo.Annotation(typeof(object)), a, "Validation failed");

                        TestLog.Compare(CountAnnotations<B>(xo), 0, "unexpected number of annotations");

                        xo.RemoveAnnotations(typeof(A));
                        TestLog.Compare(CountAnnotations<A>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 1, Desc = "Null parameters throw")]
                public void Annotations_13()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        try
                        {
                            xo.AddAnnotation(null);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentNullException e)
                        {
                            TestLog.Compare(e.ParamName, "annotation", "Wrong parameter name");
                            TestLog.Compare(xo.Annotation<object>(), null, "Validation failed");
                            try
                            {
                                xo.AddAnnotation(null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException) { }
                        }
                    }
                    foreach (XObject xo in GetXObjects())
                    {
                        try
                        {
                            xo.RemoveAnnotations(null);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentNullException e)
                        {
                            TestLog.Compare(e.ParamName, "type", "Wrong parameter name");
                            try
                            {
                                xo.RemoveAnnotations(null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException) { }
                        }
                    }
                    foreach (XObject xo in GetXObjects())
                    {
                        try
                        {
                            foreach (object a in xo.Annotations(null)) ;
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentNullException e)
                        {
                            TestLog.Compare(e.ParamName, "type", "Wrong parameter name");
                            try
                            {
                                foreach (object a in xo.Annotations(null)) ;
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException) { }
                        }
                    }
                    foreach (XObject xo in GetXObjects())
                    {
                        try
                        {
                            xo.Annotation(null);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentNullException e)
                        {
                            TestLog.Compare(e.ParamName, "type", "Wrong parameter name");
                            try
                            {
                                xo.Annotation(null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException) { }
                        }
                    }
                }

                //[Variation(Priority = 1, Desc = "Typed string null parameters throw")]
                public void Annotations_14()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        try
                        {
                            xo.AddAnnotation((string)null);
                            throw new TestException(TestResult.Failed, "");
                        }
                        catch (ArgumentNullException e)
                        {
                            TestLog.Compare(e.ParamName, "annotation", "Wrong parameter name");
                            TestLog.Compare(xo.Annotation<object>(), null, "Validation failed");
                            try
                            {
                                xo.AddAnnotation((string)null);
                                throw new TestException(TestResult.Failed, "");
                            }
                            catch (ArgumentNullException) { }
                        }
                    }
                }

                //[Variation(Priority = 0, Desc = "Add annotation with same class name but different namespace")]
                public void Annotations_15()
                {
                    DifferentNamespace.A a1 = new DifferentNamespace.A();
                    A a2 = new A();

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(a1);
                        xo.AddAnnotation(a2);

                        ValidateAnnotations<DifferentNamespace.A>(xo, new DifferentNamespace.A[] { a1 });
                        ValidateAnnotations<A>(xo, new A[] { a2 });

                        TestLog.Compare(xo.Annotation<DifferentNamespace.A>(), a1, "Validation failed");
                        TestLog.Compare((DifferentNamespace.A)xo.Annotation(typeof(DifferentNamespace.A)), a1, "Validation failed");
                        TestLog.Compare(xo.Annotation<A>(), a2, "Validation failed");
                        TestLog.Compare((A)xo.Annotation(typeof(A)), a2, "Validation failed");
                    }
                }

                //[Variation(Priority = 0, Desc = "Remove annotation with same class name but different namespace")]
                public void Annotations_16()
                {
                    DifferentNamespace.A a1 = new DifferentNamespace.A();
                    A a2 = new A();

                    foreach (XObject xo in GetXObjects())
                    {
                        xo.AddAnnotation(a1);
                        xo.AddAnnotation(a2);
                        xo.RemoveAnnotations<DifferentNamespace.A>();

                        TestLog.Compare(CountAnnotations<DifferentNamespace.A>(xo), 0, "unexpected number of annotations");
                        ValidateAnnotations<A>(xo, new A[] { a2 });
                        TestLog.Compare(xo.Annotation<A>(), a2, "Validation failed");
                        TestLog.Compare((A)xo.Annotation(typeof(A)), a2, "Validation failed");

                        xo.RemoveAnnotations(typeof(A));
                        TestLog.Compare(CountAnnotations<A>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Remove annotations of different types and different XObjects")]
                public void Annotations_17()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        AddAnnotation(xo);
                        foreach (Type type in GetTypes())
                        {
                            RemoveAnnotations(xo, type);
                        }
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Remove twice annotations of different types and different XObjects")]
                public void Annotations_18()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        foreach (Type type in GetTypes())
                        {
                            RemoveAnnotations(xo, type);
                        }
                        foreach (Type type in GetTypes())
                        {
                            RemoveAnnotations(xo, type);
                        }
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Add twice, remove once annotations of different types and different XObjects")]
                public void Annotations_19()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        AddAnnotation(xo);
                        int count = CountAnnotations<object>(xo);

                        AddAnnotation(xo);
                        TestLog.Compare(CountAnnotations<object>(xo), count * 2, "unexpected number of annotations");

                        foreach (Type type in GetTypes())
                        {
                            RemoveAnnotations(xo, type);
                        }
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Add annotation to XElement, and clone this element to another subtree, get null annotation")]
                public void Annotations_20()
                {
                    string str = "element1111";

                    XElement element1 = new XElement("e", new XAttribute("a", "value"));
                    element1.AddAnnotation(str);
                    XElement element2 = new XElement(element1);

                    ValidateAnnotations<string>(element1, new string[] { str });
                    TestLog.Compare(element1.Annotation<string>(), str, "Validation failed");
                    TestLog.Compare(element1.Annotation(typeof(string)), str, "Validation failed");

                    TestLog.Compare(CountAnnotations<string>(element2), 0, "unexpected number of annotations");
                }

                //[Variation(Priority = 2, Desc = "Add annotation to XElement, and remove this element, get annotation")]
                public void Annotations_21()
                {
                    string str = "element1111";
                    XElement root = new XElement("root");
                    XElement element = new XElement("elem1");
                    root.Add(element);
                    element.AddAnnotation(str);
                    element.Remove();

                    ValidateAnnotations<string>(element, new string[] { str });
                    TestLog.Compare(element.Annotation<string>(), str, "Validation failed");
                    TestLog.Compare(element.Annotation(typeof(string)), str, "Validation failed");
                }

                //[Variation(Priority = 0, Desc = "Add annotation to parent and child, valIdate annotations for each XObjects")]
                public void Annotations_22()
                {
                    string str1 = "root 1111";
                    string str2 = "element 1111";

                    XElement root = new XElement("root");
                    XElement element = new XElement("elem1");
                    root.Add(element);
                    root.AddAnnotation(str1);
                    element.AddAnnotation(str2);

                    ValidateAnnotations<string>(root, new string[] { str1 });
                    TestLog.Compare(root.Annotation<string>(), str1, "Validation failed");
                    TestLog.Compare(root.Annotation(typeof(string)), str1, "Validation failed");

                    ValidateAnnotations<string>(element, new string[] { str2 });
                    TestLog.Compare(element.Annotation<string>(), str2, "Validation failed");
                    TestLog.Compare(element.Annotation(typeof(string)), str2, "Validation failed");
                }

                //[Variation(Priority = 0, Desc = "Add annotations, remove using type object")]
                public void Annotations_23()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        AddAnnotation(xo);
                        RemoveAnnotations(xo, typeof(object));
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 2, Desc = "Enumerate annotations without adding")]
                public void Annotations_24()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        object annotation1 = xo.Annotation(typeof(object));
                        TestLog.Compare(annotation1, null, "Annotation should be null");
                        object annotation2 = xo.Annotation<object>();
                        TestLog.Compare(annotation2, null, "Annotation should be null");

                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                        TestLog.Compare(CountAnnotations<string>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 0, Desc = "Remove annotations using type object")]
                public void Annotations_25()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        AddAnnotation(xo);
                        RemoveAnnotations<object>(xo);
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }

                //[Variation(Priority = 0, Desc = "Remove twice annotations without adding using type object")]
                public void Annotations_26()
                {
                    foreach (XObject xo in GetXObjects())
                    {
                        RemoveAnnotations<object>(xo);
                        RemoveAnnotations<object>(xo);
                        TestLog.Compare(CountAnnotations<object>(xo), 0, "unexpected number of annotations");
                    }
                }


                //
                // helpers
                //

                public static List<XObject> GetXObjects()
                {
                    List<XObject> aXObject = new List<XObject>();

                    aXObject.Add(new XDocument());
                    aXObject.Add(new XAttribute("attr", "val"));
                    aXObject.Add(new XElement("elem1"));
                    aXObject.Add(new XText("text1"));
                    aXObject.Add(new XComment("comment1"));
                    aXObject.Add(new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"));
                    aXObject.Add(new XCData("cdata cdata"));
                    aXObject.Add(new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1dtd1", "dtd1dtd1dtd1dtd1"));

                    return aXObject;
                }

                public static object[] GetObjects()
                {
                    object[] aObject = new object[] {
                        new A(),
                        new B(),
                        new DifferentNamespace.A(),
                        new DifferentNamespace.B(),
                        "stringstring",
                        12345,
                        new Dictionary<string, string>(),
                        new XDocument(),
                        new XAttribute("attr", "val"),
                        new XElement("elem1"),
                        new XText("text1 text1"),
                        new XComment("comment1 comment1"),
                        new XProcessingInstruction("pi1", "pi1pi1pi1pi1pi1"),
                        new XCData("cdata cdata"),
                        new XDeclaration("234", "UTF-8", "yes"),
                        XNamespace.Xmlns,
                        //new XStreamingElement("elementSequence"), 
                        new XDocumentType("dtd1", "dtd1dtd1dtd1", "dtd1 dtd1","dtd1 dtd1 dtd1 ")};

                    return aObject;
                }

                public static void ValidateAnnotations<T>(XObject xo, T[] values) where T : class
                {
                    //
                    // use inefficent n^2 algorithm, which is OK for our testing purposes
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

                public static int CountAnnotations<T>(XObject xo) where T : class
                {
                    //
                    // count twice, using generics and non-generics API to increase coverage
                    //

                    int count1 = 0;
                    foreach (T annotation in xo.Annotations(typeof(T)))
                    {
                        ++count1;
                    }

                    int count2 = 0;
                    foreach (T annotation in xo.Annotations<T>())
                    {
                        ++count2;
                    }

                    //
                    // counts better to be the same
                    //

                    TestLog.Compare(
                        count1,
                        count2,
                        "Generics and non-generics annotations enumerations returned different number of objects");

                    return count1;
                }

                public static void AddAnnotation(XObject xo)
                {
                    foreach (object o in GetObjects())
                    {
                        xo.AddAnnotation(o);
                    }
                }

                public static void RemoveAnnotations(XObject xo, Type type)
                {
                    xo.RemoveAnnotations(type);
                }

                public static void RemoveAnnotations<T>(XObject xo) where T : class
                {
                    xo.RemoveAnnotations<T>();
                }

                public static Type[] GetTypes()
                {
                    Type[] types = new Type[]
                        {
                            typeof(string),
                            typeof(int),
                            typeof(Dictionary<string, string>),
                            typeof(A),
                            typeof(B),
                            typeof(DifferentNamespace.A),
                            typeof(DifferentNamespace.B),
                            typeof(XAttribute),
                            typeof(XElement),
                            typeof(Extensions),
                            typeof(XDocument),
                            typeof(XText),
                            typeof(XName),
                            typeof(XComment),
                            typeof(XProcessingInstruction),
                            typeof(XCData),
                            typeof(XDeclaration),
                            typeof(XNamespace),
                            //typeof(XStreamingElement), 
                            typeof(XDocumentType)
                        };

                    return types;
                }

                public class A { }
                public class B : A { }
            }
        }
    }

    namespace DifferentNamespace
    {
        public class A { }
        public class B : A { }
    }
}