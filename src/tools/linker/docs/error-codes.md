# ILLink Errors and Warnings

Every linker error and warning has an assigned unique error code for easier
identification. The known codes are in the range 1000 to 6000. Custom
steps should avoid using this range not to collide with existing or future
error and warning codes.

For versioned warnings, the warning version is indicated in parentheses following
the error code. For example:

#### `ILXXXX` (version): Message
- Description of the error code including more details.

## Error Codes

#### `IL1001`: Failed to process 'XML document location'. Feature 'feature' does not specify a "featurevalue" attribute

- The substitution or descriptor in 'XML document location' with feature value 'feature' does not use the `featurevalue` attribute. These attributes have to be used together.

#### `IL1002`: Failed to process 'XML document location'. Unsupported non-boolean feature definition 'feature'

- The substitution or descriptor in 'XML document location' with feature value 'feature' sets the attribute `featurevalue` to a non-boolean value. Only boolean values are supported for this attribute.

#### `IL1003`: Error processing 'XML document name': 'XmlException'

- There was an error processing a resource linker descriptor, embedded resource linker descriptor or external substitution XML (`ILLink.Substitutions.xml`). The most likely reason for this is that the descriptor file has syntactical errors.

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

- There was an error processing 'XML document location' xml file. The most likely reason for this is that the XML file has syntactical errors.

#### `IL1014`: Failed to process 'XML document location`. Unsupported value for featuredefault attribute

- Element in 'XML document location' contains a 'featuredefault' attribute with an invalid value. This attribute only supports the true value, to indicate that this is the default behavior for a feature when a value is not given.

#### `IL1015`: Unrecognized command-line option: 'option'

- The linker was passed a string that was not a linker option.

#### `IL1016`: Invalid warning version 'version'

- The value given for the --warn argument was not a valid warning version. Valid versions include integers in the range 0-9999, though not all of these map to distinct warning waves.

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

#### `IL2006` (5): Unrecognized reflection pattern

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

#### `IL2020`: Argument 'argument' specified in 'XML document location' is of unsupported type 'type'

- The constructor parameter type is not supported in the XML reading code.

#### `IL2021`: Could not parse argument 'argument' specified in 'XML document location' as a 'type'

- The XML descriptor has a 'type' attribute but the argument 'argument' does not match any of the existing enum 'type' values

#### `IL2022`: Could not find a constructor for type 'attribute type' that receives 'number of arguments' arguments as parameter

- The 'attribute type' 'number of arguments' doesnt match with the number of arguments in any of the constructor function described in 'attribute type'

#### `IL2023`: There is more than one return parameter specified for 'method' in 'XML document location'

- The XML descriptor has more than one return parameter for a single method, there can only be one return parameter

#### `IL2024`: There are duplicate parameter names for 'parameter name' inside 'method' in 'XML document location'

- The XML descriptor has more than method parameters with the same name, there can only be one return parameter

#### `IL2025`: Duplicate preserve of 'member' in 'XML document location'

- The XML descriptor marks for preservation the member or type 'member' more than once.

#### `IL2026`: Calling method annotated with `RequiresUnreferencedCodeAttribute`

- The linker found a call to a method which is annotated with 'RequiresUnreferencedCodeAttribute' which can break functionality of a trimmed application.

#### `IL2027`: Attribute 'attribute' should only be used once on 'member'.

- The linker found multiple instances of attribute 'attribute' on 'member'. This attribute is only allowed to have one instance, linker will only use the fist instance and ignore the rest.

#### `IL2028`: Attribute 'attribute' on 'method' doesn't have a required constructor argument.

- The linker found an instance of attribute 'attribute' on 'method' but it lacks a required constructor argument. Linker will ignore this attribute.

#### `IL2029`: Attribute element does not contain attribute 'fullname'

- An attribute element was declared but does not contain the attribute 'fullname' or 'fullname' attribute is empty

#### `IL2030`: Could not resolve assembly 'assembly' in attribute 'attribute' specified in the 'XML document location'

- The assembly 'assembly' described as a attribute property of 'attribute' could not be resolved in 'XML document location'

#### `IL2031`: Attribute type 'attribute type' could not be found

- The described 'attribute type' could not be found in the assemblies

#### `IL2032`: Argument 'argument' specified in 'XML document location' could not be transformed to the constructor parameter type

- The number of arguments correspond to a certain type constructor, but the type of arguments specified in the xml does not match the type of arguments in the constructor.

#### `IL2033`: PreserveDependencyAttribute is deprecated. Use DynamicDependencyAttribute instead.

