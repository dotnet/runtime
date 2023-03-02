// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class ConstructorBuilder : ConstructorInfo
    {
        protected ConstructorBuilder()
        {
        }

        public bool InitLocals
        {
            get => InitLocalsCore;
            set { InitLocalsCore = value; }
        }

        protected abstract bool InitLocalsCore { get; set; }

        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string strParamName)
            => DefineParameterCore(iSequence, attributes, strParamName);

        protected abstract ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string strParamName);

        public ILGenerator GetILGenerator()
            => GetILGeneratorCore(64);

        public ILGenerator GetILGenerator(int streamSize)
            => GetILGeneratorCore(streamSize);

        protected abstract ILGenerator GetILGeneratorCore(int streamSize);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttributeCore(con, binaryAttribute);

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttributeCore(customBuilder);

        protected abstract void SetCustomAttributeCore(CustomAttributeBuilder customBuilder);

        public void SetImplementationFlags(MethodImplAttributes attributes)
            => SetImplementationFlagsCore(attributes);

        protected abstract void SetImplementationFlagsCore(MethodImplAttributes attributes);
    }
}
