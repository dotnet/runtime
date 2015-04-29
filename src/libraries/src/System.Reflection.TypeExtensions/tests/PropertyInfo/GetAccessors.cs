// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System;
using System.Text;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.PropertyInfoTests
{
    internal class Binding_Flags
    {
        internal static BindingFlags LookupAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        internal static BindingFlags Default = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static BindingFlags ConstructorLookupAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    }

    public class Co4389GetAccessors
    {
        private static String s_strLoc = String.Empty;

        /// data members corresponding to the properties
        protected short m_prvPropAA = -1;
        public short m_PropAA = -2;
        public float m_PropBB = -2.0f;
        private double _propCC = 123.456;

        /// declare/define all the properties here

        // MyPropAA - ReadWrite
        private String MyPropAA
        {
            get { return m_PropAA.ToString(); }			//get accessor for property {String MyPropAA}
            set { m_PropAA = Int16.Parse(value); }		//set accessor for property {String MyPropAA}
        }

        // MyPropBB - ReadWrite  property
        public double MyPropBB
        {
            get { return (double)m_PropBB; }		//get accessor for property {double MyPropBB}
            set { m_PropBB = (float)value; }		//set accessor for property {double MyPropBB}
        }

        private double MyPropCC
        {
            get { return (double)_propCC; }		//get accessor for property {double MyPropCC}
            set { _propCC = (float)value; }		//set accessor for property {double MyPropCC}
        }

        [Fact]
        public void GetAccessorsTests()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            // String str = null;
            Type clObj = null;
            PropertyInfo pInfo = null;
            PropertyInfo pInfo2 = null;

            MethodInfo mInfo = null;
            MethodInfo[] mInfoArr = null;
            ParameterInfo[] paramInfoArr = null;


            ///// [] Reflect on this class and set up propinfo structures
            clObj = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.Co4389GetAccessors");

            pInfo = clObj.GetProperty("MyPropBB", Binding_Flags.DefaultLookup);
            pInfo2 = clObj.GetProperty("MyPropCC", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.Equal("MyPropBB", pInfo.Name);
            mInfoArr = pInfo.GetAccessors();
            Assert.Equal(2, mInfoArr.Length);

            mInfo = pInfo.GetGetMethod(false); //shld get {double get$MyPropBB ()}
            Assert.True(mInfo.IsPublic);
            Assert.Equal("Double", mInfo.ReturnType.Name);
            paramInfoArr = mInfo.GetParameters();
            Assert.Equal(0, paramInfoArr.Length);

            mInfo = pInfo2.GetGetMethod(true); //should get {double get$MyPropCC ()}
            Assert.False(mInfo.IsPublic);
            Assert.Equal("Double", mInfo.ReturnType.Name);

            paramInfoArr = mInfo.GetParameters();
            Assert.Equal(0, paramInfoArr.Length);

            mInfo = pInfo.GetSetMethod(false);
            Assert.NotNull(mInfo);
            Assert.True(mInfo.IsPublic);
            Assert.Equal("Void", mInfo.ReturnType.Name);

            paramInfoArr = mInfo.GetParameters();
            Assert.Equal(1, paramInfoArr.Length);
            Assert.Equal("Double", paramInfoArr[0].ParameterType.Name);

            // GetSetMethod NonPublic
            mInfo = pInfo2.GetSetMethod(true);
            Assert.NotNull(mInfo);
            Assert.False(mInfo.IsPublic);
            Assert.Equal("Void", mInfo.ReturnType.Name);

            paramInfoArr = mInfo.GetParameters();
            Assert.Equal(1, paramInfoArr.Length);
            Assert.Equal("Double", paramInfoArr[0].ParameterType.Name);
        }
    }
}