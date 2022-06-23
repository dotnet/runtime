// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if smolloy_add_schema_import

using System;
using System.Xml;
using System.Xml.Schema;
#if smolloy_codedom_stubbed
using System.CodeDom.Stubs;
#elif smolloy_codedom_full_internalish
using System.Runtime.Serialization.CodeDom;
#endif
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    internal sealed class ContractCodeDomInfo
    {
        private string? _clrNamespace;
        private Dictionary<string, object?>? _memberNames;  // NOTE TODO smolloy - This is weird. The only entries ever added to this dictionary are always null. Seems like we really just need a list of keys.

        internal string? ClrNamespace
        {
            get { return ReferencedTypeExists ? null : _clrNamespace; }
            set
            {
                if (ReferencedTypeExists)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotSetNamespaceForReferencedType, TypeReference?.BaseType)));
                else
                    _clrNamespace = value;
            }
        }

        internal CodeNamespace? CodeNamespace { get; set; }

        internal bool IsProcessed { get; set; }

        internal bool ReferencedTypeExists { get; set; }

        internal CodeTypeDeclaration? TypeDeclaration { get; set; }

        internal CodeTypeReference? TypeReference { get; set; }

        internal bool UsesWildcardNamespace { get; set; }

        internal Dictionary<string, object?> GetMemberNames()
        {
            if (ReferencedTypeExists)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotSetMembersForReferencedType, TypeReference?.BaseType)));
            else
            {
                if (_memberNames == null)
                {
                    _memberNames = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                }
                return _memberNames;
            }
        }
    }
}
#endif
