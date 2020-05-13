# ILLinker Errors and Warnings

Every linker error and warning has an assigned unique error code for easier
identification. The known codes are in the range 1000 to 6000. Custom
steps should avoid using this range not to collide with existing or future
error and warning codes.

## Error Codes

#### `IL1001`: Failed to process XML substitution: 'XML document location'. Feature 'feature' does not specify a "featurevalue" attribute

- The substitution in 'XML document location' with feature value 'feature' does not use the `featurevalue` attribute. These attributes have to be used together.

#### `IL1002`: Failed to process XML substitution: 'XML document location'. Unsupported non-boolean feature definition 'feature'

- The substitution in 'XML document location' with feature value 'feature' sets the attribute `featurevalue` to a non-boolean value. Only boolean values are supported for this attribute.

#### `IL1003`: Error processing 'XML document name': 'XmlException'

- There was an error processing a resource linker descriptor, embedded resource linker descriptor or external substitution XML (`ILLink.Substitutions.xml`). The most likely reason for this is that the descriptor file has syntactical errors.

#### `IL1004`: Failed to process description file 'XML document location': 'XmlException'

- There was an error processing an XML descriptor. The most likely reason for this is that the descriptor file has syntactical errors.

#### `IL1005`: Error processing method 'method' in assembly 'assembly'

- There was an error processing method 'method'. An exception with more details is printed.

#### `IL1006`: Cannot stub constructor on 'type' when base type does not have default constructor

- There was an error trying to create a new instance of type 'type'. Its construtor was marked for substitution in a substitution XML, but the base type of 'type' doesn't have a default constructor. Constructors of derived types marked for substitution require to have a default constructor in its base type.

#### `IL1007`: Missing predefined 'type' type

#### `IL1008`: Could not find constructor on 'type'

#### `IL1009`: Assembly 'assembly' reference 'reference' could not be resolved

- There was en error resolving the reference assembly 'reference'. An exception with more details is printed.

#### `IL1010`: Assembly 'assembly' cannot be loaded due to failure in processing 'reference' reference

- The assembly 'assembly' could not be loaded due to an error processing the reference assembly 'reference'. An exception with more details is printed.

#### `IL1011`: Failed to write 'output'

- There was an error writing the linked assembly 'output'. An exception with more details is printed.

#### `IL1012`: IL Linker has encountered an unexpected error. Please report the issue at https://github.com/mono/linker/issues

- There was an unexpected error while linking. An exception with more details is printed to the MSBuild log. Please share this stack trace with the IL Linker team to further investigate the cause and possible solution.

#### `IL1013`: Error processing 'XML document location': 'XmlException'

- There was an error processing 'XML document location' xml file. The most likely reason for this is that the descriptor file has syntactical errors.


----
## Warning Codes

#### `IL2001`: Type 'type' has no fields to preserve

- The XML descriptor preserves fields on type 'type', but this type has no fields.

#### `IL2002`: Type 'type' has no methods to preserve

- The XML descriptor preserves methods on type 'type', but this type has no methods.

#### `IL2003`: Could not resolve 'assembly' assembly dependency specified in a 'PreserveDependency' attribute that targets method 'method'

- The assembly 'assembly' in `PreserveDependency` attribute could not be resolved.

#### `IL2004`: Could not resolve 'type' type dependency specified in a 'PreserveDependency' attribute that targets method 'method'

- The type 'type' in `PreserveDependency` attribute could not be resolved.

#### `IL2005`: Could not resolve dependency member 'member' declared in type 'type' specified in a 'PreserveDependency' attribute that targets method 'method'

- The member 'member' in `PreserveDependency` attribute could not be resolved.

#### `IL2006`: Unrecognized reflection pattern

- The linker found an unrecognized reflection access pattern. The most likely reason for this is that the linker could not resolve a member that is being accessed dynamicallly. To fix this, use the `DynamicallyAccessedMemberAttribute` and specify the member kinds you're trying to access.

#### `IL2007`: Could not resolve assembly 'assembly' specified in the 'XML document location'

- The assembly 'assembly' in the XML could not be resolved.

#### `IL2008`: Could not resolve type 'type' specified in the 'XML document location'

- The type 'type' in the XML could not be resolved.

#### `IL2009`: Could not find method 'method' in type 'type' specified in 'XML document location'

- The 'XML document location' defined a method 'method' on type 'type', but the method was not found.

#### `IL2010`: Invalid value for 'signature' stub

- The value 'value' used in the substitution XML for method 'signature' does not represent a value of a built-in type, or does not match the return type of the method.

#### `IL2011`: Unknown body modification 'action' for 'signature'

- The value 'action' of the body attribute used in the substitution XML is invalid (the only supported options are `remove` and `stub`).

#### `IL2012`: Could not find field 'field' in type 'type' specified in 'XML document location'

- The 'XML document location' defined a field 'field' on type 'type', but the field was not found.

#### `IL2013`: Substituted field 'field' needs to be static field

- The substituted field 'field' was non-static or constant. Only static non-constant fields are supported.

#### `IL2014`: Missing 'value' attribute for field 'field'

- A field was specified for substitution but no value to be substituted was given.

#### `IL2015`: Invalid value for 'field': 'value'

- The value 'value' used in the substitution XML for field 'field' is not a built-in type, or does not match the type of 'field'.

#### `IL2016`: Could not find event 'event' in type 'type' specified in 'XML document location'

- The 'XML document location' defined a event 'event' on type 'type', but the event was not found.

#### `IL2017`: Could not find property 'property' in type 'type' specified in 'XML document location'

- The 'XML document location' defined a property 'property' on type 'type', but the property was not found.

#### `IL2018`: Could not find the get accessor of property 'property' in type 'type' specified in 'XML document location'

- The 'XML document location' defined the get accessor of property 'property' on type 'type', but the accessor was not found.

#### `IL2019`: Could not find the set accessor of property 'property' in type 'type' specified in 'XML document location'

- The 'XML document location' defined the set accessor of property 'property' on type 'type', but the accessor was not found.

#### `IL2020`: DynamicallyAccessedMembers attribute was specified but no argument was proportioned 

- The XML descriptor has a DynamicallyAccessedMembers attribute but the argument 'argument' does not match any of the existing DynamicallyAccessedMemberTypes 

#### `IL2021`: Could not parse argument 'argument' specified in 'XML document location' as a DynamicallyAccessedMemberTypes

- The XML descriptor has a DynamicallyAccessedMembers attribute but the argument 'argument' does not match any of the existing DynamicallyAccessedMemberTypes 

#### `IL2022`: DynamicallyAccessedMembers attribute was specified but there is more than one argument

- The XML descriptor has more than one argument for a single DynamicallyAccessedMembers attribute, there can only be one argument in order to parse it

#### `IL2023`: There is more than one return parameter specified for 'method' in '_xmlDocumentLocation'

- The XML descriptor has more than one return parameter for a single method, there can only be one return parameter
