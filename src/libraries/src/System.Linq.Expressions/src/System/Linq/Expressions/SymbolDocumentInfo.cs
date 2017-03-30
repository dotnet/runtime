// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Stores information needed to emit debugging symbol information for a
    /// source file, in particular the file name and unique language identifier.
    /// </summary>
    public class SymbolDocumentInfo
    {
        internal SymbolDocumentInfo(string fileName)
        {
            ContractUtils.RequiresNotNull(fileName, nameof(fileName));
            FileName = fileName;
        }

        /// <summary>
        /// The source file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Returns the language's unique identifier, if any.
        /// </summary>
        public virtual Guid Language => Guid.Empty;

        /// <summary>
        /// Returns the language vendor's unique identifier, if any.
        /// </summary>
        public virtual Guid LanguageVendor => Guid.Empty;

        internal static readonly Guid DocumentType_Text = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd);

        /// <summary>
        /// Returns the document type's unique identifier, if any.
        /// Defaults to the guid for a text file.
        /// </summary>
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

    public partial class Expression
    {
        /// <summary>
        /// Creates an instance of <see cref="SymbolDocumentInfo"/>.
        /// </summary>
        /// <param name="fileName">A <see cref="String"/> to set the <see cref="SymbolDocumentInfo.FileName"/> equal to.</param>
        /// <returns>A <see cref="SymbolDocumentInfo"/> that has the <see cref="SymbolDocumentInfo.FileName"/> property set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName)
        {
            return new SymbolDocumentInfo(fileName);
        }

        /// <summary>
        /// Creates an instance of <see cref="SymbolDocumentInfo"/>.
        /// </summary>
        /// <param name="fileName">A <see cref="String"/> to set the <see cref="SymbolDocumentInfo.FileName"/> equal to.</param>
        /// <param name="language">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.Language"/> equal to.</param>
        /// <returns>A <see cref="SymbolDocumentInfo"/> that has the <see cref="SymbolDocumentInfo.FileName"/>
        /// and <see cref="SymbolDocumentInfo.Language"/> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language)
        {
            return new SymbolDocumentWithGuids(fileName, ref language);
        }

        /// <summary>
        /// Creates an instance of <see cref="SymbolDocumentInfo"/>.
        /// </summary>
        /// <param name="fileName">A <see cref="String"/> to set the <see cref="SymbolDocumentInfo.FileName"/> equal to.</param>
        /// <param name="language">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.Language"/> equal to.</param>
        /// <param name="languageVendor">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.LanguageVendor"/> equal to.</param>
        /// <returns>A <see cref="SymbolDocumentInfo"/> that has the <see cref="SymbolDocumentInfo.FileName"/>
        /// and <see cref="SymbolDocumentInfo.Language"/>
        /// and <see cref="SymbolDocumentInfo.LanguageVendor"/> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language, Guid languageVendor)
        {
            return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor);
        }

        /// <summary>
        /// Creates an instance of <see cref="SymbolDocumentInfo"/>.
        /// </summary>
        /// <param name="fileName">A <see cref="String"/> to set the <see cref="SymbolDocumentInfo.FileName"/> equal to.</param>
        /// <param name="language">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.Language"/> equal to.</param>
        /// <param name="languageVendor">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.LanguageVendor"/> equal to.</param>
        /// <param name="documentType">A <see cref="Guid"/> to set the <see cref="SymbolDocumentInfo.DocumentType"/> equal to.</param>
        /// <returns>A <see cref="SymbolDocumentInfo"/> that has the <see cref="SymbolDocumentInfo.FileName"/>
        /// and <see cref="SymbolDocumentInfo.Language"/>
        /// and <see cref="SymbolDocumentInfo.LanguageVendor"/>
        /// and <see cref="SymbolDocumentInfo.DocumentType"/> properties set to the specified value.</returns>
        public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language, Guid languageVendor, Guid documentType)
        {
            return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor, ref documentType);
        }
    }
}
