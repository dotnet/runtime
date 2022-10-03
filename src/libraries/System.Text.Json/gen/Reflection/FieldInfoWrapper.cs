// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;

namespace System.Text.Json.Reflection
{
    internal sealed class FieldInfoWrapper : FieldInfo
    {
        private readonly IFieldSymbol _field;
        private readonly MetadataLoadContextInternal _metadataLoadContext;
        public FieldInfoWrapper(IFieldSymbol parameter, MetadataLoadContextInternal metadataLoadContext)
        {
            _field = parameter;
            _metadataLoadContext = metadataLoadContext;

            NeedsAtSign = SyntaxFacts.GetKeywordKind(_field.Name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(_field.Name) != SyntaxKind.None;
        }

        private FieldAttributes? _attributes;

        public override FieldAttributes Attributes
        {
            get
            {
                if (!_attributes.HasValue)
                {
                    _attributes = default(FieldAttributes);

                    if (_field.IsStatic)
                    {
                        _attributes |= FieldAttributes.Static;
                    }

                    if (_field.IsReadOnly)
                    {
                        _attributes |= FieldAttributes.InitOnly;
                    }

                    switch (_field.DeclaredAccessibility)
                    {
                        case Accessibility.Public:
                            _attributes |= FieldAttributes.Public;
                            break;
                        case Accessibility.Private:
                            _attributes |= FieldAttributes.Private;
                            break;
                        case Accessibility.Protected:
                            _attributes |= FieldAttributes.Family;
                            break;
                    }
                }

                return _attributes.Value;
            }
        }

        public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();

        public override Type FieldType => _field.Type.AsType(_metadataLoadContext);

        public override Type DeclaringType => _field.ContainingType.AsType(_metadataLoadContext);

        public override string Name => _field.Name;

        public bool NeedsAtSign { get; }

        public override Type ReflectedType => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj)
        {
            throw new NotImplementedException();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _field.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public Location? Location => _field.Locations.Length > 0 ? _field.Locations[0] : null;
    }
}
