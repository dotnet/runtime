using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Interface for generation of marshalling code for P/Invoke stubs
    /// </summary>
    internal interface IMarshallingGenerator
    {
        /// <summary>
        /// Get the native type syntax for <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Type syntax for the native type representing <paramref name="info"/></returns>
        TypeSyntax AsNativeType(TypePositionInfo info);

        /// <summary>
        /// Get the <paramref name="info"/> as a parameter of the P/Invoke declaration
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Parameter syntax for <paramref name="info"/></returns>
        ParameterSyntax AsParameter(TypePositionInfo info);

        /// <summary>
        /// Get the <paramref name="info"/> as an argument to be passed to the P/Invoke
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>Argument syntax for <paramref name="info"/></returns>
        ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Generate code for marshalling
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>List of statements to be added to the P/Invoke stub</returns>
        /// <remarks>
        /// The generator should return the appropriate statements based on the
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/>.
        /// For <see cref="StubCodeContext.Stage.Pin"/>, any statements not of type
        /// <see cref="FixedStatementSyntax"/> will be ignored.
        /// </remarks>
        IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context);
    }

    internal class MarshallingGenerators
    {
        public static readonly BoolMarshaller Bool = new BoolMarshaller();
        public static readonly Forwarder Forwarder = new Forwarder();
        public static readonly NumericMarshaller Numeric = new NumericMarshaller();

        public static bool TryCreate(TypePositionInfo info, out IMarshallingGenerator generator)
        {
#if GENERATE_FORWARDER
            generator = MarshallingGenerators.Forwarder;
            return true;
#else
            if (info.IsNativeReturnPosition && !info.IsManagedReturnPosition)
            {
                // [TODO] Use marshaller for native HRESULT return / exception throwing
                // Debug.Assert(info.ManagedType.SpecialType == SpecialType.System_Int32)
            }

            switch (info.ManagedType.SpecialType)
            {
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    generator = MarshallingGenerators.Numeric;
                    return true;

                case SpecialType.System_Boolean:
                    generator = MarshallingGenerators.Bool;
                    return true;
                default:
                    generator = MarshallingGenerators.Forwarder;
                    return false;
            }
#endif
        }
    }
}
