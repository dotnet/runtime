// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace ILAssembler.Tests.GeneratedCases
{
    /// <summary>
    /// A generated method with nested <c>{ }</c> blocks that declare locals, and what the assembler must produce for
    /// it: the type of each local slot, and the slot each <c>ldloc name</c> refers to, in instruction order.
    /// </summary>
    public sealed record GeneratedLocalsMethod(
        string Name,
        string Body,
        ImmutableArray<string> SlotTypes,
        ImmutableArray<int> ReferencedSlots);

    /// <summary>
    /// A generated program: one class with one or more <see cref="GeneratedLocalsMethod"/>s, and how many of each
    /// generated shape it has.
    /// </summary>
    public sealed record GeneratedLocalsProgram(ImmutableArray<GeneratedLocalsMethod> Methods, ImmutableSortedDictionary<string, int> Shapes)
    {
        public string ToSource()
        {
            var source = new StringBuilder();
            source.AppendLine(".assembly extern System.Runtime { }");
            source.AppendLine(".assembly test { }");
            source.AppendLine(".class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object");
            source.AppendLine("{");
            foreach (GeneratedLocalsMethod method in Methods)
            {
                source.AppendLine($"    .method public static void {method.Name}() cil managed");
                source.AppendLine("    {");
                source.Append(method.Body);
                source.AppendLine("    }");
            }

            source.AppendLine("}");
            return source.ToString();
        }

        public override string ToString()
            => $"methods={Methods.Length} " + string.Join(" ", Shapes.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    /// <summary>
    /// Generates methods whose locals are declared in nested lexical blocks, with a fixed seed so that every run
    /// builds the same cases and a failing case is reproduced by its index.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each method has a root scope and blocks nested up to four deep, with zero to three sibling blocks per level.
    /// Each scope declares zero to three locals in one <c>.locals</c> directive. A local has a name from a small
    /// pool, so that inner blocks shadow outer names and a scope sometimes declares a name twice, or no name. Its
    /// slot is the next free one, written with or without <c>[n]</c>; a slot past the end, which pads the slot
    /// table; or, written as <c>[n]</c>, a slot that no open scope holds, reused with the type it already has.
    /// Padding slots that no later declaration types are declared at the end of the root scope, so every method
    /// assembles without diagnostics. Between blocks, a scope has a few <c>nop</c>s and loads some of the names
    /// visible there with <c>ldloc name</c>.
    /// </para>
    /// <para>
    /// The model follows native ilasm: a local without <c>[n]</c> takes the slot after the highest one declared so
    /// far, a slot holds the type of its declarations, and a name refers to the first declaration of that name in
    /// the innermost open scope that declares it. Instructions are written in their long forms, so the model knows
    /// every IL offset: <c>nop</c>, <c>pop</c> and <c>ret</c> are one byte and <c>ldloc</c> is four.
    /// </para>
    /// </remarks>
    public static class LocalScopeCaseGenerator
    {
        private const int Seed = 20261006 + 3;
        private const int CaseCount = 200;
        private const int MaximumDepth = 4;

        private static readonly string[] s_names = ["a", "b", "c", "d"];
        private static readonly string[] s_types = ["int32", "string", "object", "float64", "int64", "bool"];

        /// <summary>The generated programs.</summary>
        public static ImmutableArray<GeneratedLocalsProgram> Cases { get; } = Generate(new Random(Seed));

        /// <summary>The index and description of each generated program.</summary>
        public static TheoryData<int, string> CaseData
        {
            get
            {
                var data = new TheoryData<int, string>();
                for (int i = 0; i < Cases.Length; i++)
                {
                    data.Add(i, Cases[i].ToString());
                }

                return data;
            }
        }

        /// <summary>
        /// Gets, for each shape the generator produces, the number of cases that have it, so that a test can check
        /// that the cases cover every shape.
        /// </summary>
        public static ImmutableSortedDictionary<string, int> CasesPerShape
            => Cases.SelectMany(program => program.Shapes.Keys)
                .GroupBy(shape => shape, StringComparer.Ordinal)
                .ToImmutableSortedDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        private static ImmutableArray<GeneratedLocalsProgram> Generate(Random random)
        {
            var cases = ImmutableArray.CreateBuilder<GeneratedLocalsProgram>(CaseCount);
            for (int i = 0; i < CaseCount; i++)
            {
                int methodCount = 1 + random.Next(3);
                var methods = ImmutableArray.CreateBuilder<GeneratedLocalsMethod>(methodCount);
                var shapes = new Dictionary<string, int>();
                for (int m = 0; m < methodCount; m++)
                {
                    methods.Add(new MethodGenerator(random, shapes).Generate($"M{m}"));
                }

                cases.Add(new GeneratedLocalsProgram(
                    methods.MoveToImmutable(),
                    shapes.ToImmutableSortedDictionary(StringComparer.Ordinal)));
            }

            return cases.MoveToImmutable();
        }

        /// <summary>Generates one method, keeping native ilasm's slot table and scope chain as it writes the body.</summary>
        private sealed class MethodGenerator
        {
            private readonly Random _random;
            private readonly Dictionary<string, int> _stats;
            private readonly StringBuilder _body = new();
            private readonly List<string?> _slotTypes = new();
            private readonly List<bool> _slotInScope = new();
            private readonly List<OpenScope> _openScopes = new();
            private readonly List<int> _referencedSlots = new();
            private int _offset;

            public MethodGenerator(Random random, Dictionary<string, int> stats)
            {
                _random = random;
                _stats = stats;
            }

            public GeneratedLocalsMethod Generate(string name)
            {
                Block(depth: 0, indent: "        ");
                return new GeneratedLocalsMethod(
                    name,
                    _body.ToString(),
                    _slotTypes.Select(type => type!).ToImmutableArray(),
                    _referencedSlots.ToImmutableArray());
            }

            private void Count(string key, int increment = 1) => _stats[key] = _stats.GetValueOrDefault(key) + increment;

            private void Line(string indent, string text) => _body.Append(indent).AppendLine(text);

            private void Block(int depth, string indent)
            {
                bool root = depth == 0;
                string inner = root ? indent : indent + "    ";
                if (!root)
                {
                    Line(indent, "{");
                    Count("blocks");
                    Count($"depth{depth}");
                }

                var scope = new OpenScope();
                _openScopes.Add(scope);
                Declare(scope, _random.Next(4), inner);
                int children = depth < MaximumDepth ? _random.Next(4) : 0;
                for (int i = 0; i < children; i++)
                {
                    Instructions(inner);
                    Block(depth + 1, inner);
                }

                Instructions(inner);
                if (root)
                {
                    DeclarePaddingSlots(scope, inner);
                    Line(inner, "ret");
                    _offset += 1;
                }

                foreach (int slot in scope.DeclaredSlots)
                {
                    _slotInScope[slot] = false;
                }

                _openScopes.RemoveAt(_openScopes.Count - 1);
                if (!root)
                {
                    Line(indent, "}");
                }
            }

            private void Declare(OpenScope scope, int count, string indent)
            {
                if (count == 0)
                {
                    return;
                }

                var declarations = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    string? name = _random.Next(5) == 0 ? null : s_names[_random.Next(s_names.Length)];
                    string type = s_types[_random.Next(s_types.Length)];
                    int? explicitSlot;
                    int slot;
                    int choice = _random.Next(100);
                    List<int> free = Enumerable.Range(0, _slotTypes.Count).Where(index => !_slotInScope[index]).ToList();
                    if (choice < 20 && free.Count > 0)
                    {
                        slot = free[_random.Next(free.Count)];
                        explicitSlot = slot;
                        type = _slotTypes[slot] ?? type;
                        Count(_slotTypes[slot] is null ? "fillsPadding" : "reusesClosedSlot");
                    }
                    else if (choice < 35)
                    {
                        slot = _slotTypes.Count + 1 + _random.Next(2);
                        explicitSlot = slot;
                        Count("gaps");
                    }
                    else if (choice < 60)
                    {
                        slot = _slotTypes.Count;
                        explicitSlot = slot;
                        Count("explicitNext");
                    }
                    else
                    {
                        slot = _slotTypes.Count;
                        explicitSlot = null;
                    }

                    while (_slotTypes.Count <= slot)
                    {
                        _slotTypes.Add(null);
                        _slotInScope.Add(false);
                    }

                    _slotTypes[slot] = type;
                    _slotInScope[slot] = true;
                    scope.DeclaredSlots.Add(slot);
                    if (name is null)
                    {
                        Count("unnamed");
                    }
                    else
                    {
                        if (scope.Names.ContainsKey(name))
                        {
                            Count("duplicateNameInScope");
                        }
                        else if (_openScopes.Take(_openScopes.Count - 1).Any(outer => outer.Names.ContainsKey(name)))
                        {
                            Count("shadows");
                        }

                        scope.Names.TryAdd(name, slot);
                    }

                    declarations.Add((explicitSlot is int n ? $"[{n}] " : string.Empty) + type + (name is null ? string.Empty : " " + name));
                }

                Line(indent, $".locals init ({string.Join(", ", declarations)})");
            }

            private void DeclarePaddingSlots(OpenScope root, string indent)
            {
                var declarations = new List<string>();
                for (int slot = 0; slot < _slotTypes.Count; slot++)
                {
                    if (_slotTypes[slot] is null)
                    {
                        string name = $"pad{slot}";
                        _slotTypes[slot] = "int32";
                        _slotInScope[slot] = true;
                        root.DeclaredSlots.Add(slot);
                        root.Names.Add(name, slot);
                        declarations.Add($"[{slot}] int32 {name}");
                        Count("padDeclaredAtEnd");
                    }
                }

                if (declarations.Count > 0)
                {
                    Line(indent, $".locals init ({string.Join(", ", declarations)})");
                }
            }

            private void Instructions(string indent)
            {
                int nops = _random.Next(3);
                for (int i = 0; i < nops; i++)
                {
                    Line(indent, "nop");
                    _offset += 1;
                }

                SortedSet<string> visible = new(_openScopes.SelectMany(scope => scope.Names.Keys), StringComparer.Ordinal);
                foreach (string name in visible)
                {
                    if (_random.Next(5) < 2)
                    {
                        Line(indent, $"ldloc {name}");
                        Line(indent, "pop");
                        _offset += 5;
                        _referencedSlots.Add(Resolve(name));
                        Count("references");
                    }
                }
            }

            private int Resolve(string name)
            {
                for (int i = _openScopes.Count - 1; i >= 0; i--)
                {
                    if (_openScopes[i].Names.TryGetValue(name, out int slot))
                    {
                        return slot;
                    }
                }

                throw new InvalidOperationException($"'{name}' is not visible");
            }

            private sealed class OpenScope
            {
                public Dictionary<string, int> Names { get; } = new(StringComparer.Ordinal);

                public List<int> DeclaredSlots { get; } = new();
            }
        }
    }
}
