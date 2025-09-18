// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Diagnostics.DataContract.JsonConverter;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class DataDescriptorModel
{
    public int Version => 1;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Baseline { get; }
    public IReadOnlyDictionary<string, TypeModel> Types { get; }
    public IReadOnlyDictionary<string, GlobalModel> Globals { get; }
    public IReadOnlyDictionary<string, GlobalModel> SubDescriptors { get; }
    public IReadOnlyDictionary<string, int> Contracts { get; }
    [JsonIgnore]
    public uint PlatformFlags { get; }
    // The number of indirect globals plus 1 for the placeholder at index 0
    [JsonIgnore]
    public int PointerDataCount => 1 + Globals.Values.Count(g => g.Value.Kind == GlobalValue.KindEnum.Indirect) + SubDescriptors.Values.Count(s => s.Value.Kind == GlobalValue.KindEnum.Indirect);

    private DataDescriptorModel(string baseline, IReadOnlyDictionary<string, TypeModel> types, IReadOnlyDictionary<string, GlobalModel> globals, IReadOnlyDictionary<string, GlobalModel> subDescriptors, IReadOnlyDictionary<string, int> contracts, uint platformFlags)
    {
        Baseline = baseline;
        Types = types;
        Globals = globals;
        SubDescriptors = subDescriptors;
        Contracts = contracts;
        PlatformFlags = platformFlags;
    }

    public const string PointerTypeName = "pointer";
    public const string StringTypeName = "string";

    internal void DumpModel()
    {
        Console.WriteLine("\nData Descriptor Model:");
        Console.WriteLine($"Platform Flags: 0x{PlatformFlags:x8}");
        Console.WriteLine($"Baseline: {Baseline}");
        foreach (var (typeName, type) in Types)
        {
            Console.WriteLine($"Type: {typeName}");
            if (type.Size != null)
            {
                Console.WriteLine($"  Size: 0x{type.Size:x8}");
            }
            foreach (var (fieldName, field) in type.Fields)
            {
                Console.WriteLine($"  Field: {fieldName}");
                Console.WriteLine($"    Type: {field.Type}");
                Console.WriteLine($"    Offset: 0x{field.Offset:x8}");
            }
        }
        foreach (var (globalName, global) in Globals)
        {
            Console.WriteLine($"Global: {globalName}");
            Console.WriteLine($"  Type: {global.Type}");
            Console.WriteLine($"  Value: {global.Value}");
        }
        foreach (var (subDescriptorName, subDescriptor) in SubDescriptors)
        {
            Console.WriteLine($"Sub-Descriptor: {subDescriptorName}");
            Console.WriteLine($"  Type: {subDescriptor.Type}");
            Console.WriteLine($"  Value: {subDescriptor.Value}");
        }
        foreach (var (contractName, contract) in Contracts)
        {
            Console.WriteLine($"Contract: {contractName}");
            Console.WriteLine($"  Version: {contract}");
        }
    }

    private static JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null, // leave unchanged
    };
    public string ToJson()
    {
        // always writes the "compact" format, see data_descriptor.md
        return JsonSerializer.Serialize(this, s_jsonSerializerOptions);
    }

    public class Builder
    {
        private string _baseline;
        private readonly string _baselinesDir;
        private bool _baselineParsed;
        private readonly Dictionary<string, TypeModelBuilder> _types = new();
        private readonly Dictionary<string, GlobalBuilder> _globals = new();
        private readonly Dictionary<string, GlobalBuilder> _subDescriptors = new();
        private readonly Dictionary<string, ContractBuilder> _contracts = new();
        public Builder(string baselinesDir)
        {
            _baseline = string.Empty;
            _baselineParsed = false;
            _baselinesDir = baselinesDir;
        }

        public uint PlatformFlags {get; set;}

        public TypeModelBuilder AddOrUpdateType(string name, int? size)
        {
            if (!_baselineParsed)
            {
                throw new InvalidOperationException("Baseline must be set before adding types");
            }
            if (!_types.TryGetValue(name, out var type))
            {
                type = new TypeModelBuilder();
                _types[name] = type;
            }
            type.Size = size;
            return type;
        }

        public GlobalBuilder AddOrUpdateGlobal(string name, string type, GlobalValue? value)
        {
            if (!_baselineParsed)
            {
                throw new InvalidOperationException("Baseline must be set before adding globals");
            }
            if (!_globals.TryGetValue(name, out var global))
            {
                global = new GlobalBuilder();
                _globals[name] = global;
            }
            global.Type = type;
            global.Value = value;
            return global;
        }

        public GlobalBuilder AddOrUpdateSubDescriptor(string name, string type, GlobalValue? value)
        {
            if (!_baselineParsed)
            {
                throw new InvalidOperationException("Baseline must be set before adding globals");
            }
            if (!_subDescriptors.TryGetValue(name, out var subDescriptor))
            {
                subDescriptor = new GlobalBuilder();
                _subDescriptors[name] = subDescriptor;
            }
            subDescriptor.Type = type;
            subDescriptor.Value = value;
            return subDescriptor;
        }

        public void AddOrUpdateContract(string name, int version)
        {
            if (!_contracts.TryGetValue(name, out var contract))
            {
                contract = new ContractBuilder();
                _contracts[name] = contract;
            }
            contract.Version = version;
        }

        public void SetBaseline(string baseline)
        {
            if (_baseline != string.Empty && _baseline != baseline)
            {
                throw new InvalidOperationException($"Baseline already set to {_baseline} cannot set to {baseline}");
            }
            var baselines = new DirectoryBaselines(_baselinesDir);
            if (baselines.BaselineNames.Contains(baseline))
            {
                _baseline = baseline;
            }
            else
            {
                throw new InvalidOperationException($"Baseline '{baseline}' not known");
            }
            _baseline = baseline;
            if (!_baselineParsed)
            {
                _baselineParsed = true; // kind of a hack - set it before parsing the baseline, so we can call AddOrUpdateType
                ParseBaseline();
            }
        }

        private void ParseBaseline()
        {
            if (_baseline != "empty")
            {
                throw new InvalidOperationException("TODO: [cdac] - implement baseline parsing");
            }
        }

        public DataDescriptorModel Build()
        {
            var types = new Dictionary<string, TypeModel>();
            foreach (var (typeName, typeBuilder) in _types)
            {
                types[typeName] = typeBuilder.Build(typeName);
            }
            var globals = new Dictionary<string, GlobalModel>();
            foreach (var (globalName, globalBuilder) in _globals)
            {
                GlobalValue? v = globalBuilder.Value;
                if (v == null)
                {
                    throw new InvalidOperationException($"Value must be set for global {globalName}");
                }
                globals[globalName] = new GlobalModel { Type = globalBuilder.Type, Value = v.Value };
            }
            var subDescriptors = new Dictionary<string, GlobalModel>();
            foreach (var (subDescriptorName, subDescriptorBuilder) in _subDescriptors)
            {
                GlobalValue? v = subDescriptorBuilder.Value;
                if (v == null)
                {
                    throw new InvalidOperationException($"Value must be set for sub-descriptor {subDescriptorName}");
                }
                subDescriptors[subDescriptorName] = new GlobalModel { Type = subDescriptorBuilder.Type, Value = v.Value };
            }
            var contracts = new Dictionary<string, int>();
            foreach (var (contractName, contractBuilder) in _contracts)
            {
                contracts[contractName] = contractBuilder.Build();
            }
            return new DataDescriptorModel(_baseline, types, globals, subDescriptors, contracts, PlatformFlags);
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
            var fields = new Dictionary<string, FieldModel>();
            foreach (var (fieldName, fieldBuilder) in _fields)
            {
                fields.Add(fieldName, fieldBuilder.Build(typeName, fieldName));
            }
            return new TypeModel { Size = _size, Fields = fields };
        }
    }

    public class GlobalBuilder
    {
        private string _type = string.Empty;
        private GlobalValue? _value;
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
        public GlobalValue? Value
        {
            get => _value;
            set
            {
                if (_value != null && _value != value)
                {
                    throw new InvalidOperationException($"Value already set to {_value} cannot set to {value}");
                }
                _value = value;
            }
        }
    }

    internal sealed class FieldBuilder
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

        public FieldModel Build(string typeName, string fieldName)
        {
            if (_offset == null)
            {
                throw new InvalidOperationException($"Offset must be set for {typeName}.{fieldName}");
            }
            return new FieldModel { Type = _type, Offset = (int)_offset };
        }
    }

    [JsonConverter(typeof(FieldModelJsonConverter))]
    public readonly struct FieldModel
    {
        public string Type { get; init; }
        public int Offset { get; init; }
    }

    [JsonConverter(typeof(TypeModelJsonConverter))]
    public readonly struct TypeModel
    {
        public int? Size { get; init; }
        public IReadOnlyDictionary<string, FieldModel> Fields { get; init; }
    }

    [JsonConverter(typeof(GlobalValueJsonConverter))]
    public readonly struct GlobalValue : IEquatable<GlobalValue>
    {
        public enum KindEnum
        {
            Direct,
            Indirect,
            String
        }

        public KindEnum Kind { get; private init; }
        public ulong NumericValue { get; }
        public string StringValue { get; }
        public static GlobalValue MakeDirect(ulong value) => new GlobalValue(value) { Kind = KindEnum.Direct };
        public static GlobalValue MakeIndirect(uint auxDataIdx) => new GlobalValue((ulong)auxDataIdx) { Kind = KindEnum.Indirect };
        public static GlobalValue MakeString(string value) => new GlobalValue(value) { Kind = KindEnum.String };
        private GlobalValue(ulong value) { NumericValue = value; StringValue = string.Empty;}
        private GlobalValue(string value) { StringValue = value; }

        public static bool operator ==(GlobalValue left, GlobalValue right) => left.Equals(right);
        public static bool operator !=(GlobalValue left, GlobalValue right) => !(left == right);

        public bool Equals(GlobalValue other) => other.Kind == Kind && other.NumericValue == NumericValue && other.StringValue == StringValue;
        public override bool Equals(object? obj) => obj is GlobalValue value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Kind, NumericValue, StringValue);
        public override string ToString()
        {
            return Kind switch
            {
                KindEnum.Direct => $"0x{NumericValue:x}",
                KindEnum.Indirect => $"Indirect({NumericValue})",
                KindEnum.String => $"'{StringValue}'",
                _ => throw new InvalidOperationException("Unknown GlobalValue type")
            };
        }
    }

    [JsonConverter(typeof(GlobalModelJsonConverter))]
    public readonly struct GlobalModel
    {
        public string Type { get; init; }
        public GlobalValue Value { get; init; }
    }

    public class ContractBuilder
    {
        private int? _version;
        public ContractBuilder()
        {
        }

        public int? Version
        {
            get => _version;
            set
            {
                if (_version != null && _version != value)
                {
                    throw new InvalidOperationException($"Version already set to {_version} cannot set to {value}");
                }
                _version = value;
            }
        }

        // There is no ContractModel right now because the only info we keep is the version.
        // As a result it is convenient to use a Dictionary<string,int> for the contracts since
        // the JSON serialization coincides with what we want.
        public int Build()
        {
            if (_version == null)
            {
                throw new InvalidOperationException("Version must be set for contract");
            }
            return _version.Value;
        }
    }
}
