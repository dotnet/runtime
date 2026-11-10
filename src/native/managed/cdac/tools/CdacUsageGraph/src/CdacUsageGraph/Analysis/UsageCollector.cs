// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;

namespace CdacUsageGraph.Analysis;

/// <summary>
/// Mutable, indexed accumulator for the walk. A Data type used without any recorded field still
/// gets an entry. When the walk completes, <see cref="Build"/> freezes these indexes into the
/// ordered, record-oriented <see cref="UsageGraph"/>.
/// </summary>
internal sealed class UsageCollector
{
    private readonly DataTypeIndex _index;
    private readonly Dictionary<ContractVersion, ContractUsageBuilder> _contracts = new();

    public UsageCollector(DataTypeIndex index)
    {
        _index = index;
    }

    public void RecordContract(ContractVersion label) => GetOrAddContract(label);

    /// <summary>Records that <paramref name="type"/> is used, even if no field is read.</summary>
    public void RecordType(ContractVersion label, DataDescriptorType type) =>
        GetOrAddContract(label).GetOrAddDataType(DataName(type));

    public void RecordDependencies(
        ContractVersion label,
        DataDescriptorType declaringType,
        DataDescriptorDependencies dependencies)
    {
        foreach (string? typeName in dependencies.TypeSizeTypeNames)
            GetOrAddContract(label).GetOrAddDataType(DependencyDataName(declaringType, typeName)).UsesTypeSize = true;
        foreach (DataDescriptorFieldDependency field in dependencies.Fields)
            RecordField(label, declaringType, field);
    }

    /// <summary>Records a specific field usage (and implicitly the type usage).</summary>
    public void RecordField(
        ContractVersion label,
        DataDescriptorType declaringType,
        DataDescriptorFieldDependency dependency)
    {
        GetOrAddContract(label).GetOrAddDataType(
            DependencyDataName(declaringType, dependency.TypeName)).RecordField(
                dependency.FieldName,
                dependency.NativeType);
    }

    public void RecordContractUsed(ContractVersion label, ContractInterface contractInterface)
    {
        if (label.Interface == contractInterface)
            return;

        GetOrAddContract(label).ContractsUsed.Add(contractInterface);
    }

    public void RecordGlobal(
        ContractVersion label,
        string name,
        string type,
        bool isOptional)
    {
        GetOrAddContract(label).RecordGlobal(name, type, isOptional);
    }

    public UsageGraph Build(string cdacRoot, int dataTypeCount) =>
        new(
            cdacRoot,
            dataTypeCount,
            _contracts
                .OrderBy(entry => entry.Key.Interface.Name, StringComparer.Ordinal)
                .ThenBy(entry => entry.Key.Version, StringComparer.Ordinal)
                .Select(entry => entry.Value.Build(entry.Key))
                .ToArray());

    private ContractUsageBuilder GetOrAddContract(ContractVersion label)
    {
        if (!_contracts.TryGetValue(label, out ContractUsageBuilder? usage))
            _contracts[label] = usage = new ContractUsageBuilder();
        return usage;
    }

    private static string DataName(DataDescriptorType type) => "Data." + type.Name;

    private string DependencyDataName(DataDescriptorType declaringType, string? typeName)
    {
        if (typeName is null)
            return DataName(declaringType);

        return _index.TryGetType(typeName, out DataDescriptorType dependencyType)
            ? DataName(dependencyType)
            : $"Data.{typeName}";
    }

    private static string MergeType(string? current, string incoming, string description)
    {
        if (string.IsNullOrEmpty(incoming))
            throw new InvalidOperationException($"{description} has no native type.");
        if (current is null)
            return incoming;
        if (current == incoming)
            return current;

        throw new InvalidOperationException(
            $"{description} was read with multiple native types: '{current}' and '{incoming}'.");
    }

    private sealed class ContractUsageBuilder
    {
        private readonly Dictionary<string, DataTypeUsageBuilder> _dataTypes =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, GlobalUsageBuilder> _globals =
            new(StringComparer.Ordinal);

        public HashSet<ContractInterface> ContractsUsed { get; } = [];

        public DataTypeUsageBuilder GetOrAddDataType(string name)
        {
            if (!_dataTypes.TryGetValue(name, out DataTypeUsageBuilder? usage))
                _dataTypes[name] = usage = new DataTypeUsageBuilder(name);
            return usage;
        }

        public void RecordGlobal(string name, string type, bool isOptional)
        {
            if (!_globals.TryGetValue(name, out GlobalUsageBuilder? usage))
                _globals[name] = usage = new GlobalUsageBuilder(name);
            usage.Type = MergeType(usage.Type, type, $"Global '{name}'");
            usage.IsOptional &= isOptional;
        }

        public ContractVersionUsage Build(ContractVersion label) =>
            new(
                label,
                _dataTypes.Values
                    .OrderBy(usage => usage.Name, StringComparer.Ordinal)
                    .Select(usage => usage.Build())
                    .ToArray(),
                _globals.Values
                    .OrderBy(usage => usage.Name, StringComparer.Ordinal)
                    .Select(usage => usage.Build())
                    .ToArray(),
                ContractsUsed
                    .OrderBy(contract => contract.Name, StringComparer.Ordinal)
                    .ToArray());
    }

    private sealed class DataTypeUsageBuilder(string name)
    {
        private readonly Dictionary<string, FieldUsageBuilder> _fields =
            new(StringComparer.Ordinal);

        public string Name { get; } = name;
        public bool UsesTypeSize { get; set; }

        public void RecordField(string field, string type)
        {
            if (!_fields.TryGetValue(field, out FieldUsageBuilder? usage))
                _fields[field] = usage = new FieldUsageBuilder(field);
            usage.Type = MergeType(
                usage.Type,
                type,
                $"Data descriptor field '{Name}.{field}'");
        }

        public DataTypeUsage Build() =>
            new(
                Name,
                UsesTypeSize,
                _fields.Values
                    .OrderBy(usage => usage.Name, StringComparer.Ordinal)
                    .Select(usage => usage.Build())
                    .ToArray());
    }

    private sealed class FieldUsageBuilder(string name)
    {
        public string Name { get; } = name;
        public string? Type { get; set; }

        public FieldUsage Build() =>
            new(
                Name,
                Type ?? throw new InvalidOperationException(
                    $"Data descriptor field '{Name}' has no native type."));
    }

    private sealed class GlobalUsageBuilder(string name)
    {
        public string Name { get; } = name;
        public string? Type { get; set; }
        public bool IsOptional { get; set; } = true;

        public GlobalUsage Build() =>
            new(
                Name,
                Type ?? throw new InvalidOperationException(
                    $"Global '{Name}' has no native type."),
                IsOptional);
    }
}
