// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types
{
    [DataContract(Namespace = "http://basic")]
    public class Point
    {
        [DataMember]
        public int X = 42;
        [DataMember]
        public int Y = 43;
    }

    [DataContract(Namespace = "http://shapes")]
    public class Circle
    {
        [DataMember]
        public Point Center = new Point();
        [DataMember]
        public int Radius = 5;
    }

    [DataContract(Namespace = "http://shapes")]
    public class Square
    {
        [DataMember]
        public Point BottomLeft = new Point();
        [DataMember]
        public int Side = 5;
    }

    public class NonSerializableSquare
    {
        public int Length = 5;

        public NonSerializableSquare(int length)
        {
            Length = length;
        }
    }

    public struct NonAttributedPersonStruct
    {
        public string firstName;
        public string lastName;
    }

    public class NonAttributedPersonClass
    {
        public string firstName = "John";
        public string lastName = "Smith";

        internal NonAttributedPersonClass()
        {
        }
    }

    public class ExtendedSquare : Square
    {
        public string lineColor = "black";
    }

    public class RecursiveCollection1 : IEnumerable<RecursiveCollection1>
    {
        public void Add(RecursiveCollection1 item)
        {
        }

        public IEnumerator<RecursiveCollection1> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class RecursiveCollection2 : IEnumerable<KeyValuePair<string, RecursiveCollection2>>
    {
        public void Add(RecursiveCollection1 item)
        {
        }

        public IEnumerator<KeyValuePair<string, RecursiveCollection2>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class Box<T>
    {
    }

    public class RecursiveCollection3 : IEnumerable<KeyValuePair<string, Box<RecursiveCollection3>>>
    {
        public void Add(RecursiveCollection1 item)
        {
        }

        public IEnumerator<KeyValuePair<string, Box<RecursiveCollection3>>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class RecursiveCollection4 : IEnumerable<KeyValuePair<string, RecursiveCollection2>>
    {
        public void Add(RecursiveCollection1 item)
        {
        }

        public IEnumerator<KeyValuePair<string, RecursiveCollection2>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
