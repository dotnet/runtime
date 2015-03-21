// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using Xunit;

internal static class Utils
{
    internal static void Equal<T>(T[] x, T[] y, Func<T, T, bool> f)
    {
        if (x == null)
        {
            Assert.Null(y);
        }
        else if (y == null)
        {
            Assert.Null(x);
        }
        else
        {
            Assert.True(x.Length == y.Length);

            for (int i = 0; i < x.Length; i++)
            {
                Assert.True(f(x[i], y[i]));
            }
        }
    }

    private static Dictionary<string, string> s_prefixToNamespaceDesk = new Dictionary<string, string>();
    private static Dictionary<string, string> s_prefixToNamespaceCoreCLR = new Dictionary<string, string>();

    internal struct CompareResult
    {
        public bool Equal { get; set; }
        public string ErrorMessage { get; set; }
    }

    internal static int Min(int a, int b)
    {
        if (a < b) { return a; }
        return b;
    }

    internal static int Max(int a, int b)
    {
        if (a > b) { return a; }
        return b;
    }

    private static CompareResult CompareString(string expected, string actual)
    {
        if (expected == null && actual == null)
        {
            return new CompareResult { Equal = true };
        }

        if (expected == null)
        {
            return new CompareResult { ErrorMessage = "expected null, but actual was:\n" + actual };
        }

        if (actual == null)
        {
            return new CompareResult { ErrorMessage = "actual was null, but was expecting:\n" + expected };
        }

        int commonLength = Min(actual.Length, expected.Length);

        for (int currentIndex = 0; currentIndex < commonLength; ++currentIndex)
        {
            char expectedChar = expected[currentIndex];
            char actualChar = actual[currentIndex];

            if (expectedChar != actualChar)
            {
                int from = Max(currentIndex - 10, 0);
                int errPosition = currentIndex - from;
                int to = Min(currentIndex + 20, commonLength);
                string message = string.Format("strings differ at index {0}\n{3}↓\n[expected]:{1}\n[actual  ]:{2}\n{3}↑\n[Expected (with length={4})]:\n{5}\n[Actual (with length={6})]:\n{7}",
                    currentIndex,
                    expected.Substring(from, to - from),
                    actual.Substring(from, to - from),
                    new string(' ', errPosition + " expected :".Length),
                    expected.Length,
                    expected,
                    actual.Length,
                    actual);
                return new CompareResult { ErrorMessage = message };
            }
        }

        if (actual.Length != commonLength)
        {
            return new CompareResult { ErrorMessage = "actual is longer. The unwanted suffix is:\n" + actual.Substring(commonLength) };
        }

        if (expected.Length != commonLength)
        {
            return new CompareResult { ErrorMessage = "expected is longer. The missing suffix is:\n" + expected.Substring(commonLength) };
        }

        return new CompareResult { Equal = true };
    }

    internal static CompareResult Compare(string expected, string actual, bool runSmartXmlComparerOnFailure = true)
    {
        // for CORECLR we get different xml hence we have updated code for smartyXMLcomparision

        CompareResult stringcompare = CompareString(expected, actual);

        if (runSmartXmlComparerOnFailure == true)
        {
            if ((stringcompare.Equal != true) && (!string.IsNullOrEmpty(stringcompare.ErrorMessage)))
            {
                Console.WriteLine("Basic basline XML comparison failed with the error : {0}\n. Running the smart XML comparer", stringcompare.ErrorMessage);
                if (!SmartXmlCompare(expected, actual))
                {
                    return new CompareResult { ErrorMessage = "XML comparision is also failing" };
                }
                else
                {
                    return new CompareResult { Equal = true };
                }
            }
        }
        return stringcompare;
    }

    public static bool SmartXmlCompare(string expected, string actual)
    {
        XElement deskXElem = XElement.Parse(expected);
        XElement coreCLRXElem = XElement.Parse(actual);

        s_prefixToNamespaceDesk.Clear();
        s_prefixToNamespaceCoreCLR.Clear();

        return CompareXElements(deskXElem, coreCLRXElem);
    }

