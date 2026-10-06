// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    /// <summary>
    /// The largest slot index a local can have. <c>ldloc</c>, <c>ldloca</c> and <c>stloc</c> take a 16-bit index.
    /// </summary>
    private const int MaximumLocalSlot = ushort.MaxValue;

    /// <summary>
    /// The explicit <c>[n]</c> slot indices of the <c>.locals</c> directive being parsed, keyed by the position of
    /// the argument in the directive, with the token of <c>n</c>. <see cref="SetRawParameterAttributeElement"/>
    /// records them and <see cref="EndLocalsDirective"/> consumes and clears them.
    /// </summary>
    private readonly Dictionary<int, (int Slot, IToken Token)> _explicitLocalSlots = new();

    /// <summary>
    /// Returns the arguments of a <c>.locals</c> directive with <see cref="SignatureArgumentValue.Slot"/> set from
    /// the recorded <c>[n]</c> elements. As in native ilasm, <c>[-1]</c> means "no explicit slot". Any other value
    /// outside [0, <see cref="MaximumLocalSlot"/>] is reported, naming the value as written in the source, and
    /// ignored, so the local takes the next free slot (which <see cref="DeclareLocals"/> checks in turn). The
    /// range is checked on the literal as written, so a literal too large for 32 bits is reported too rather than
    /// narrowed into the range or to <c>-1</c>.
    /// </summary>
    private ImmutableArray<SignatureArgumentValue> ApplyExplicitLocalSlots(ImmutableArray<SignatureArgumentValue> arguments)
    {
        if (_explicitLocalSlots.Count == 0)
        {
            return arguments;
        }

        ImmutableArray<SignatureArgumentValue>.Builder builder = arguments.ToBuilder();
        foreach ((int index, (int parsedSlot, IToken token)) in _explicitLocalSlots)
        {
            // The recorded value is ParseInt32's, which narrows the literal to 32 bits without a check:
            // [4294967296] would be slot 0 and [4294967295] would be [-1]. A literal that cannot be read has
            // already been reported by ParseInt32 and keeps its value.
            long slot = ReadLocalSlotLiteral(token.Text) ?? parsedSlot;

            // An [n] is recorded for an argument only as that argument is parsed, so every recorded position has
            // an argument; the bound check guards against a grammar change that records one for an argument
            // that is then dropped.
            if (index >= builder.Count || slot == -1)
            {
                continue;
            }

            if (slot < 0 || slot > MaximumLocalSlot)
            {
                // The token's text rather than the value read, which is capped for a literal past the range:
                // [2147483648] is reported as 2147483648.
                ReportError(
                    DiagnosticIds.LocalSlotOutOfRange,
                    string.Format(DiagnosticMessageTemplates.LocalSlotOutOfRange, token.Text, MaximumLocalSlot),
                    token);
                continue;
            }

            builder[index] = builder[index] with { Slot = (int)slot };
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Reads the value of an <c>[n]</c> slot literal as the grammar's integer token writes it (an optional
    /// <c>-</c>, then <c>0x</c> and hexadecimal digits, <c>0</c> and octal digits, or decimal digits) without
    /// narrowing it. A magnitude past <see cref="MaximumLocalSlot"/> + 1 reads as <see cref="MaximumLocalSlot"/> + 1
    /// with the literal's sign, so a literal of any length that is outside the slot range reads as a value outside
    /// it, and none reads as <c>-1</c>.
    /// </summary>
    /// <returns>
    /// The value, or <see langword="null"/> for a literal with a digit its base does not have (an invalid octal
    /// literal, which <see cref="ParseInt32"/> reports).
    /// </returns>
    /// <remarks>
    /// Only local slots are read this way. A raw <c>[n]</c> on a parameter keeps <see cref="ParseInt32"/>'s value.
    /// </remarks>
    private static long? ReadLocalSlotLiteral(string text)
    {
        ReadOnlySpan<char> digits = text.AsSpan();
        bool negative = digits.StartsWith("-".AsSpan());
        if (negative)
        {
            digits = digits.Slice(1);
        }

        // The same bases as ParseInt32: 0x is hexadecimal, and any other leading 0 is octal.
        int radix = 10;
        if (digits.StartsWith("0x".AsSpan()))
        {
            radix = 16;
            digits = digits.Slice(2);
        }
        else if (digits.StartsWith("0".AsSpan()))
        {
            radix = 8;
        }

        long magnitude = 0;
        foreach (char c in digits)
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => radix,
            };
            if (digit >= radix)
            {
                return null;
            }

            magnitude = Math.Min(magnitude * radix + digit, MaximumLocalSlot + 1);
        }

        return negative ? -magnitude : magnitude;
    }

    /// <summary>
    /// Declares the locals of a <c>.locals</c> directive in the innermost open lexical scope, in order.
    /// </summary>
    /// <remarks>
    /// This follows native ilasm's <c>EmitLocals</c>. A local without an explicit slot takes the next slot after
    /// the highest one declared so far in the method. A local with slot <c>n</c> takes slot <c>n</c>; when
    /// <c>n</c> is past the end of the slot table, the slots before it are added without a type, to be typed by a
    /// later declaration. Declaring a slot that already exists is allowed: if the slot is in use
    /// (<see cref="LocalSlot.InScope"/>), the declaration is reported as a warning, and if its type differs from
    /// the slot's, as an error. Either way the slot takes the new type. A slot freed when its scope closed may be
    /// declared again with the same type without a diagnostic; that is how a method can have fewer slots than
    /// local declarations. A local that would take the next slot when that is past
    /// <see cref="MaximumLocalSlot"/> (a slot after 65535) is reported and not declared: no local instruction
    /// could name it, and the PDB could not record it. A <c>...</c> sentinel in the list is not a local: as in
    /// native ilasm, it takes no slot. That is only a <c>...</c> standing alone as an argument; in
    /// <c>... int32 a</c> the sentinel is part of the type, and the argument is a local like any other.
    /// A named local is added to the scope's names unless the scope already has a local with that name, so the
    /// first declaration of a name in a scope is the one that name refers to.
    /// </remarks>
    private void DeclareLocals(
        CurrentMethodContext method,
        ImmutableArray<SignatureArgumentValue> arguments,
        CILParser.LocalsDeclContext context)
    {
        ImmutableArray<SignatureArg> locals = MaterializeSignatureArguments(arguments);
        LexicalScope scope = method.OpenScopes[^1];
        List<LocalSlot> slots = method.LocalSlots;
        for (int i = 0; i < locals.Length; i++)
        {
            SignatureArg local = locals[i];
            if (local.IsSentinel)
            {
                continue;
            }

            int index = arguments[i].Slot ?? slots.Count;

            // The one check of the slot a declaration gets. ApplyExplicitLocalSlots has replaced an out-of-range
            // [n] with "next slot", so what fails here is a next slot past the last one, after [65535].
            if (index < 0 || index > MaximumLocalSlot)
            {
                ReportError(
                    DiagnosticIds.LocalSlotOutOfRange,
                    string.Format(DiagnosticMessageTemplates.LocalSlotOutOfRange, index, MaximumLocalSlot),
                    context);
                continue;
            }

            if (index < slots.Count)
            {
                LocalSlot existing = slots[index];
                if (existing.InScope)
                {
                    ReportWarning(
                        DiagnosticIds.LocalSlotInUse,
                        string.Format(DiagnosticMessageTemplates.LocalSlotInUse, index),
                        context);
                }

                if (existing.Type is not null && !existing.Type.ContentEquals(local.SignatureBlob))
                {
                    ReportError(
                        DiagnosticIds.LocalSlotTypeConflict,
                        string.Format(DiagnosticMessageTemplates.LocalSlotTypeConflict, index),
                        context);
                }
            }
            else
            {
                while (slots.Count <= index)
                {
                    slots.Add(new LocalSlot(context.Start));
                }
            }

            LocalSlot slot = slots[index];
            slot.Type = local.SignatureBlob;
            slot.InScope = true;
            scope.DeclaredSlots.Add(index);
            if (local.Name is not null)
            {
                scope.Names.TryAdd(local.Name, index);
            }
        }
    }

    /// <summary>
    /// Opens a lexical scope for a <c>{ }</c> block of the current method body, starting at the current offset.
    /// </summary>
    private void OpenLexicalScope(CurrentMethodContext method)
        => method.OpenScopes.Add(new LexicalScope(CurrentMethodBodyOffset));

    /// <summary>
    /// Closes the innermost open lexical scopes of the method, at the current offset, until <paramref name="count"/>
    /// remain open. The slots that a closed scope declared are no longer in use, even when an enclosing scope that
    /// is still open declared the same slot (<see cref="LocalSlot.InScope"/>).
    /// </summary>
    private void CloseLexicalScopes(CurrentMethodContext method, int count)
    {
        while (method.OpenScopes.Count > count)
        {
            LexicalScope scope = method.OpenScopes[^1];
            method.OpenScopes.RemoveAt(method.OpenScopes.Count - 1);
            foreach (int slot in scope.DeclaredSlots)
            {
                method.LocalSlots[slot].InScope = false;
            }
        }
    }

    /// <summary>
    /// Sets the method body's local signature from the slot table: one entry per slot, in slot order. A slot that
    /// no declaration typed is reported, as native ilasm does, and written as <c>int32</c> so the signature stays
    /// well-formed for error-tolerant output.
    /// </summary>
    private void EmitLocalSignature(CurrentMethodContext method)
    {
        List<LocalSlot> slots = method.LocalSlots;
        if (slots.Count == 0)
        {
            return;
        }

        BlobBuilder localsSignature = new();
        LocalVariablesEncoder localsEncoder = new BlobEncoder(localsSignature).LocalVariableSignature(slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Type is { } type)
            {
                type.WriteContentTo(localsEncoder.AddVariable().Builder);
            }
            else
            {
                ReportError(
                    DiagnosticIds.UndefinedLocalSlotType,
                    string.Format(DiagnosticMessageTemplates.UndefinedLocalSlotType, i, method.Definition.Name),
                    slots[i].Declaration);
                localsEncoder.AddVariable().Type().Int32();
            }
        }

        method.Definition.LocalsSignature = _entityRegistry.GetOrCreateStandaloneSignature(localsSignature);
    }

    /// <summary>
    /// A slot of a method's local signature.
    /// </summary>
    private sealed class LocalSlot
    {
        /// <summary>Creates a slot that no declaration has typed yet and that is not in use.</summary>
        /// <param name="declaration">The first token of the <c>.locals</c> directive that added the slot.</param>
        public LocalSlot(IToken declaration) => Declaration = declaration;

        /// <summary>
        /// Gets the first token of the <c>.locals</c> directive that added the slot: the one that declared it, or,
        /// for a slot added as padding before an explicit slot index, the one that declared that index.
        /// </summary>
        public IToken Declaration { get; }

        /// <summary>
        /// Gets or sets the type blob of the slot's latest declaration, or <see langword="null"/> when no
        /// declaration has typed it.
        /// </summary>
        public BlobBuilder? Type { get; set; }

        /// <summary>
        /// Gets or sets whether the slot is in use, which makes declaring it again a warning. As in native ilasm,
        /// which keeps one such flag per slot, every declaration of the slot sets it and the closing of any scope
        /// that declared the slot clears it. So when a block declares a slot that an enclosing scope also declared
        /// and then closes, the slot is no longer in use, although the enclosing scope is still open.
        /// </summary>
        public bool InScope { get; set; }
    }

    /// <summary>
    /// An open lexical scope of a method body: the method's root scope or a <c>{ }</c> block, including the bodies
    /// of <c>.try</c>, <c>catch</c>, <c>filter</c>, <c>finally</c> and <c>fault</c>.
    /// </summary>
    private sealed class LexicalScope
    {
        /// <param name="startOffset">The IL offset at which the scope starts.</param>
        public LexicalScope(int startOffset) => StartOffset = startOffset;

        /// <summary>Gets the IL offset at which the scope starts: 0 for the root scope, the offset at <c>{</c> for a block.</summary>
        public int StartOffset { get; }

        /// <summary>
        /// Gets the slot of each name declared in this scope. Names resolve from the innermost open scope outward.
        /// </summary>
        public Dictionary<string, int> Names { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the slot of each local declared in this scope, named or not, in declaration order.</summary>
        public List<int> DeclaredSlots { get; } = new();
    }
}
