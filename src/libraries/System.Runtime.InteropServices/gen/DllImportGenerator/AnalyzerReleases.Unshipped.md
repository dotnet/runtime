### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DLLIMPORTGEN001         | SourceGeneration | Error    | TypeNotSupported
DLLIMPORTGEN002         | SourceGeneration | Error    | ConfigurationNotSupported
DLLIMPORTGEN003         | SourceGeneration | Error    | TargetFrameworkNotSupported
DLLIMPORTGENANALYZER001 | Usage            | Error    | BlittableTypeMustBeBlittable
DLLIMPORTGENANALYZER002 | Usage            | Error    | CannotHaveMultipleMarshallingAttributes
DLLIMPORTGENANALYZER003 | Usage            | Error    | NativeTypeMustBeNonNull
DLLIMPORTGENANALYZER004 | Usage            | Error    | NativeTypeMustBeBlittable
DLLIMPORTGENANALYZER005 | Usage            | Error    | GetPinnableReferenceReturnTypeBlittable
DLLIMPORTGENANALYZER006 | Usage            | Error    | NativeTypeMustBePointerSized
DLLIMPORTGENANALYZER007 | Usage            | Error    | NativeTypeMustHaveRequiredShape
DLLIMPORTGENANALYZER008 | Usage            | Error    | ValuePropertyMustHaveSetter
DLLIMPORTGENANALYZER009 | Usage            | Error    | ValuePropertyMustHaveGetter
DLLIMPORTGENANALYZER010 | Usage            | Warning  | GetPinnableReferenceShouldSupportAllocatingMarshallingFallback
DLLIMPORTGENANALYZER011 | Usage            | Warning  | StackallocMarshallingShouldSupportAllocatingMarshallingFallback
DLLIMPORTGENANALYZER012 | Usage            | Error    | StackallocConstructorMustHaveStackBufferSizeConstant
DLLIMPORTGENANALYZER013 | Usage            | Warning  | GeneratedDllImportMissingRequiredModifiers
DLLIMPORTGENANALYZER014 | Usage            | Error    | RefValuePropertyUnsupported
DLLIMPORTGENANALYZER015 | Interoperability | Disabled | ConvertToGeneratedDllImportAnalyzer
DLLIMPORTGENANALYZER016 | Usage            | Error    | GenericTypeMustBeClosed
DLLIMPORTGENANALYZER017 | Usage            | Warning  | GeneratedDllImportContainingTypeMissingModifiers
