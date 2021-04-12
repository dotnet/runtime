// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Stores information necessary to emit debugging symbol information for a source file, in particular the file name and unique language identifier.</summary>
    public class SymbolDocumentInfo
    {
        internal SymbolDocumentInfo(string fileName)
        {
            ContractUtils.RequiresNotNull(fileName, nameof(fileName));
            FileName = fileName;
        }

        /// <summary>The source file name.</summary>
        /// <value>The string representing the source file name.</value>
        public string FileName { get; }

        /// <summary>Returns the language's unique identifier, if any.</summary>
        /// <value>The language's unique identifier</value>
        public virtual Guid Language => Guid.Empty;

        /// <summary>Returns the language vendor's unique identifier, if any.</summary>
        /// <value>The language vendor's unique identifier.</value>
        public virtual Guid LanguageVendor => Guid.Empty;

        internal static readonly Guid DocumentType_Text = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd);

        /// <summary>Returns the document type's unique identifier, if any. Defaults to the GUID for a text file.</summary>
        /// <value>The document type's unique identifier.</value>
        public virtual Guid DocumentType => DocumentType_Text;
    }

    internal sealed class SymbolDocumentWithGuids : SymbolDocumentInfo
    {
        internal SymbolDocumentWithGuids(string fileName, ref Guid language)
            : base(fileName)
        {
            Language = language;
            DocumentType = DocumentType_Text;
        }

        internal SymbolDocumentWithGuids(string fileName, ref Guid language, ref Guid vendor)
            : base(fileName)
        {
            Language = language;
            LanguageVendor = vendor;
            DocumentType = DocumentType_Text;
        }

        internal SymbolDocumentWithGuids(string fileName, ref Guid language, ref Guid vendor, ref Guid documentType)
            : base(fileName)
        {
            Language = language;
            LanguageVendor = vendor;
            DocumentType = documentType;
        }

        public override Guid Language { get; }

        public override Guid LanguageVendor { get; }

        public override Guid DocumentType { get; }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
        /// <param name="fileName">A <see cref="string" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that has the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> property set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName)
        {
            return new SymbolDocumentInfo(fileName);
        }

        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
        /// <param name="fileName">A <see cref="string" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.</param>
        /// <param name="language">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that has the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language)
        {
            return new SymbolDocumentWithGuids(fileName, ref language);
        }

        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
        /// <param name="fileName">A <see cref="string" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.</param>
        /// <param name="language">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.</param>
        /// <param name="languageVendor">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that has the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language, Guid languageVendor)
        {
            return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor);
        }

        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
        /// <param name="fileName">A <see cref="string" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.</param>
        /// <param name="language">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.</param>
        /// <param name="languageVendor">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> equal to.</param>
        /// <param name="documentType">A <see cref="System.Guid" /> to set the <see cref="System.Linq.Expressions.SymbolDocumentInfo.DocumentType" /> equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.SymbolDocumentInfo" /> that has the <see cref="System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.Language" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> and <see cref="System.Linq.Expressions.SymbolDocumentInfo.DocumentType" /> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language, Guid languageVendor, Guid documentType)
        {
            return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor, ref documentType);
        }
    }
}
