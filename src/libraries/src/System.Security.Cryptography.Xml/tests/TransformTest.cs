//
// Unit tests for Transform
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class ConcreteTransform : Transform
    {

        protected override XmlNodeList GetInnerXml()
        {
            throw new NotImplementedException();
        }

        public override object GetOutput(Type type)
        {
            return new MemoryStream();
        }

        public override object GetOutput()
        {
            throw new NotImplementedException();
        }

        public override Type[] InputTypes
        {
            get { throw new NotImplementedException(); }
        }

        public override void LoadInnerXml(global::System.Xml.XmlNodeList nodeList)
        {
            throw new NotImplementedException();
        }

        public override void LoadInput(object obj)
        {
            throw new NotImplementedException();
        }

        public override Type[] OutputTypes
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class TransformTest
    {

        [Fact]
        public void GetDigestedOutput_Null()
        {
            Assert.Throws<NullReferenceException>(() => new ConcreteTransform().GetDigestedOutput(null));
        }
    }
}