- PreserveDependencyAttribute was an internal attribute that was never officially supported. Instead, use the similar DynamicDependencyAttribute.

#### `IL2034`: Invalid DynamicDependencyAttribute on 'member'

- The input contains an invalid use of DynamicDependencyAttribute. Ensure that you are using one of the officially supported constructors.

#### `IL2035`: Unresolved assembly 'assemblyName' in DynamicDependencyAttribute on 'member'

- The assembly string given in a DynamicDependencyAttribute constructor could not be resolved. Ensure that the argument specifies a valid asembly name, and that the assembly is available to the linker.

#### `IL2036`: Unresolved type 'typeName' in DynamicDependencyAttribute on 'member'

- The type in a DynamicDependencyAttribute constructor could not be resolved. Ensure that the argument specifies a valid type name or type reference, that the type exists in the specified assembly, and that the assembly is available to the linker.

#### `IL2037`: No members were resolved for 'memberSignature/memberTypes'.

- The member signature or DynamicallyAccessedMemberTypes in a DynamicDependencyAttribute constructor did not resolve to any members on the type. If you using a signature, ensure that it refers to an existing member, and that it uses the format defined at https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format. If using DynamicallyAccessedMemberTypes, ensure that the type contains members of the specified member types.

#### `IL2038`: Missing 'name' attribute for resource.

- The resource element in a substitution file did not have a 'name' attribute. Add a 'name' attribute with the name of the resource to remove.

#### `IL2039`: Invalid 'action' attribute for resource 'resource'.

- The resource element in a substitution file did not have a valid 'action' attribute. Add an 'action' attribute to this element, with value 'remove' to tell the linker to remove this resource.

#### `IL2040`: Could not find embedded resource 'resource' to remove in assembly 'assembly'.

- The resource name in a substitution file could not be found in the specified assembly. Ensure that the resource name matches the name of an embedded resource in the assembly.

#### `IL2041`: DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.

- DynamicallyAccessedMembersAttribute was put directly on the member itself. This is only allowed for instance methods on System.Type and similar classes. Usually this means the attribute should be placed on the return value of the method (or one of its parameters).

#### `IL2042`: Could not find a unique backing field for property 'property' to propagate DynamicallyAccessedMembersAttribute

- The property 'property' has DynamicallyAccessedMembersAttribute on it, but the linker could not determine the backing fields for the property to propagate the attribute to the field.

#### `IL2043`: Trying to propagate DynamicallyAccessedMemberAttribute from property 'property' to its getter 'method', but it already has such attribute.

- Propagating DynamicallyAccessedMembersAttribute from property 'property' to its getter 'method' found that the getter already has such an attribute. The existing attribute will be used.

#### `IL2044`: Could not find any type in namespace 'namespace' specified in 'XML document location'

- The 'XML document location' specifies a namespace 'namespace' but there are no types found in such namespace.

#### `IL2045`: Custom Attribute 'type' is being referenced in code but the linker was instructed to remove all instances of this attribute. If the attribute instances are necessary make sure to either remove the linker attribute XML portion which removes the attribute instances, or to override this use the linker XML descriptor to keep the attribute type (which in turn keeps all of its instances).

- CustomAttribute 'type' is being referenced in the code but the 'type' has been removed using the "remove" attribute tag on a type inside the LinkAttributes xml

#### `IL2046`: Presence of RequiresUnreferencedCodeAttribute on method '<method>' doesn't match overridden method 'base method'. All overridden methods must have RequiresUnreferencedCodeAttribute.

- All overrides of a virtual method including the base method must either have or not have the RequiresUnreferencedCodeAttribute.

#### `IL2047`: DynamicallyAccessedMemberTypes in DynamicallyAccessedMembersAttribute on <member> don't match overridden <base member>. All overriden members must have the same DynamicallyAccessedMembersAttribute usage.

- All overrides of a virtual method including the base method must have the same DynamicallyAccessedMemberAttribute usage on all it's components (return value, parameters and generic parameters).

#### `IL2048`: Internal attribute 'RemoveAttributeInstances' can only be used on a type, but is being used on 'member type' 'member'

- Internal attribute 'RemoveAttributeInstances' is a special attribute that should only be used on custom attribute types and is being used on'member type' 'member'.

#### `IL2049`: Unrecognized internal attribute 'attribute'

- The internal attribute name 'attribute' being used in the xml is not supported by the linker, check the spelling and the supported internal attributes.

#### `IL2050`: Correctness of COM interop cannot be guaranteed

- P/invoke method 'method' declares a parameter with COM marshalling. Correctness of COM interop cannot be guaranteed after trimming. Interfaces and interface members might be removed.
