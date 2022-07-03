// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

using ExceptionUtil = System.Runtime.Serialization.Schema.DiagnosticUtility.ExceptionUtility;

namespace System.Runtime.Serialization.Schema
{
    internal sealed class ContractCodeDomInfo
    {
        private string? _clrNamespace;
        // TODO smolloy - This was a Dictionary<string,object?> previously, so adding a duplicate entry would throw an exception.
        // HashSet does not allow duplicates either, but it just returns false instead of throwing. I think it's safe to not
        // throw in that case here, so long as we don't add duplicates. It's just a string list.
        private HashSet<string>? _memberNames;

        internal string? ClrNamespace
        {
            get { return ReferencedTypeExists ? null : _clrNamespace; }
            set
            {
                if (ReferencedTypeExists)
                    throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotSetNamespaceForReferencedType, TypeReference?.BaseType)));
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

        internal HashSet<string> GetMemberNames()
        {
            if (ReferencedTypeExists)
                throw ExceptionUtil.ThrowHelperError(new InvalidOperationException(SR.Format(SR.CannotSetMembersForReferencedType, TypeReference?.BaseType)));
            else
                return _memberNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
