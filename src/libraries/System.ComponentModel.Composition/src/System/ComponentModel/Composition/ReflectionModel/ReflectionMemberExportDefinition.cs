// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class ReflectionMemberExportDefinition : ExportDefinition, ICompositionElement
    {
        private readonly LazyMemberInfo _member;
        private readonly ExportDefinition _exportDefinition;
        private readonly ICompositionElement? _origin;

        public ReflectionMemberExportDefinition(LazyMemberInfo member, ExportDefinition exportDefinition, ICompositionElement? origin)
        {
            ArgumentNullException.ThrowIfNull(exportDefinition);

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

        public override IDictionary<string, object?> Metadata => field ??= _exportDefinition.Metadata.AsReadOnly();

        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        ICompositionElement? ICompositionElement.Origin
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

        private string GetDisplayName() =>
            $"{ToReflectionMember().GetDisplayName()} (ContractName=\"{ContractName}\")";    // NOLOC
    }
}
