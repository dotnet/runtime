// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Represents a field whose address is mapped to an external symbol.
    /// </summary>
    public class ExternSymbolMappedField : FieldDesc
    {
        private readonly TypeDesc _fieldType;
        private readonly string _symbolName;

        public ExternSymbolMappedField(TypeDesc fieldType, string symbolName)
        {
            _fieldType = fieldType;
            _symbolName = symbolName;
        }

        public override string Name => _symbolName;

        public string SymbolName => _symbolName;

        public override DefType OwningType => _fieldType.Context.SystemModule.GetGlobalModuleType();

        public override TypeDesc FieldType => _fieldType;
        public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => null;

        public override bool IsStatic => true;

        public override bool IsInitOnly => true;

        public override bool IsThreadStatic => false;

        public override bool HasRva => true;

        public override bool IsLiteral => false;

        public override TypeSystemContext Context => _fieldType.Context;

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }

#if !SUPPORT_JIT
        protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            return _symbolName.CompareTo(((ExternSymbolMappedField)other)._symbolName);
        }

        protected override int ClassCode => 7462882;
#endif
    }
}
