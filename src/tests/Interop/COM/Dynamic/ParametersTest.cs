// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Runtime.InteropServices;
    using TestLibrary;

    internal class ParametersTest
    {
        // See default values in Contract.idl
        private const int Default1 = 1;
        private const int Default2 = 2;
        private const int Default3 = 3;
        private const int MissingParamId = -1;

        private dynamic obj;
        private int one;
        private int two;
        private int three;

        public ParametersTest(int seed = 123)
        {
            Type t = Type.GetTypeFromCLSID(Guid.Parse(ServerGuids.ParametersTest));
            obj = Activator.CreateInstance(t);

            Random rand = new Random(seed);
            one = rand.Next();
            two = rand.Next();
            three = rand.Next();
        }

        public void Run()
        {
            Console.WriteLine($"Running {nameof(ParametersTest)}");
            Named();
            DefaultValue();
            Optional();
            VarArgs();
            Invalid();
        }

        private void Named()
        {
            int[] expected = { one, two, three };

            // Name all arguments
            int[] ret = obj.Required(first: one, second: two, third: three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all named arguments");

            // Name some arguments
            ret = obj.Required(one, two, third: three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some named arguments");

            ret = obj.Required(one, second: two, third: three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some named arguments");

            // Name in different order
            ret = obj.Required(third: three, first: one, second: two);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with out-of-order named arguments");

            ret = obj.Required(one, third: three, second: two);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with out-of-order named arguments");

            // Invalid name
            COMException e = Assert.Throws<COMException>(() => obj.Required(one, two, invalid: three));
            const int DISP_E_UNKNOWNNAME = unchecked((int)0x80020006);
            Assert.AreEqual(DISP_E_UNKNOWNNAME, e.HResult, "Unexpected HRESULT on COMException");
        }

        private void DefaultValue()
        {
            int[] expected = { Default1, Default2, Default3 };

            // Omit all arguments
            int[] ret = obj.DefaultValue();
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all arguments omitted");

            // Specify some arguments
            expected[0] = one;
            ret = obj.DefaultValue(one);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some arguments specified");

            expected[1] = two;
            ret = obj.DefaultValue(one, two);
            Assert.AreAllEqual(expected, ret);

            // Specify all arguments
            expected[1] = two;
            expected[2] = three;
            ret = obj.DefaultValue(one, two, three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all arguments specified");

            // Named arguments
            expected[0] = Default1;
            expected[1] = Default2;
            ret = obj.DefaultValue(third: three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with named arguments");
        }

        private void Optional()
        {
            int[] expected = { MissingParamId, MissingParamId, MissingParamId };

            // Omit all arguments
            int[] ret = obj.Optional();
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all arguments omitted");

            // Specify some arguments
            expected[0] = one;
            ret = obj.Optional(one);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some arguments specified");

            expected[1] = Default2;
            ret = obj.Mixed(one);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some arguments specified");

            expected[1] = two;
            ret = obj.Optional(one, two);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some arguments specified");

            ret = obj.Mixed(one, two);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with some arguments specified");

            // Specify all arguments
            expected[1] = two;
            expected[2] = three;
            ret = obj.Optional(one, two, three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all arguments specified");

            ret = obj.Mixed(one, two, three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with all arguments specified");

            // Named arguments
            expected[1] = MissingParamId;
            ret = obj.Optional(first: one, third: three);
            Assert.AreAllEqual(expected, ret, "Unexpected result calling function with named arguments");
        }

        private void VarArgs()
        {
            VarEnum[] ret = obj.VarArgs();
            Assert.AreEqual(0, ret.Length);

            // COM server returns the type of each variant
            ret = obj.VarArgs(false);
            Assert.AreEqual(1, ret.Length);
            Assert.AreAllEqual(new [] { VarEnum.VT_BOOL }, ret);

            VarEnum[] expected = { VarEnum.VT_BSTR, VarEnum.VT_R8, VarEnum.VT_DATE, VarEnum.VT_I4 };
            ret = obj.VarArgs("s", 10d, new DateTime(), 10);
            Assert.AreEqual(expected.Length, ret.Length);
            Assert.AreAllEqual(expected, ret);
        }

        private void Invalid()
        {
            // Too few parameters
            Assert.Throws<System.Reflection.TargetParameterCountException>(() => obj.Mixed());
            Assert.Throws<System.Reflection.TargetParameterCountException>(() => obj.Required(one, two));

            // Too many parameters
            Assert.Throws<System.Reflection.TargetParameterCountException>(() => obj.Required(one, two, three, one));

            // Invalid type
            Assert.Throws<System.ArgumentException>(() => obj.Required("one", "two", "three"));
        }
    }
}
