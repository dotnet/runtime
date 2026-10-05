# Managed IL Assembler - Fixed Issues

| Fixed issue | ilasm behavior | Managed ILASM behavior |
| --- | --- | --- |
| `FloatingPointInstruction_ByteForms_EmitExpectedConstants` | Rejects the `bytearray` operand for floating-point constants. | Accepts byte-array forms for floating-point instruction constants. |
| `LabelBasedFilterRegion_EmitsExactExceptionRegionBounds` | Rejects the label-based filter region as undefined or structurally separated. | Emits the label-based filter region with the requested bounds. |
| `Ldtoken_NumericTokenEmittedCorrectly` | Rejects a numeric metadata token as an `ldtoken` operand. | Accepts and emits a numeric metadata token operand for `ldtoken`. |
| `ValidKeyFile_EmbedsPublicKeyAndSetsAssemblyFlag` | Reports that full signing is unsupported. | Embeds the full-signing key and sets the assembly flag. |
| `CustomAttribute_ObjectArrayWithNestedArrays_DecodesProperly` | Rejects nested attribute-array forms including `float32('a')`. | Accepts and decodes the nested attribute arrays. |
| `Typedef_CustomAttributeWithOwner_AssemblyUsePreservesOwner` | Crashes without a diagnostic. | Resolves the custom-attribute typedef and preserves its explicit assembly owner. |
| `Typedef_FieldAndCustomAttributeForms_EmitResolvableMetadata` | Crashes without a diagnostic. | Resolves the field and custom-attribute typedef forms and emits resolvable metadata. |
| `FieldAttributes_EmitExpectedFlagsAndNullConstant` | Rejects the tested `volatile` field modifier form. | Accepts the form and emits the expected field flags. |
| Native unmanaged method bodies | Refuses to compile the tested `native unmanaged` method bodies. | Accepts the method bodies and applies the tested pseudoattribute updates. |
| `DataLabelReference_MissingTarget_ReportsDiagnosticAndOmitsRelocation` | Fails without producing an output image. | Reports the missing target and emits a recoverable image without the relocation. |
| `ExportDirective_WithoutVTableFixup_DoesNotEmitNativeExportArtifacts` | Emits `.sdata`, vtable-fixup, and export directories for the unmatched `.export`. | Ignores the unmatched `.export` and omits native export artifacts. |
| `ExportOrdinal_MaxValueAsBase_EmitsSingleEntry` | Crashes without a diagnostic for an export at ordinal `int.MaxValue`. | Emits one export whose base ordinal is `int.MaxValue`. |
| `ExportOrdinal_NonPositive_ReportsDiagnosticAndOmitsExport` | Emits invalid artifacts for ordinals 0 and -1 and crashes on `int.MinValue`. | Reports non-positive ordinals and omits their exports. |
| `ExportStub_TargetMachine_IsExecutableAndRelocatable` | Rejects ARM64 exports and emits different stub layouts on supported targets. | Emits the expected executable, relocatable, target-specific export stubs. |
| `VTableFixupAndExport_UseDeclaredWritableDataAndExecutableStubs` | Places a later sparse-ordinal export stub in writable, non-executable `.sdata`. | Places every export stub in executable, non-writable `.text`, independent of ordinal gaps. |
| `Export_InvalidVTableReference_ReportsDiagnosticAndOmitsExport` | Emits fixup or export artifacts without diagnosing invalid entry or slot references. | Reports the invalid reference and omits the export. |
| `VTableEntry_InvalidAssociationWithoutExport_ReportsDiagnostic` | Emits vtable fixups without diagnosing invalid associations. | Reports the invalid associations and leaves the affected slot empty. |
| Adjacent typed data items | Rejects the second typed item in a `.data` declaration. | Accepts adjacent typed values in a `.data` declaration. |
| `VTableFixup_InsufficientDataLabelStorage_ReportsDiagnosticAndOmitsFixup` | Emits a fixup that exceeds its backing data without a diagnostic. | Reports the undersized backing storage and omits the fixup. |
| `VTableFixup_InvalidSlotCount_ReportsDiagnosticAndOmitsFixup` | Truncates or wraps invalid slot counts into emitted fixups. | Reports invalid slot counts and omits the fixup. |
| `VTableFixup_InvalidWidthForTarget_ReportsDiagnostic` | Emits a fixup with an invalid target width without a diagnostic. | Reports the invalid width and omits the fixup. |
| `VTableFixup_MissingDataLabel_ReportsDiagnosticAndOmitsFixup` | Fails without producing an output image. | Reports the missing data label and emits a recoverable image without the fixup. |
| `GenericParameterCount_OutsideMetadataIndexRange_RequiresErrorTolerant` | Silently emits more than 65,536 generic parameters and wraps their 16-bit metadata ordinals back to 0. | Reports that the generic parameter count exceeds the metadata encoding limit; only error-tolerant mode emits the wrapped metadata. |
| Generic parameter constraint owner outside the metadata index range | Silently emits a constraint owned by a generic parameter whose 16-bit metadata ordinal has wrapped. | Reports that the constraint owner index cannot be encoded; error-tolerant mode preserves the constraint for inspection. |
| `MethodGenericParameterAndConstraintDirectives_AttachCustomAttributes` | Reports an out-of-range generic parameter and a missing owner or index. | Resolves method generic parameter and constraint directives by name. |
| `MethodReferenceForms_EmitResolvableCallTokensAndMethodSpecification` | Treats the unqualified in-type call as an unresolved global reference. | Resolves the call to the method in the containing type. |
| `TypelistMscorlibAndSemicolonControls_EmitExpectedMetadata` | Leaves the implicit `System.Object` base unresolved. | Resolves the implicit object base through `mscorlib`. |
| Compiler-controlled scope disambiguator | Strips the `$PST` token-specific disambiguator from anywhere within a symbol that it is present. | Strips the `$PST` disambiguator only from the end of a symbol name, the only place ildasm adds it. |