    private static bool CompareXElements(XElement baselineXElement, XElement actualXElement)
    {
        // Check whether the XName is the same, this can be done by comparing the two XNames.

        if (!baselineXElement.Name.Equals(actualXElement.Name))
        {
            // Two nodes could be same even if their localName is not the same.
            // For example- 

            // Desktop
            //<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4 xmlns:i="http://www.w3.org/2001/XMLSchema-instance" z:Id="i1" xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/" xmlns="http://schemas.datacontract.org/2004/07/SerializationTypes">
            //  <genericData1 z:Id="i2">
            //    <BaseData></BaseData>
            //    <DerivedData></DerivedData>
            //  </genericData1>
            //  <genericData2 z:Id="i3">
            //    <BaseData></BaseData>
            //    <DerivedData></DerivedData>
            //  </genericData2>
            //</GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4>

            // vs CoreCLR.
            //<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2RkuXKXCQ z:Id="i1" xmlns="http://schemas.datacontract.org/2004/07/SerializationTypes" xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/">
            //  <genericData1 z:Id="i2">
            //    <BaseData />
            //    <DerivedData />
            //  </genericData1>
            //  <genericData2 z:Id="i3">
            //    <BaseData />
            //    <DerivedData />
            //  </genericData2>
            //</GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2RkuXKXCQ>

            // Note the incorrect padding in the end of GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2RkuXKXCQ
            // The difference is MD5 hashcode applied to the Type.FullName and is because full typeName in desktop and CoreCLR returns different value.

            // Hack for the above reason.
            int deskIdx, coreCLRIdx;
            if (-1 != (deskIdx = baselineXElement.Name.LocalName.IndexOf("zbP0weY4")))
            {
                // Check if the CoreCLR string contains the other.
                if (-1 != (coreCLRIdx = actualXElement.Name.LocalName.IndexOf("RkuXKXCQ")))
                {
                    // Check whether the substring before this matches
                    if (0 == String.Compare(baselineXElement.Name.LocalName.Substring(0, deskIdx), actualXElement.Name.LocalName.Substring(0, coreCLRIdx)))
                    {
                        // Check if the namespace matched.
                        if (baselineXElement.Name.Namespace.Equals(actualXElement.Name.Namespace)) return true;
                    }
                }
            }
            string message = string.Format("Namespace difference \n[expected]:{0}\n[actual  ]:{1}\n Padding expected elements is {2}",
                    baselineXElement.Name.Namespace,
                    actualXElement.Name.Namespace,
                    actualXElement.Name.LocalName
                    );
            Console.WriteLine("Either padding is different or namespace is not matching.\n" + message);

            return false;
        }

        //Comparising attributes
        XAttribute[] deskAtrs = baselineXElement.Attributes().OrderBy(m => m.Value).ToArray();
        XAttribute[] coreCLRAtrs = actualXElement.Attributes().OrderBy(m => m.Value).ToArray();

        if (deskAtrs.Length != coreCLRAtrs.Length)
        {
            Console.WriteLine("Number of attributes differ.Expected number of attributes are " + deskAtrs.Length.ToString() + " Actual number of attributes are " + coreCLRAtrs.Length.ToString());
            return false;
        }

        // At this point the attributes should be all in the same order.
        for (int i = 0; i < deskAtrs.Length; i++)
        {
            if (deskAtrs[i].IsNamespaceDeclaration != coreCLRAtrs[i].IsNamespaceDeclaration)
            {
                Console.WriteLine("Either expected attribute {0} is not namespace declaration or actual attribute {1}", deskAtrs[i].ToString(), coreCLRAtrs[i].ToString());
                return false;
            }
            if (deskAtrs[i].IsNamespaceDeclaration)
            {
                if (0 != String.Compare(deskAtrs[i].Name.NamespaceName, coreCLRAtrs[i].Name.NamespaceName))
                {
                    Console.WriteLine("Namspaces are different.Expected {0} namespace doesn't match with actual {1} namespace ", deskAtrs[i].Name.NamespaceName, coreCLRAtrs[i].Name.NamespaceName);
                    return false;
                }
                if (0 != String.Compare(deskAtrs[i].Value, coreCLRAtrs[i].Value))
                {
                    Console.WriteLine("Attribute values are different. Expected {0} attribute values doesn't match with actual {1} attribute value.", deskAtrs[i].Value, coreCLRAtrs[i].Value);
                    return false;
                }

                // Update the dictionaries
                s_prefixToNamespaceDesk[deskAtrs[i].Name.LocalName] = deskAtrs[i].Value;
                s_prefixToNamespaceCoreCLR[coreCLRAtrs[i].Name.LocalName] = coreCLRAtrs[i].Value;
            }
            else
            {
                if (!deskAtrs[i].Name.Equals(coreCLRAtrs[i].Name))
                {
                    Console.WriteLine("Attribute names are different. Expected name is {0} but actual name is {1}", deskAtrs[i].Name, coreCLRAtrs[i].Name);
                    return false;
                }

                string deskPrefix, coreCLRPrefix;
                if (IsPrefixedAttributeValue(deskAtrs[i].Value, out deskPrefix))
                {
                    if (IsPrefixedAttributeValue(coreCLRAtrs[i].Value, out coreCLRPrefix))
                    {
                        // Check if they both have the same namespace.
                        XNamespace deskns = baselineXElement.GetNamespaceOfPrefix(deskPrefix);
                        XNamespace coreclrns = actualXElement.GetNamespaceOfPrefix(coreCLRPrefix);
                        if (!deskns.Equals(coreclrns))
                        {
                            Console.WriteLine("XML namespace of attribute is different. Expected is {0} but actual is {1}", deskns.NamespaceName, coreclrns.NamespaceName);
                            return false;
                        }
                        // Update the dictionaries
                        s_prefixToNamespaceDesk[deskPrefix] = deskns.NamespaceName;
                        s_prefixToNamespaceCoreCLR[coreCLRPrefix] = coreclrns.NamespaceName;
                    }
                    else
                    {
                        Console.WriteLine("Either expected {0} or actual {1} attribute value doesn't have prefix :", deskAtrs[i].Value, coreCLRAtrs[i].Value);
                        return false;
                    }
                }
            }
        }

        if (!CompareValue(baselineXElement.Value, actualXElement.Value)) return false;


        // Serialized values can only have XElement and XText and hence we do not traverse the complete node structures and only the descendants.
        XElement[] deskChildElems = baselineXElement.Descendants().OrderBy(m => m.Name.NamespaceName).ToArray();
        XElement[] coreCLRChildElems = actualXElement.Descendants().OrderBy(m => m.Name.NamespaceName).ToArray();

        for (int i = 0; i < deskChildElems.Length; i++)
        {
            if (!CompareXElements(deskChildElems[i], coreCLRChildElems[i])) return false;
        }

        // If we have reached here, XML is same.
        return true;
    }


