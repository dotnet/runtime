// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Runtime.InteropServices;
    using Xunit;

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
            AssertExtensions.CollectionEqual(expected, ret);

            // Name some arguments
            ret = obj.Required(one, two, third: three);
            AssertExtensions.CollectionEqual(expected, ret);

            ret = obj.Required(one, second: two, third: three);
            AssertExtensions.CollectionEqual(expected, ret);

            // Name in different order
            ret = obj.Required(third: three, first: one, second: two);
            AssertExtensions.CollectionEqual(expected, ret);

            ret = obj.Required(one, third: three, second: two);
            AssertExtensions.CollectionEqual(expected, ret);

            // Invalid name
            COMException e = Assert.Throws<COMException>(() => obj.Required(one, two, invalid: three));
            const int DISP_E_UNKNOWNNAME = unchecked((int)0x80020006);
            Assert.Equal(DISP_E_UNKNOWNNAME, e.HResult);
        }

        private void DefaultValue()
        {
            int[] expected = { Default1, Default2, Default3 };

            // Omit all arguments
            int[] ret = obj.DefaultValue();
            AssertExtensions.CollectionEqual(expected, ret);

            // Specify some arguments
            expected[0] = one;
            ret = obj.DefaultValue(one);
            AssertExtensions.CollectionEqual(expected, ret);

            expected[1] = two;
            ret = obj.DefaultValue(one, two);
            AssertExtensions.CollectionEqual(expected, ret);

            // Specify all arguments
            expected[1] = two;
            expected[2] = three;
            ret = obj.DefaultValue(one, two, three);
            AssertExtensions.CollectionEqual(expected, ret);

            // Named arguments
            expected[0] = Default1;
            expected[1] = Default2;
            ret = obj.DefaultValue(third: three);
            AssertExtensions.CollectionEqual(expected, ret);
        }

        private void Optional()
        {
            int[] expected = { MissingParamId, MissingParamId, MissingParamId };

            // Omit all arguments
            int[] ret = obj.Optional();
            AssertExtensions.CollectionEqual(expected, ret);

            // Specify some arguments
            expected[0] = one;
            ret = obj.Optional(one);
            AssertExtensions.CollectionEqual(expected, ret);

            expected[1] = Default2;
            ret = obj.Mixed(one);
            AssertExtensions.CollectionEqual(expected, ret);

            expected[1] = two;
            ret = obj.Optional(one, two);
            AssertExtensions.CollectionEqual(expected, ret);

            ret = obj.Mixed(one, two);
            AssertExtensions.CollectionEqual(expected, ret);

            // Specify all arguments
            expected[1] = two;
            expected[2] = three;
            ret = obj.Optional(one, two, three);
            AssertExtensions.CollectionEqual(expected, ret);

            ret = obj.Mixed(one, two, three);
            AssertExtensions.CollectionEqual(expected, ret);

            // Named arguments
            expected[1] = MissingParamId;
            ret = obj.Optional(first: one, third: three);
            AssertExtensions.CollectionEqual(expected, ret);
        }

        private void VarArgs()
        {
            VarEnum[] ret = obj.VarArgs();
            Assert.Empty(ret);

            // COM server returns the type of each variant
            ret = obj.VarArgs(false);
            Assert.Single(ret);
            AssertExtensions.CollectionEqual(new [] { VarEnum.VT_BOOL }, ret);

            VarEnum[] expected = { VarEnum.VT_BSTR, VarEnum.VT_R8, VarEnum.VT_DATE, VarEnum.VT_I4 };
            ret = obj.VarArgs("s", 10d, new DateTime(), 10);
            Assert.Equal(expected.Length, ret.Length);
            AssertExtensions.CollectionEqual(expected, ret);
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
