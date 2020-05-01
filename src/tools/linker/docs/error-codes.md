# ILLinker Errors and Warnings

Every linker error and warning has an assigned unique error code for easier
identification. The known codes are in the range 1000 to 6000. Custom
steps should avoid using this range not to collide with existing or future
error and warning codes.

## Error Codes

#### IL1001: To be added


## Warning Codes

#### IL2001: Type 'type' has no fields to preserve

The XML descriptor preserves fields on type 'type', but this type has no fields.

#### IL2002: Type 'type' has no methods to preserve

The XML descriptor preserves methods on type 'type', but this type has no methods.

#### IL2003: Could not resolve 'assembly' assembly dependency specified in a 'PreserveDependency' attribute that targets method 'method'

The assembly 'assembly' in `PreserveDependency` attribute could not be resolved.

#### IL2004: Could not resolve 'type' type dependency specified in a 'PreserveDependency' attribute that targets method 'method'

The type 'type' in `PreserveDependency` attribute could not be resolved.

#### IL2005: Could not resolve dependency member 'member' declared in type 'type' specified in a 'PreserveDependency' attribute that targets method 'method'

The member 'member' in `PreserveDependency` attribute could not be resolved.

#### IL2006: Unrecognized reflection pattern

The linker found an unrecognized reflection access pattern. The most likely reason for this is that the linker could not resolve a member that is being accessed dynamicallly. To fix this, use the `DynamicallyAccessedMemberAttribute` and specify the member kinds you're trying to access.

#### IL2007: Could not match assembly 'assembly' for substitution

The assembly 'assembly' in the substitution XML could not be resolved.

#### IL2008: Could not resolve type 'type' for substitution

The type 'type' in the substitution XML could not be resolved.

#### IL2009: Could not find method 'method' for substitution

The method 'method' in the substitution XML could not be resolved.

#### IL2010: Invalid value for 'signature' stub

The value 'value' used in the substitution XML for method 'signature' does not represent a value of a built-in type, or does not match the return type of the method.

#### IL2011: Unknown body modification 'action' for 'signature'

The value 'action' of the body attribute used in the substitution XML is invalid (the only supported options are `remove` and `stub`).

#### IL2012: Could not find field 'field' for substitution

The field 'field' specified in the substitution XML was not found.

#### IL2013: Substituted field 'field' needs to be static field

The substituted field 'field' was non-static or constant. Only static non-constant fields are supported.

#### IL2014: Missing 'value' attribute for field 'field'

A field was specified for substitution but no value to be substituted was given.

#### IL2015: Invalid value for 'field': 'value'

The value 'value' used in the substitution XML for field 'field' is not a built-in type, or does not match the type of 'field'.
