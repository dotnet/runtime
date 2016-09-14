// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System.ComponentModel;

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [
        Flags
    ]
    internal enum GeneratorSupport
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ArraysOfArrays = 0x1,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        EntryPointMethod = 0x2,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GotoStatements = 0x4,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultidimensionalArrays = 0x8,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        StaticConstructors = 0x10,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        TryCatchStatements = 0x20,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ReturnTypeAttributes = 0x40,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeclareValueTypes = 0x80,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeclareEnums = 0x0100,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeclareDelegates = 0x0200,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeclareInterfaces = 0x0400,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        DeclareEvents = 0x0800,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AssemblyAttributes = 0x1000,
        /// <devdoc>
        ///    <para>Supports custom metadata attributes declared on parameters for methods and constructors. Allows
        ///          use of CodeParameterDeclarationExpress.CustomAttributes.</para>
        /// </devdoc>
        ParameterAttributes = 0x2000,
        /// <devdoc>
        ///    <para>Supports declaring and calling parameters with a FieldDirection of Out or Ref, meaning that
        ///          the value is a type of reference parameter.</para>
        /// </devdoc>
        ReferenceParameters = 0x4000,
        /// <devdoc>
        ///    <para>Supports contructors that call other constructors within the same class. Allows use of the 
        ///          CodeConstructor.ChainedConstructorArgs collection.</para>
        /// </devdoc>
        ChainedConstructorArguments = 0x8000,
        /// <devdoc>
        ///    <para>Supports declaring types that are nested within other types. This allows the insertion of a 
        ///          CodeTypeReference into the Members collection of another CodeTypeReference.</para>
        /// </devdoc>
        NestedTypes = 0x00010000,
        /// <devdoc>
        ///    <para>Supports declaring methods, properties or events that simultaneously implement more than one interface of
        ///          a type that have a matching name. This allows insertion of more than one entry into the ImplementationTypes 
        ///          collection or CodeMemberProperty, CodeMemberMethod and CodeMemberEvent.</para>
        /// </devdoc>
        MultipleInterfaceMembers = 0x00020000,
        /// <devdoc>
        ///    <para>Supports the declaration of public static fields, properties, methods and events. This allows use of 
        ///          MemberAttributes.Static in combination with access values other than MemberAttributes.Private.</para>
        /// </devdoc>
        PublicStaticMembers = 0x00040000,
        /// <devdoc>
        ///    <para>Supports the generation arbitarily nested expressions. Not all generators may be able to deal with 
        ///          multiple function calls or binary operations in the same expression. Without this, CodeMethodInvokeExpression and
        ///          CodeBinaryOperatorExpression should only be used (a) as the Right value of a CodeAssignStatement or (b) in a
        ///          CodeExpressionStatement.</para>
        /// </devdoc>
        ComplexExpressions = 0x00080000,
        /// <devdoc>
        ///    <para>Supports linking with Win32 resources.</para>
        /// </devdoc>
        Win32Resources = 0x00100000,
        /// <devdoc>
        ///    <para>Supports linking with CLR resources (both linked and embedded).</para>
        /// </devdoc>
        Resources = 0x00200000,
        /// <devdoc>
        ///    <para>Supports partial classes.</para>
        /// </devdoc>
        PartialTypes = 0x00400000,
        GenericTypeReference = 0x00800000,
        GenericTypeDeclaration = 0x01000000,
        DeclareIndexerProperties = 0x02000000,
    }
}
