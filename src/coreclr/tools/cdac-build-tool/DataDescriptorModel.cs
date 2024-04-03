// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class DataDescriptorModel
{
    public IReadOnlyDictionary<string, TypeModel> Types { get; }
    private DataDescriptorModel(IReadOnlyDictionary<string, TypeModel> types)
    {
        Types = types;
    }

    internal void DumpModel()
    {
        foreach (var (typeName, type) in Types)
        {
            Console.WriteLine($"Type: {typeName}");
            if (type.Size != null)
            {
                Console.WriteLine($"  Size: {type.Size}");
            }
            foreach (var (fieldName, field) in type.Fields)
            {
                Console.WriteLine($"  Field: {fieldName}");
                Console.WriteLine($"    Type: {field.Type}");
                Console.WriteLine($"    Offset: {field.Offset}");
            }
        }
    }

    public class Builder
    {
        private readonly Dictionary<string, TypeModelBuilder> _types = new();
        private readonly Dictionary<string, GlobalBuilder> _globals = new();
        public Builder()
        {

        }

        public TypeModelBuilder AddOrUpdateType(string name, int? size)
        {
            if (!_types.TryGetValue(name, out var type))
            {
                type = new TypeModelBuilder();
                _types[name] = type;
            }
            type.Size = size;
            return type;
        }

        public DataDescriptorModel Build()
        {
            var types = new Dictionary<string, TypeModel>();
            foreach (var (typeName, typeBuilder) in _types)
            {
                types[typeName] = typeBuilder.Build(typeName);
            }
            return new DataDescriptorModel(types);
        }
    }

    public class TypeModelBuilder
    {
        private readonly Dictionary<string, FieldBuilder> _fields = new();
        private int? _size;
        public TypeModelBuilder() { }

        public int? Size
        {
            get => _size;
            set
            {
                if (_size != null && (value == null || _size != (int)value))
                {
                    throw new InvalidOperationException($"Size already set to {_size} cannot set to {value}");
                }
                _size = value;
            }
        }

        public void AddOrUpdateField(string name, string type, int? offset)
        {
            if (!_fields.TryGetValue(name, out var field))
            {
                field = new FieldBuilder();
                _fields[name] = field;
            }
            field.Type = type;
            field.Offset = offset;
        }

        public TypeModel Build(string typeName)
        {
            var fields = new Dictionary<string, Field>();
            foreach (var (fieldName, fieldBuilder) in _fields)
            {
                fields.Add(fieldName, fieldBuilder.Build(typeName, fieldName));
            }
            return new TypeModel { Size = _size, Fields = fields };
        }
    }

    public class GlobalBuilder { }

    class FieldBuilder
    {
        private string _type = string.Empty;
        private int? _offset;
        public string Type
        {
            get => _type;
            set
            {
                if (_type != string.Empty && _type != value)
                {
                    throw new InvalidOperationException($"Type already set to {_type} cannot set to {value}");
                }
                _type = value;
            }
        }

        public int? Offset
        {
            get => _offset;
            set
            {
                if (_offset != null && (value == null || _offset != (int)value))
                {
                    throw new InvalidOperationException($"Offset already set to {_offset} cannot set to {value}");
                }
                _offset = value;
            }
        }

        public Field Build(string typeName, string fieldName)
        {
            if (_offset == null)
            {
                throw new InvalidOperationException($"Offset must be set for {typeName}.{fieldName}");
            }
            return new Field { Type = _type, Offset = (int)_offset };
        }
    }

    public readonly struct Field
    {
        public string Type { get; init; }
        public int Offset { get; init; }
    }

    public readonly struct TypeModel
    {
        public int? Size { get; init; }
        public IReadOnlyDictionary<string, Field> Fields { get; init; }
    }

}
