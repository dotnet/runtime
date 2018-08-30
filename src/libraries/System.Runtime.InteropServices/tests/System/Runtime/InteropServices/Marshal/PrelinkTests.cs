// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PrelinkTests
    {
        [Fact]
        public void Prelink_ValidMethod_Success()
        {
            MethodInfo method = typeof(PrelinkTests).GetMethod(nameof(Prelink_ValidMethod_Success));
            Marshal.Prelink(method);
            Marshal.Prelink(method);
        }

        [Fact]
        public void Prelink_RuntimeSuppliedMethod_Success()
        {
            MethodInfo method = typeof(Math).GetMethod(nameof(Math.Abs), new Type[] { typeof(double) });
            Marshal.Prelink(method);
            Marshal.Prelink(method);
        }

        [Fact]
        public void Prelink_NullMethod_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("m", () => Marshal.Prelink(null));
        }

        [Fact]
        public void Prelink_NonRuntimeMethod_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("m", null, () => Marshal.Prelink(new NonRuntimeMethodInfo()));
        }
        
        public class NonRuntimeMethodInfo : MethodInfo
        {
            public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

            public override MethodAttributes Attributes => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

            public override ParameterInfo[] GetParameters() => throw new NotImplementedException();

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
