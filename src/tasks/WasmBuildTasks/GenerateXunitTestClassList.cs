// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Writes the list of xunit test classes in <see cref="Assembly" /> that should get their own Helix
/// work item, one fully qualified class name per line.
///
/// This replaces hand-maintained job list files: a class missing from such a file is silently never
/// run, and a class whose tests are all filtered out costs an empty work item.
/// </summary>
public class GenerateXunitTestClassList : Task
{
    private const string TestCategoryAttributeName = "TestCategoryAttribute";

    // ConditionalFact/ConditionalTheory derive from Fact/Theory, but base types cannot be resolved
    // from metadata alone without loading the reference closure, so match them by name as well.
    private static readonly HashSet<string> s_testMethodAttributeNames = new(StringComparer.Ordinal)
    {
        "FactAttribute",
        "TheoryAttribute",
        "ConditionalFactAttribute",
        "ConditionalTheoryAttribute"
    };

    /// <summary>Path to the built test assembly to scan.</summary>
    [Required]
    public string Assembly { get; set; } = string.Empty;

    /// <summary>Path of the file to write the class names to.</summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// <c>[TestCategory(...)]</c> values that the consuming lane filters out. A class is omitted when
    /// it carries one of these, or when every test method it declares carries one - in both cases the
    /// resulting work item would run no tests at all.
    /// </summary>
    public string[] ExcludedTestCategories { get; set; } = [];

    public override bool Execute()
    {
        if (!File.Exists(Assembly))
        {
            Log.LogError($"Test assembly '{Assembly}' does not exist.");
            return false;
        }

        HashSet<string> excluded = new(ExcludedTestCategories.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);

        List<string> classNames;
        try
        {
            classNames = GetTestClasses(excluded);
        }
        catch (Exception ex) when (IsIoException(ex))
        {
            Log.LogError($"Failed to read '{Assembly}': {ex.Message}");
            return false;
        }
        catch (BadImageFormatException bife)
        {
            Log.LogError($"Failed to read metadata from '{Assembly}': {bife.Message}");
            return false;
        }

        if (classNames.Count == 0)
        {
            Log.LogError($"No xunit test classes found in '{Assembly}'. Expected at least one public non-abstract " +
                         $"class declaring a method attributed with one of: {string.Join(", ", s_testMethodAttributeNames)}.");
            return false;
        }

        classNames.Sort(StringComparer.Ordinal);
        try
        {
            WriteIfChanged(classNames);
        }
        catch (Exception ex) when (IsIoException(ex))
        {
            Log.LogError($"Failed to write '{OutputPath}': {ex.Message}");
            return false;
        }

        Log.LogMessage(MessageImportance.Low, $"Wrote {classNames.Count} test class names to '{OutputPath}'.");
        return !Log.HasLoggedErrors;
    }

    private static bool IsIoException(Exception ex) =>
        ex is IOException or UnauthorizedAccessException;

    private List<string> GetTestClasses(HashSet<string> excluded)
    {
        using FileStream stream = File.OpenRead(Assembly);
        using PEReader peReader = new(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        List<string> classNames = [];
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);

            // Nested types have a NestedPublic/NestedPrivate/... visibility, so this also skips them.
            // xunit can run nested test classes, but no test in this repo relies on that.
            TypeAttributes attributes = type.Attributes;
            if ((attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public
                || (attributes & TypeAttributes.Abstract) != 0
                || (attributes & TypeAttributes.Interface) != 0)
            {
                continue;
            }

            if (GetCategories(reader, type.GetCustomAttributes()).Overlaps(excluded))
                continue;

            if (!HasRunnableTestMethod(reader, type, excluded))
                continue;

            string ns = reader.GetString(type.Namespace);
            string name = reader.GetString(type.Name);
            classNames.Add(string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}");
        }

        return classNames;
    }

    private static bool HasRunnableTestMethod(MetadataReader reader, TypeDefinition type, HashSet<string> excluded)
    {
        // GetMethods() only returns methods declared on this type, so a test class is not picked up
        // through tests it inherits from a shared base class.
        foreach (MethodDefinitionHandle handle in type.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);

            bool isTest = false;
            HashSet<string> categories = new(StringComparer.OrdinalIgnoreCase);
            foreach (CustomAttributeHandle attributeHandle in method.GetCustomAttributes())
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                if (!TryGetAttributeTypeName(reader, attribute, out string? attributeName))
                    continue;

                if (s_testMethodAttributeNames.Contains(attributeName))
                    isTest = true;
                else if (attributeName == TestCategoryAttributeName && TryGetCategory(reader, attribute, out string? category))
                    categories.Add(category);
            }

            if (isTest && !categories.Overlaps(excluded))
                return true;
        }

        return false;
    }

    private static HashSet<string> GetCategories(MetadataReader reader, CustomAttributeHandleCollection handles)
    {
        HashSet<string> categories = new(StringComparer.OrdinalIgnoreCase);
        foreach (CustomAttributeHandle handle in handles)
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (TryGetAttributeTypeName(reader, attribute, out string? name)
                && name == TestCategoryAttributeName
                && TryGetCategory(reader, attribute, out string? category))
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    private static bool TryGetAttributeTypeName(MetadataReader reader, CustomAttribute attribute, [NotNullWhen(true)] out string? name)
    {
        EntityHandle declaringType;
        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                declaringType = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                break;
            case HandleKind.MethodDefinition:
                declaringType = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                break;
            default:
                name = null;
                return false;
        }

        name = declaringType.Kind switch
        {
            HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)declaringType).Name),
            HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)declaringType).Name),
            _ => null
        };

        return name is not null;
    }

    private static bool TryGetCategory(MetadataReader reader, CustomAttribute attribute, [NotNullWhen(true)] out string? category)
    {
        category = null;
        try
        {
            // TestCategoryAttribute has a single 'string category' constructor argument, so the value
            // blob is the 0x0001 prolog followed by that string.
            BlobReader blobReader = reader.GetBlobReader(attribute.Value);
            if (blobReader.Length < sizeof(ushort) || blobReader.ReadUInt16() != 1)
                return false;

            category = blobReader.ReadSerializedString();
            return !string.IsNullOrEmpty(category);
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private void WriteIfChanged(List<string> classNames)
    {
        string contents = string.Join(Environment.NewLine, classNames) + Environment.NewLine;
        if (File.Exists(OutputPath) && File.ReadAllText(OutputPath) == contents)
        {
            Log.LogMessage(MessageImportance.Low, $"'{OutputPath}' is up to date.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputPath))!);
        File.WriteAllText(OutputPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
