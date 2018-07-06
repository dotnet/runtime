// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionMemberExportDefinition : ExportDefinition, ICompositionElement
    {
        private readonly LazyMemberInfo _member;
        private readonly ExportDefinition _exportDefinition;
        private readonly ICompositionElement _origin;
        private IDictionary<string, object> _metadata;

        public ReflectionMemberExportDefinition(LazyMemberInfo member, ExportDefinition exportDefinition, ICompositionElement origin)
        {
            if (exportDefinition == null)
            {
                throw new ArgumentNullException(nameof(exportDefinition));
            }

            _member = member;
            _exportDefinition = exportDefinition;
            _origin = origin;
        }

        public override string ContractName
        {
            get { return _exportDefinition.ContractName; }
        }

        public LazyMemberInfo ExportingLazyMember
        {
            get { return _member; }
        }

        public override IDictionary<string, object> Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    _metadata = _exportDefinition.Metadata.AsReadOnly();
                }
                return _metadata;
            }
        }

        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        ICompositionElement ICompositionElement.Origin
        {
            get { return _origin; }
        }

        public override string ToString()
        {
            return GetDisplayName();
        }

        public int GetIndex()
        {
            return ExportingLazyMember.ToReflectionMember().UnderlyingMember.MetadataToken;
        }

        public ExportingMember ToExportingMember()
        {
            return new ExportingMember(this, ToReflectionMember());
        }

        private ReflectionMember ToReflectionMember()
        {
            return ExportingLazyMember.ToReflectionMember();
        }

        private string GetDisplayName()
        {
            return string.Format(CultureInfo.CurrentCulture,
                   "{0} (ContractName=\"{1}\")",    // NOLOC
                   ToReflectionMember().GetDisplayName(),
                   ContractName);
        }
    }
}