    private static bool CompareValue(string deskElemValue, string coreCLRElemValue)
    {
        if (deskElemValue.Equals(coreCLRElemValue)) return true;

        // For text of the form 
        // <z:QName xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/" xmlns:a="def">a:abc</z:QName>

        // In the above XML text the XElement.Value is a:abc which in CoreCLR could be something like d1p1:abc
        // and hence we value check will fail.
        // To mitigate this we store the namespaces from the parent XElement and use it to check the actual value.
        string deskPrefix, coreCLRPrefix;

        if (IsPrefixedAttributeValue(deskElemValue, out deskPrefix))
        {
            if (IsPrefixedAttributeValue(coreCLRElemValue, out coreCLRPrefix))
            {
                // Check whether the prefixes have the right namespaces attached.
                string deskNs, coreCLRNs;
                if (s_prefixToNamespaceDesk.TryGetValue(deskPrefix, out deskNs))
                {
                    if (s_prefixToNamespaceCoreCLR.TryGetValue(coreCLRPrefix, out coreCLRNs))
                    {
                        if (deskNs.Equals(coreCLRNs))
                        {
                            // Also we check that the rest of the strings match.
                            if (0 == String.Compare(deskElemValue.Substring(deskPrefix.Length), coreCLRElemValue.Substring(coreCLRPrefix.Length)))
                                return true;
                        }
                    }
                }
            }
            Console.WriteLine("Attribute value {0} has empty prefix value before :", coreCLRElemValue);
            return false;
        }
        Console.WriteLine("Attribute value {0} has empty prefix value before :", deskElemValue);
        return false;
    }

    private static bool IsPrefixedAttributeValue(string atrValue, out string localPrefix)
    {
        int prefixIndex = atrValue.IndexOf(":");
        if (prefixIndex != -1)
        {
            localPrefix = atrValue.Substring(0, prefixIndex);
            return true;
        }
        else
        {
            localPrefix = String.Empty;
        }
        Console.WriteLine("Given attribute value {0} does not have any prefix value before :", atrValue);
        return false;
    }
}
