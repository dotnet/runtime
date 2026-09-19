// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ILVerify;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Determines whether every typeref/memberref/methodspec/typespec token referenced from a method body
    /// defined in a module is accessible to the method that references it. If this holds for every method in
    /// the module, the runtime can trust that fact and skip the equivalent JIT-time access checks
    /// (READYTORUN_FLAG_SkipAccessValidation) for methods defined in this module.
    ///
    /// This mirrors (and is intentionally analogous to) TypeValidationChecker, but instead of validating that
    /// type *definitions* in the module are well-formed, it validates that references *from* method bodies in
    /// the module obey ECMA-335 accessibility rules (Partition I, 8.5.3), using the same algorithm
    /// (AccessVerificationHelpers.CanAccess) that ILVerify uses to validate accessibility for full IL
    /// verification.
    /// </summary>
    internal sealed class AccessValidationChecker
    {
        private readonly ConcurrentBag<string> _errors = new ConcurrentBag<string>();

        private AccessValidationChecker() { }

        public static async Task<(bool canSkipValidation, string[] reasonsWhyItFailed)> CanSkipValidation(EcmaModule module)
        {
            AccessValidationChecker checker = new AccessValidationChecker();
            bool canSkipValidation = await checker.CanSkipValidationInstance(module);
            return (canSkipValidation, checker._errors.ToArray());
        }

        private async Task<bool> CanSkipValidationInstance(EcmaModule module)
        {
            List<Task<bool>> tasks = new List<Task<bool>>();

            foreach (var type in module.GetAllTypes())
            {
                if (type is EcmaType ecmaType)
                    tasks.Add(Task.Run(() => ValidateType(ecmaType)));
            }
            tasks.Add(Task.Run(() => ValidateType(module.GetGlobalModuleType())));

            bool allOk = true;
            foreach (var task in tasks)
            {
                if (!await task)
                    allOk = false;
            }

            return allOk;
        }

        private bool ValidateType(EcmaType type)
        {
            bool ok = true;
            foreach (MethodDesc methodDesc in type.GetMethods())
            {
                if (methodDesc is not EcmaMethod method)
                    continue;

                if (!ValidateMethod(method))
                    ok = false;
            }

            return ok;
        }

        private bool ValidateMethod(EcmaMethod method)
        {
            EcmaMethodIL il;
            try
            {
                il = EcmaMethodIL.Create(method);
            }
            catch (Exception ex)
            {
                _errors.Add($"'{method}': failed to read method body ({ex.Message})");
                return false;
            }

            // Methods with no IL body (abstract, p/invoke, internal call, etc.) have nothing to scan.
            if (il == null)
                return true;

            MetadataType callingType = (MetadataType)method.OwningType;

            try
            {
                byte[] ilBytes = il.GetILBytes();
                ILReader reader = new ILReader(ilBytes);

                while (reader.HasNext)
                {
                    ILOpcode opcode = reader.ReadILOpcode();

                    if (!TryGetTokenOperandKind(opcode, out TokenOperandKind kind))
                    {
                        reader.Skip(opcode);
                        continue;
                    }

                    int token = reader.ReadILToken();

                    if (!ValidateToken(method, callingType, il, token, kind))
                        return false;
                }

                foreach (ILExceptionRegion region in il.GetExceptionRegions())
                {
                    if (region.Kind != ILExceptionRegionKind.Catch)
                        continue;

                    if (!ValidateToken(method, callingType, il, region.ClassToken, TokenOperandKind.Type))
                        return false;
                }
            }
            catch (Exception ex)
            {
                _errors.Add($"'{method}': failed to scan method body for access validation ({ex.GetType().Name}: {ex.Message})");
                return false;
            }

            return true;
        }

        private bool ValidateToken(EcmaMethod method, MetadataType callingType, EcmaMethodIL il, int token, TokenOperandKind kind)
        {
            object resolved;
            try
            {
                resolved = il.GetObject(token, NotFoundBehavior.ReturnNull);
            }
            catch (Exception ex)
            {
                _errors.Add($"'{method}': could not resolve token 0x{token:x8} ({ex.Message})");
                return false;
            }

            if (resolved == null)
            {
                _errors.Add($"'{method}': unresolvable token 0x{token:x8}");
                return false;
            }

            bool accessOk;
            switch (resolved)
            {
                case TypeDesc targetType:
                    accessOk = callingType.CanAccess(targetType);
                    break;
                case MethodDesc targetMethod:
                    // Check accessibility against the typical (fully uninstantiated) method definition rather
                    // than the resolved (possibly instantiated) MethodDesc. A method reference from within a
                    // generic method body may be instantiated with unbound generic-parameter placeholders (e.g.
                    // a call to Foo<!!2>() from within a method that itself has 3 generic parameters); computing
                    // the fully-substituted Signature of such a method can throw, and doing so isn't even useful
                    // here: generic parameters/signature variables are always considered accessible (see
                    // CanAccess(TypeDesc)), so the typical definition's (unsubstituted) signature yields the same
                    // accessibility answer without needing to substitute anything.
                    accessOk = callingType.CanAccess(targetMethod.GetTypicalMethodDefinition());
                    break;
                case FieldDesc targetField:
                    // Same rationale as the MethodDesc case above.
                    accessOk = callingType.CanAccess(targetField.GetTypicalFieldDefinition());
                    break;
                default:
                    // Not a type/method/field (e.g. a string for ldstr, or an unexpected object). Nothing to check.
                    return true;
            }

            if (!accessOk)
            {
                _errors.Add($"'{method}': reference to '{resolved}' (token 0x{token:x8}, {kind}) is not accessible from '{callingType}'");
            }

            return accessOk;
        }

        private enum TokenOperandKind
        {
            Type,
            Method,
            Field,
            TypeOrMethodOrField, // ldtoken
        }

        private static bool TryGetTokenOperandKind(ILOpcode opcode, out TokenOperandKind kind)
        {
            switch (opcode)
            {
                case ILOpcode.jmp:
                case ILOpcode.call:
                case ILOpcode.callvirt:
                case ILOpcode.newobj:
                case ILOpcode.ldftn:
                case ILOpcode.ldvirtftn:
                    kind = TokenOperandKind.Method;
                    return true;

                case ILOpcode.ldfld:
                case ILOpcode.ldflda:
                case ILOpcode.stfld:
                case ILOpcode.ldsfld:
                case ILOpcode.ldsflda:
                case ILOpcode.stsfld:
                    kind = TokenOperandKind.Field;
                    return true;

                case ILOpcode.cpobj:
                case ILOpcode.ldobj:
                case ILOpcode.castclass:
                case ILOpcode.isinst:
                case ILOpcode.unbox:
                case ILOpcode.unbox_any:
                case ILOpcode.stobj:
                case ILOpcode.box:
                case ILOpcode.newarr:
                case ILOpcode.ldelema:
                case ILOpcode.ldelem:
                case ILOpcode.stelem:
                case ILOpcode.refanyval:
                case ILOpcode.mkrefany:
                case ILOpcode.initobj:
                case ILOpcode.constrained:
                case ILOpcode.sizeof_:
                    kind = TokenOperandKind.Type;
                    return true;

                case ILOpcode.ldtoken:
                    kind = TokenOperandKind.TypeOrMethodOrField;
                    return true;

                // calli's operand is a standalone signature token (not a typeref/memberref/methodspec/typespec)
                // and ldstr's operand is a user string token. Neither names a member whose accessibility needs
                // to be checked here; the types embedded in a calli signature are covered elsewhere (as part of
                // whichever typeref/typespec is used to build that signature at its definition site).
                case ILOpcode.calli:
                case ILOpcode.ldstr:
                default:
                    kind = default;
                    return false;
            }
        }
    }
}
