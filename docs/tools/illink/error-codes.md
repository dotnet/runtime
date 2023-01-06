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

- There was an error trying to create a new instance of type 'type'. Its constructor was marked for substitution in a substitution XML, but the base type of 'type' doesn't have a default constructor. Constructors of derived types marked for substitution require to have a default constructor in its base type.

#### `IL1007`: Missing predefined 'type' type

#### `IL1008`: Could not find constructor on 'type'

#### `IL1009`: Assembly reference 'name' could not be resolved

- There was an error resolving assembly 'name'. Consider adding missing reference to your project or pass `--skip-unresolved` option if you are sure the dependencies don't need to be analyzed.

#### `IL1010`: Assembly 'assembly' cannot be loaded due to failure in processing 'reference' reference

- The assembly 'assembly' could not be loaded due to an error processing the reference assembly 'reference'. An exception with more details is printed.

#### `IL1011`: Failed to write 'output'

- There was an error writing the linked assembly 'output'. An exception with more details is printed.

#### `IL1012`: IL Linker has encountered an unexpected error. Please report the issue at https://github.com/dotnet/linker/issues

- There was an unexpected error while trimming. An exception with more details is printed to the MSBuild log. Please share this stack trace with the IL Linker team to further investigate the cause and possible solution.

#### `IL1013`: Error processing 'XML document location': 'XmlException'

- There was an error processing 'XML document location' xml file. The most likely reason for this is that the XML file has syntactical errors.

#### `IL1014`: Failed to process 'XML document location`. Unsupported value for featuredefault attribute

- Element in 'XML document location' contains a 'featuredefault' attribute with an invalid value. This attribute only supports the true value, to indicate that this is the default behavior for a feature when a value is not given.

#### `IL1015`: Unrecognized command-line option: 'option'

- The linker was passed a string that was not a linker option.

#### `IL1016`: Invalid warning version 'version'

- The value given for the --warn argument was not a valid warning version. Valid versions include integers in the range 0-9999, though not all of these map to distinct warning waves.

#### `IL1017`: Invalid value 'value' for '--generate-warning-suppressions' option

- Invalid value 'value' was used for command-line option '--generate-warning-suppressions'; must be 'cs' or 'xml'.

#### `IL1018`: Missing argument for '{optionName}' option

- The command-line option 'optionName' was specified but no argument was given.

#### `IL1019`: Value used with '--custom-data' has to be in the KEY=VALUE format

- The command-line option --custom-data receives a key-value pair using the format KEY=VALUE.

#### `IL1020`: No files to link were specified. Use one of '{resolvers}' options

#### `IL1021`: Options '--new-mvid' and '--deterministic' cannot be used at the same time

#### `IL1022`: The assembly '{arg}' specified for '--custom-step' option could not be found

#### `IL1023`: The path to the assembly '{arg}' specified for '--custom-step' must be fully qualified

#### `IL1024`: Invalid value '{arg}' specified for '--custom-step' option

- There was an error in the format of the custom step 'arg' given.

#### `IL1025`: Expected '+' or '-' to control new step insertion

- A custom step that is inserted relative to an existing step in the pipeline must specify whether to be added before (-) or after (+) the step it's relative to.

#### `IL1026`: Pipeline step '{name}' could not be found

- A custom step was specified for insertion relative to a non existent step 'name'.

#### `IL1027`: Custom step '{type}' could not be found

- The custom step 'type' could not be found in the given assembly.

#### `IL1028`: Custom step '{type}' is incompatible with this linker version

#### `IL1029`: Invalid optimization value '{text}'

- The optimization 'text' is invalid. Optimization values can either be 'beforefieldinit', 'overrideremoval', 'unreachablebodies', 'unusedinterfaces', 'ipconstprop', or 'sealer'.

#### `IL1030`: Invalid argument for '{token}' option

#### `IL1031`: Invalid assembly action '{action}'

#### `IL1032`: Root assembly '{assemblyFile}' could not be found

#### `IL1033`: XML descriptor file '{xmlFile}' could not be found

#### `IL1034`: Root assembly '{name}' does not have entry point

#### `IL1035`: Root assembly '{name}' cannot use action '{action}'

#### `IL1036`: Invalid assembly name '{assemblyName}'

#### `IL1037`: Invalid assembly root mode '{mode}'

#### `IL1038`: Exported type '{type.Name}' cannot be resolved

#### `IL1039`: Reference assembly '{assemblyPath}' could not be loaded

- A reference assembly input passed via -reference could not be loaded.

#### `IL1040`: Failed to resolve {name}

- Metadata element 'name' cannot be resolved. This usually means there is a version mismatch between dependencies.

#### `IL1041`: The type '{typeName}' used with attribute value 'value' could not be found

- The type name used to define custom attribute value could not be resolved. This can mean the assembly reference is missing or that the type does not exist.

#### `IL1042`: Cannot convert value '{value}' to type '{typeName}'

- The 'value' specified for the custom attribute value cannot be converted to specified argument type 'typeName'.

#### `IL1043`: Custom attribute argument for 'type' type requires nested 'argument' node

- The syntax for custom attribute value for 'type' requires to also specify the underlying attribute type.

#### `IL1044`: Could not resolve custom attribute type value '{value}'

- The 'value' specified for the custom attribute of `System.Type` type could not be resolved.

#### `IL1045`: Unexpected attribute argument type 'type'

- The type name used with attribute type is not one of the supported types.

#### `IL1046`: Invalid metadata '{name}' option

----
## Warning Codes

#### `IL2001`: Type 'type' has no fields to preserve

- The XML descriptor preserves fields on type 'type', but this type has no fields.
  ```XML
  <linker>
    <assembly fullname="test">
      <type fullname="TestType" preserve="fields" />
    </assembly>
  </linker>
  ```
  ```C#
  // IL2001: Type 'TestType' has no fields to preserve
  class TestType
  {
      void OnlyMethod() {}
  }
  ```


#### `IL2002`: Type 'type' has no methods to preserve

- The XML descriptor preserves methods on type 'type', but this type has no methods.

  ```XML
  <linker>
    <assembly fullname="test">
      <type fullname="TestType" preserve="methods" />
    </assembly>
  </linker>
  ```
  ```C#
  // IL2001: Type 'TestType' has no methods to preserve
  struct TestType
  {
      public int Number;
  }
  ```

#### `IL2003`: Could not resolve dependency assembly 'assembly name' specified in a 'PreserveDependency' attribute

- The assembly 'assembly' in `PreserveDependency` attribute could not be resolved.

  ```C#
  // IL2003: Could not resolve dependency assembly 'NonExistentAssembly' specified in a 'PreserveDependency' attribute
  [PreserveDependency("MyMethod", "MyType", "NonExistentAssembly")]
  void TestMethod()
  {
  }
  ```

#### `IL2004`: Could not resolve dependency type 'type' specified in a 'PreserveDependency' attribute

- The type 'type' in `PreserveDependency` attribute could not be resolved.

  ```C#
  // IL2004: Could not resolve dependency type 'NonExistentType' specified in a 'PreserveDependency' attribute
  [PreserveDependency("MyMethod", "NonExistentType", "MyAssembly")]
  void TestMethod()
  {
  }
  ```

#### `IL2005`: Could not resolve dependency member 'member' declared in type 'type' specified in a 'PreserveDependency' attribute

- The member 'member' in `PreserveDependency` attribute could not be resolved.

  ```C#
  // IL2005: Could not resolve dependency member 'NonExistentMethod' declared on type 'MyType' specified in a 'PreserveDependency' attribute
  [PreserveDependency("NonExistentMethod", "MyType", "MyAssembly")]
  void TestMethod()
  {
  }
  ```

#### `IL2007`: Could not resolve assembly 'assembly'

- The assembly 'assembly' in the XML could not be resolved.

  ```XML
  <!-- IL2007: Could not resolve assembly 'NonExistentAssembly' -->
  <linker>
    <assembly fullname="NonExistentAssembly" />
  </linker>
  ```

#### `IL2008`: Could not resolve type 'type'

- The type 'type' in the XML could not be resolved.

  ```XML
  <!-- IL2008: Could not resolve type 'NonExistentType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="NonExistentType" />
    </assembly>
  </linker>
  ```

#### `IL2009`: Could not find method 'method' on type 'type'

- The 'XML document location' defined a method 'method' on type 'type', but the method was not found.

  ```XML
  <!-- IL2009: Could not find method 'NonExistentMethod' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="NonExistentMethod" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2010`: Invalid value for 'signature' stub

- The value used in the substitution XML for method 'signature' does not represent a value of a built-in type, or does not match the return type of the method.

  ```XML
  <!-- IL2010: Invalid value for 'MyType.MyMethodReturningInt()' stub -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethodReturningInt" body="stub" value="NonNumber" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2011`: Unknown body modification 'action' for 'signature'

- The value 'action' of the body attribute used in the substitution XML is invalid (the only supported options are `remove` and `stub`).

  ```XML
  <!-- IL2010: Unknown body modification 'nonaction' for 'MyType.MyMethod()' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod" body="nonaction" value="NonNumber" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2012`: Could not find field 'field' on type 'type'

- The 'XML document location' defined a field 'field' on type 'type', but the field was not found.

  ```XML
  <!-- IL2012: Could not find field 'NonExistentField' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <field name="NonExistentField" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2013`: Substituted field 'field' needs to be static field

- The substituted field 'field' was non-static or constant. Only static non-constant fields are supported.

  ```XML
  <!-- IL2013: Substituted field 'MyType.InstanceField' needs to be static field -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <field name="InstanceField" value="5" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2014`: Missing 'value' attribute for field 'field'

- A field was specified for substitution but no value to be substituted was given.

  ```XML
  <!-- IL2014: Missing 'value' attribute for field 'MyType.MyField' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <field name="MyField" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2015`: Invalid value 'value' for 'field'

- The value 'value' used in the substitution XML for field 'field' is not a built-in type, or does not match the type of 'field'.

  ```XML
  <!-- IL2015: Invalid value 'NonNumber' for 'MyType.IntField' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <field name="IntField" value="NonNumber" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2016`: Could not find event 'event' on type 'type'

- The 'XML document location' defined a event 'event' on type 'type', but the event was not found.

  ```XML
  <!-- IL2016: Could not find event 'NonExistentEvent' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <event name="NonExistentEvent" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2017`: Could not find property 'property' on type 'type'

- The 'XML document location' defined a property 'property' on type 'type', but the property was not found.

  ```XML
  <!-- IL2017: Could not find property 'NonExistentProperty' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <property name="NonExistentProperty" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2018`: Could not find the get accessor of property 'property' on type 'type'

- The 'XML document location' defined the get accessor of property 'property' on type 'type', but the accessor was not found.

  ```XML
  <!-- IL2018: Could not find the get accessor of property 'SetOnlyProperty' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <property signature="System.Boolean SetOnlyProperty" accessors="get" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2019`: Could not find the set accessor of property 'property' on type 'type'

- The 'XML document location' defined the set accessor of property 'property' on type 'type', but the accessor was not found.

  ```XML
  <!-- IL2019: Could not find the set accessor of property 'GetOnlyProperty' on type 'MyType' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <property signature="System.Boolean GetOnlyProperty" accessors="set" />
      </type>
    </assembly>
  </linker>
  ```

#### `IL2022`: Could not find matching constructor for custom attribute 'attribute-type' arguments

- The XML attribute arguments for attribute type 'attribute-type' use values or types which don't match to any constructor on 'attribute-type'

  ```XML
  <!-- IL2022: Could not find matching constructor for custom attribute 'attribute-type' arguments -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <attribute fullname="AttributeWithNoParametersAttribute">
          <argument>ExtraArgumentValue</argument>
        </attribute>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2023`: There is more than one 'return' child element specified for method 'method'

- Method 'method' has more than one `return` element specified. There can only be one `return` element to specify attribute on the return parameter of the method.

  ```XML
  <!-- IL2023: There is more than one 'return' child element specified for method 'method' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod">
          <return>
            <attribute fullname="FirstAttribute"/>
          </return>
          <return>
            <attribute fullname="SecondAttribute"/>
          </return>
        </method>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2024`: More than one value specified for parameter 'parameter' of method 'method'

- Method 'method' has more than one `parameter` element for parameter 'parameter'. There can only be one value specified for each parameter.

  ```XML
  <!-- IL2024: More than one value specified for parameter 'parameter' of method 'method' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod">
          <parameter name="methodParameter">
            <attribute fullname="FirstAttribute"/>
          </parameter>
          <parameter name="methodParameter">
            <attribute fullname="SecondAttribute"/>
          </parameter>
        </method>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2025`: Duplicate preserve of 'member'

- The XML descriptor marks for preservation the member or type 'member' more than once.

  ```XML
  <!-- IL2024: More than one value specified for parameter 'parameter' of method 'method' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod"/>
        <method name="MyMethod"/>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2026` Trim analysis: Using member 'method' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. [message]. [url]

- The linker found a call to a member which is annotated with `RequiresUnreferencedCodeAttribute` which can break functionality of a trimmed application.

  ```C#
  [RequiresUnreferencedCode("Use 'MethodFriendlyToTrimming' instead", Url="http://help/unreferencedcode")]
  void MethodWithUnreferencedCodeUsage()
  {
  }

  void TestMethod()
  {
      // IL2026: Using member 'MethodWithUnreferencedCodeUsage' which has 'RequiresUnreferencedCodeAttribute' 
      // can break functionality when trimming application code. Use 'MethodFriendlyToTrimming' instead. http://help/unreferencedcode
      MethodWithUnreferencedCodeUsage();
  }
  ```

#### `IL2027`: Attribute 'attribute' should only be used once on 'member'.

- The linker found multiple instances of attribute 'attribute' on 'member'. This attribute is only allowed to have one instance, linker will only use the fist instance and ignore the rest.

  ```C#
  // Note: C# won't allow this because RequiresUnreferencedCodeAttribute only allows one instantiation,
  // but it's a good demonstration (it's possible to get to this state using LinkAttributes.xml)

  // IL2027: Attribute 'RequiresUnreferencedCodeAttribute' should only be used once on 'MethodWithUnreferencedCodeUsage()'.
  [RequiresUnreferencedCode("Use A instead")]
  [RequiresUnreferencedCode("Use B instead")]
  void MethodWithUnreferencedCodeUsage()
  {
  }
  ```

#### `IL2028`: Attribute 'attribute' doesn't have the required number of parameters specified

- The linker found an instance of attribute 'attribute' on 'method' but it lacks a required constructor parameter or it has more parameters than accepted. Linker will ignore this attribute.
This is technically possible if a custom assembly defines for example the `RequiresUnreferencedCodeAttribute` type with parameterless constructor and uses it. ILLink will recognize the attribute since it only does a namespace and type name match, but it expects it to have exactly one parameter in its constructor.

#### `IL2029`: 'attribute' element does not contain required attribute 'fullname' or it's empty

- An 'attribute' element must have an attribute 'fullname' with a non-empty value

  ```XML
  <!-- IL2029: 'attribute' element does not contain required attribute 'fullname' or it's empty -->
  <linker>
    <assembly fullname="MyAssembly">
      <attribute/>
    </assembly>
  </linker>
  ```

#### `IL2030`: Could not resolve assembly 'assembly' for attribute 'attribute'

- The assembly name 'assembly' specified for attribute with full name 'attribute' could not be resolved

  ```XML
  <!-- IL2030: Could not resolve assembly 'NonExistentAssembly' for attribute 'MyAttribute' -->
  <linker>
    <assembly fullname="MyAssembly">
      <attribute fullname="MyAttribute" assembly="NonExistentAssembly"/>
    </assembly>
  </linker>
  ```

#### `IL2031`: Attribute type 'attribute type' could not be found

- The described 'attribute type' could not be found in the assemblies

  ```XML
  <!-- IL2031: Attribute type 'NonExistentTypeAttribute' could not be found -->
  <linker>
    <assembly fullname="MyAssembly">
      <attribute fullname="NonExistentTypeAttribute"/>
    </assembly>
  </linker>
  ```

#### `IL2032`: Trim analysis: Unrecognized value passed to the parameter 'parameter' of method 'CreateInstance'. It's not possible to guarantee the availability of the target type.

- The value passed as the assembly name or type name to the `CreateInstance` method can't be statically analyzed, ILLink can't make sure that the type is available.

  ``` C#
  void TestMethod(string assemblyName, string typeName)
  {
      // IL2032 Trim analysis: Unrecognized value passed to the parameter 'typeName' of method 'System.Activator.CreateInstance(string, string)'. It's not possible to guarantee the availability of the target type.
      Activator.CreateInstance("MyAssembly", typeName);

      // IL2032 Trim analysis: Unrecognized value passed to the parameter 'assemblyName' of method 'System.Activator.CreateInstance(string, string)'. It's not possible to guarantee the availability of the target type.
      Activator.CreateInstance(assemblyName, "MyType");
  }
  ```

#### `IL2033`: 'PreserveDependencyAttribute' is deprecated. Use 'DynamicDependencyAttribute' instead.

- `PreserveDependencyAttribute` was an internal attribute that was never officially supported. Instead, use the similar `DynamicDependencyAttribute`.

  ```C#
  // IL2033: 'PreserveDependencyAttribute' is deprecated. Use 'DynamicDependencyAttribute' instead.
  [PreserveDependency("OtherMethod")]
  public void TestMethod()
  {
  }
  ```

#### `IL2034`: The 'DynamicDependencyAttribute' could not be analyzed

- The input contains an invalid use of `DynamicDependencyAttribute`. Ensure that you are using one of the officially supported constructors.
This is technically possible if a custom assembly defines `DynamicDependencyAttribute` with a different constructor than the one the ILLink recognizes. ILLink will recognize the attribute since it only does a namespace and type name match, but the actual instantiation was not recognized.

#### `IL2035`: Unresolved assembly 'assemblyName' in 'DynamicDependencyAttribute'

- The assembly string 'assemblyName' given in a `DynamicDependencyAttribute` constructor could not be resolved. Ensure that the argument specifies a valid assembly name, and that the assembly is available to the linker.

  ```C#
  // IL2035: Unresolved assembly 'NonExistentAssembly' in 'DynamicDependencyAttribute'
  [DynamicDependency("Method", "Type", "NonExistentAssembly")]
  public void TestMethod()
  {
  }
  ```

#### `IL2036`: Unresolved type 'typeName' in 'DynamicDependencyAttribute'

- The type in a `DynamicDependencyAttribute` constructor could not be resolved. Ensure that the argument specifies a valid type name or type reference, that the type exists in the specified assembly, and that the assembly is available to the linker.

  ```C#
  // IL2036: Unresolved type 'NonExistentType' in 'DynamicDependencyAttribute'
  [DynamicDependency("Method", "NonExistentType", "MyAssembly")]
  public void TestMethod()
  {
  }
  ```

#### `IL2037`: No members were resolved for 'memberSignature/memberTypes'.

- The member signature or `DynamicallyAccessedMemberTypes` in a `DynamicDependencyAttribute` constructor did not resolve to any members on the type. If you are using a signature, ensure that it refers to an existing member, and that it uses the format defined at https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format. If using `DynamicallyAccessedMemberTypes`, ensure that the type contains members of the specified member types.

  ```C#
  // IL2036: No members were resolved for 'NonExistingMethod'.
  [DynamicDependency("NonExistingMethod", "MyType", "MyAssembly")]
  public void TestMethod()
  {
  }
  ```

#### `IL2038`: Missing 'name' attribute for resource.

- The `resource` element in a substitution file did not have a `name` attribute. Add a `name` attribute with the name of the resource to remove.

  ```XML
  <!-- IL2038: Missing 'name' attribute for resource. -->
  <linker>
    <assembly fullname="MyAssembly">
      <resource />
    </assembly>
  </linker>
  ```

#### `IL2039`: Invalid value 'value' for attribute 'action' for resource 'resource'.

- The resource element in a substitution file did not have a valid 'action' attribute. Add an 'action' attribute to this element, with value 'remove' to tell the linker to remove this resource.

  ```XML
  <!-- IL2039: Invalid value 'NonExistentAction' for attribute 'action' for resource 'MyResource'. -->
  <linker>
    <assembly fullname="MyAssembly">
      <resource name="MyResource" action="NonExistentAction"/>
    </assembly>
  </linker>
  ```

#### `IL2040`: Could not find embedded resource 'resource' to remove in assembly 'assembly'.

- The resource name in a substitution file could not be found in the specified assembly. Ensure that the resource name matches the name of an embedded resource in the assembly.

  ```XML
  <!-- IL2040: Could not find embedded resource 'NonExistentResource' to remove in assembly 'MyAssembly'. -->
  <linker>
    <assembly fullname="MyAssembly">
      <resource name="NonExistentResource" action="remove"/>
    </assembly>
  </linker>
  ```

#### `IL2041` Trim analysis: The 'DynamicallyAccessedMembersAttribute' is not allowed on methods. It is allowed on method return value or method parameters though.

- `DynamicallyAccessedMembersAttribute` was put directly on the method itself. This is only allowed for instance methods on System.Type and similar classes. Usually this means the attribute should be placed on the return value of the method or one of the method parameters.

  ```C#
  // IL2041: The 'DynamicallyAccessedMembersAttribute' is not allowed on methods. It is allowed on method return value or method parameters though.
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)]

  // Instead if it is meant for the return value it should be done like this:
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)]
  public Type GetInterestingType()
  {
    // ...
  }
  ```

#### `IL2042` Trim analysis: Could not find a unique backing field for property 'property' to propagate 'DynamicallyAccessedMembersAttribute'

- The property 'property' has `DynamicallyAccessedMembersAttribute` on it, but the linker could not determine the backing field for the property to propagate the attribute to the field.

  ```C#
  // IL2042: Could not find a unique backing field for property 'MyProperty' to propagate 'DynamicallyAccessedMembersAttribute'
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)]
  public Type MyProperty
  {
    get { return GetTheValue(); }
    set { }
  }

  // To fix this annotate the accessors manually:
  public Type MyProperty
  {
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)] 
    get { return GetTheValue(); }

    [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)]
    set { }
  }
  ```

#### `IL2043` Trim analysis: 'DynamicallyAccessedMembersAttribute' on property 'property' conflicts with the same attribute on its accessor 'method'.

- Propagating `DynamicallyAccessedMembersAttribute` from property 'property' to its accessor 'method' found that the accessor already has such an attribute. The existing attribute will be used.

  ```C#
  // IL2043: 'DynamicallyAccessedMembersAttribute' on property 'MyProperty' conflicts with the same attribute on its accessor 'get_MyProperty'.
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicMethods)]
  public Type MyProperty
  {
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicFields)]
    get { return GetTheValue(); }
  }
  ```

#### `IL2044`: Could not find any type in namespace 'namespace'

- The XML descriptor specifies a namespace 'namespace' but there are no types found in such namespace. This typically means that the namespace is misspelled.

  ```XML
  <!-- IL2044: Could not find any type in namespace 'NonExistentNamespace' -->
  <linker>
    <assembly fullname="MyAssembly">
      <namespace fullname="NonExistentNamespace" />
    </assembly>
  </linker>
  ```

#### `IL2045` Trim analysis: Attribute 'type' is being referenced in code but the linker was instructed to remove all instances of this attribute. If the attribute instances are necessary make sure to either remove the linker attribute XML portion which removes the attribute instances, or override the removal by using the linker XML descriptor to keep the attribute type (which in turn keeps all of its instances).

- Attribute 'type' is being referenced in the code but the attribute instances have been removed using the 'RemoveAttributeInstances' internal attribute inside the LinkAttributes XML.

  ```XML
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyAttribute">
        <attribute internal="RemoveAttributeInstances"/>
      </type>
    </assembly>
  </linker>
  ```

  ```C#
  // This attribute instance will be removed
  [MyAttribute]
  class MyType
  {
  }

  public void TestMethod()
  {
    // IL2045 for 'MyAttribute' reference
    typeof(MyType).GetCustomAttributes(typeof(MyAttribute), false);
  }
  ```

#### `IL2046`: Trim analysis: Member 'member' with 'RequiresUnreferencedCodeAttribute' has a member 'member' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.

- For all interfaces and overrides the implementation 'RequiresUnreferencedCodeAttribute' must match the definition 'RequiresUnreferecedCodeAttribute', either all the members contain the attribute o none of them.

  Here is a list of posible scenarios where the warning can be generated:

  A base member has the attribute but the derived member does not have the attribute
  ```C#
  public class Base
  {
    [RequiresUnreferencedCode("Message")]
    public virtual void TestMethod() {}
  }

  public class Derived : Base
  {
    // IL2046: Base member 'Base.TestMethod' with 'RequiresUnreferencedCodeAttribute' has a derived member 'Derived.TestMethod()' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public override void TestMethod() {}
  }
  ```
  A derived member has the attribute but the overriden base member does not have the attribute
  ```C#
  public class Base
  {
    public virtual void TestMethod() {}
  }

  public class Derived : Base
  {
    // IL2046: Member 'Derived.TestMethod()' with 'RequiresUnreferencedCodeAttribute' overrides base member 'Base.TestMethod()' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    [RequireUnreferencedCode("Message")]
    public override void TestMethod() {}
  }
  ```
  An interface member has the attribute but it's implementation does not have the attribute
  ```C#
  interface IRUC
  {
    [RequiresUnreferencedCode("Message")]
    void TestMethod();
  }

  class Implementation : IRUC
  {
    // IL2046: Interface member 'IRUC.TestMethod()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.TestMethod()' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public void TestMethod () { }
  }
  ```
  An implementation member has the attribute but the interface that implementes does not have the attribute

  ```C#
  interface IRUC
  {
    void TestMethod();
  }

  class Implementation : IRUC
  {
    [RequiresUnreferencedCode("Message")]
    // IL2046: Member 'Implementation.TestMethod()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.TestMethod()' without 'RequiresUnreferencedCodeAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public void TestMethod () { }
  }
  ```

#### `IL2048`: Internal attribute 'RemoveAttributeInstances' can only be used on a type, but is being used on 'member'

- Internal attribute 'RemoveAttributeInstances' is a special attribute that should only be used on custom attribute types and is being used on 'member'.

  ```XML
  <!-- IL2048: Internal attribute 'RemoveAttributeInstances' can only be used on a type, but is being used on 'MyMethod' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod">
          <attribute internal="RemoveAttributeInstances" />
        </method>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2049`: Unrecognized internal attribute 'attribute'

- The internal attribute name 'attribute' being used in the xml is not supported by the linker, check the spelling and the supported internal attributes.

  ```XML
  <!-- IL2049: Unrecognized internal attribute 'InvalidInternalAttributeName' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <method name="MyMethod">
          <attribute internal="InvalidInternalAttributeName" />
        </method>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2050`: Trim analysis: Correctness of COM interop cannot be guaranteed

- P/invoke method 'method' declares a parameter with COM marshalling. Correctness of COM interop cannot be guaranteed after trimming. Interfaces and interface members might be removed.

#### `IL2051`: Property element does not contain attribute 'name'

- An attribute element declares a property but this does not specify its name or is empty.

  ```XML
  <!-- IL2051: Property element does not contain attribute 'name' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <attribute fullname="MyAttribute">
          <property>UnspecifiedPropertyName</property>
        </attribute>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2052`: Property 'propertyName' could not be found

- An attribute element has property 'propertyName' but this could not be found.

  ```XML
  <!-- IL2052: Property 'NonExistentPropertyName' could not be found -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <attribute fullname="MyAttribute">
          <property name="NonExistentPropertyName">SomeValue</property>
        </attribute>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2053`: Invalid value 'propertyValue' for property 'propertyName'

- The value 'propertyValue' used in a custom attribute annotation does not match the type of the attribute's property 'propertyName'.

  ```XML
  <!-- IL2053: Invalid value 'StringValue' for property 'IntProperty' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <attribute fullname="MyAttribute">
          <property name="IntProperty">StringValue</property>
        </attribute>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2054`: Invalid argument value 'argumentValue' for parameter of type 'parameterType' of attribute 'attribute'

- The value 'argumentValue' used in a custom attribute annotation does not match the type of one of the attribute's constructor arguments. The arguments used for a custom attribute annotation should be declared in the same order the constructor uses.

  ```XML
  <!-- IL2054: Invalid argument value 'NonExistentEnumValue' for parameter of type 'MyEnumType' of attribute 'AttributeWithEnumParameterAttribute' -->
  <linker>
    <assembly fullname="MyAssembly">
      <type fullname="MyType">
        <attribute fullname="AttributeWithEnumParameterAttribute">
          <argument>NonExistentEnumValue</argument>
        </attribute>
      </type>
    </assembly>
  </linker>
  ```

#### `IL2055`: Trim analysis: Call to 'System.Type.MakeGenericType' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic type.

- This can be either that the type on which the `MakeGenericType` is called can't be statically determined, or that the type parameters to be used for generic arguments can't be statically determined. If the open generic type has `DynamicallyAccessedMembersAttribute` on any of its generic parameters, ILLink currently can't validate that the requirements are fulfilled by the calling method.

  ``` C#
  class Lazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberType.PublicParameterlessConstructor)] T> 
  {
      // ...
  }
  
  void TestMethod(Type unknownType)
  {
      // IL2055 Trim analysis: Call to `System.Type.MakeGenericType(Type[])` can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic type.
      typeof(Lazy<>).MakeGenericType(new Type[] { typeof(TestType) });

      // IL2055 Trim analysis: Call to `System.Type.MakeGenericType(Type[])` can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic type.
      unknownType.MakeGenericType(new Type[] { typeof(TestType) });
  }
  ```

#### `IL2056`: Trim analysis: 'DynamicallyAccessedMemberAttribute' on property 'property' conflicts with the same attribute on its backing field 'field'.

- Propagating `DynamicallyAccessedMembersAttribute` from property 'property' to its backing field 'field' found that the field already has such an attribute. The existing attribute will be used.
  Since ILLink will only propagate to a compiler generated backing field this warning should basically never happen. The one known way requires the user code to explicitly specify the `CompilerGeneratedAttribute` on the field to get ILLink to treat it as the compiler generated backing field.

#### `IL2057`: Trim analysis: Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(Type typeName)'. It's not possible to guarantee the availability of the target type.

- If the type name passed to the `System.Type.GetType` is statically known ILLink can make sure it's preserved and the application code will work after trimming. But if the type name is unknown, it could point to a type which ILLink will not see being used anywhere else and would remove it from the application, potentially breaking the application.

  ``` C#
  void TestMethod()
  {
      string typeName = ReadName();

      // IL2057 Trim analysis: Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(Type typeName)'
      Type.GetType(typeName);
  }
  ```

#### `IL2058`: Trim analysis: Parameters passed to method 'Assembly.CreateInstance' cannot be analyzed. Consider using methods 'System.Type.GetType' and `System.Activator.CreateInstance` instead.

- ILLink currently doesn't analyze assembly instances and thus it doesn't know on which assembly the `Assembly.CreateInstance` was called. ILLink has support for `Type.GetType` instead, for cases where the parameter is a string literal. The result of which can be passed to `Activator.CreateInstance` to create an instance of the type.

  ``` C#
  void TestMethod()
  {
      // IL2058 Trim analysis: Parameters passed to method 'Assembly.CreateInstance(string)' cannot be analyzed. Consider using methods 'System.Type.GetType' and `System.Activator.CreateInstance` instead.
      AssemblyLoadContext.Default.Assemblies.First(a => a.Name == "MyAssembly").CreateInstance("MyType");

      // This can be replaced by
      Activator.CreateInstance(Type.GetType("MyType, MyAssembly"));
  }
  ```

#### `IL2059`: Trim analysis: Unrecognized value passed to the parameter 'type' of method 'System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor'. It's not possible to guarantee the availability of the target static constructor.

- If the type passed to the `RunClassConstructor` is not statically known, ILLink can't make sure that its static constructor is available.

  ``` C#
  void TestMethod(Type type)
  {
      // IL2059 Trim analysis: Unrecognized value passed to the parameter 'type' of method 'System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(RuntimeTypeHandle type)'. 
      // It's not possible to guarantee the availability of the target static constructor.
      RuntimeHelpers.RunClassConstructor(type.TypeHandle);
  }
  ```

#### `IL2060`: Trim analysis: Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method

- This can be either that the method on which the `MakeGenericMethod` is called can't be statically determined, or that the type parameters to be used for generic arguments can't be statically determined. If the open generic method has `DynamicallyAccessedMembersAttribute` on any of its generic parameters, ILLink currently can't validate that the requirements are fulfilled by the calling method.

``` C#
class Test
{
  public static void TestGenericMethod<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
  {
  }
  
  void TestMethod(Type unknownType)
  {
    // IL2060 Trim analysis: Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method
    typeof(Test).GetMethod("TestGenericMethod").MakeGenericMethod(new Type[] { typeof(TestType) });

    // IL2060 Trim analysis: Call to 'System.Reflection.MethodInfo.MakeGenericMethod' can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method
    unknownMethod.MakeGenericMethod(new Type[] { typeof(TestType) });
  }
}
```

#### `IL2061`: Trim analysis: The assembly name 'assembly name' passed to method 'method' references assembly which is not available.

- Calling `CreateInstance` with assembly name 'assembly name' which can't be resolved.

  ``` C#
  void TestMethod()
  {
      // IL2061 Trim analysis: The assembly name 'NonExistentAssembly' passed to method 'System.Activator.CreateInstance(string, string)' references assembly which is not available.
      Activator.CreateInstance("NonExistentAssembly", "MyType");
  }
  ```

#### `IL2062` Trim analysis: Value passed to parameter 'parameter' of method 'method' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.

- The parameter 'parameter' of method 'method' has a `DynamicallyAccessedMembersAttribute`, but the value passed to it can not be statically analyzed. ILLink can't make sure that the requirements declared by the `DynamicallyAccessedMembersAttribute` are met by the argument value.

  ``` C#
  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  void TestMethod(Type[] types)
  {
      // IL2062: Value passed to parameter 'type' of method 'NeedsPublicConstructors' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
      NeedsPublicConstructors(types[1]);
  }
  ```

#### `IL2063`: Trim analysis: Value returned from method 'method' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.

- The return value of method 'method' has a `DynamicallyAccessedMembersAttribute`, but the value returned from the method can not be statically analyzed. ILLink can't make sure that the requirements declared by the `DynamicallyAccessedMembersAttribute` are met by the returned value.

  ``` C#
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod(Type[] types)
  {
      // IL2063 Trim analysis: Value returned from method 'TestMethod' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
      NeedsPublicConstructors(types[1]);
  }
  ```

#### `IL2064`: Trim analysis: Value assigned to field 'field' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.

- The field 'field' has a `DynamicallyAccessedMembersAttribute`, but the value assigned to it can not be statically analyzed. ILLink can't make sure that the requirements declared by the `DynamicallyAccessedMembersAttribute` are met by the assigned value.

  ``` C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeField;

  void TestMethod(Type[] types)
  {
      // IL2064 Trim analysis: Value assigned to field '_typeField' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
      _typeField = _types[1];
  }
  ```

#### `IL2065`: Trim analysis: Value passed to implicit 'this' parameter of method 'method' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.

- The method 'method' has a `DynamicallyAccessedMembersAttribute` (which applies to the implicit 'this' parameter), but the value used for the 'this' parameter can not be statically analyzed. ILLink can't make sure that the requirements declared by the `DynamicallyAccessedMembersAttribute` are met by the 'this' value.

  ``` C#
  void TestMethod(Type[] types)
  {
      // IL2065 Trim analysis: Value passed to implicit 'this' parameter of method 'Type.GetMethods()' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.
      _types[1].GetMethods (); // Type.GetMethods has [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] attribute
  }
  ```

#### `IL2066`: Trim analysis: Type passed to generic parameter 'parameter' of 'type or method' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.

- The generic parameter 'parameter' of 'type or method' has a `DynamicallyAccessedMembersAttribute`, but the value used for it can not be statically analyzed. ILLink can't make sure that the requirements declared by the `DynamicallyAccessedMembersAttribute` are met by the value.

*Note: this warning can't be currently produced as there's no pure IL way to pass unknown value to a generic parameter. Once ILLInk supports full analysis of arguments for `MakeGenericType`/`MakeGenericMethod` this warnings would become active.*

#### `IL2067`: Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  void TestMethod(Type type)
  {
      // IL2067 Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors(type);
  }
  ```

#### `IL2068`: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod(Type type)
  {
      // IL2068 Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      return type;
  }
  ```

#### `IL2069`: Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeField;

  void TestMethod(Type type)
  {
      // IL2069 Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      _typeField = type;
  }
  ```

#### `IL2070`: Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void TestMethod(Type type)
  {
      // IL2070 Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
  }
  ```

#### `IL2071`: Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The parameter 'source parameter' of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used

#### `IL2072`: Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type GetCustomType() { return typeof(CustomType); }

  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  void TestMethod()
  {
      // IL2072 Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors(GetCustomType());
  }
  ```

#### `IL2073`: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type GetCustomType() { return typeof(CustomType); }

  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod()
  {
      // IL2073 Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      return GetCustomType();
  }
  ```

#### `IL2074`: Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type GetCustomType() { return typeof(CustomType); }

  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeField;

  void TestMethod()
  {
      // IL2074 Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      _typeField = GetCustomType();
  }
  ```

#### `IL2075`: Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type GetCustomType() { return typeof(CustomType); }

  void TestMethod()
  {
      // IL2075 Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      GetCustomType().GetMethods(); // Type.GetMethods is annotated with DynamicallyAccessedMemberTypes.PublicMethods
  }
  ```

#### `IL2076`: Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used

#### `IL2077`: Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  Type _typeField;

  void TestMethod()
  {
      // IL2075 Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors(_typeField);
  }
  ```

#### `IL2078`: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type _typeField;

  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod()
  {
      // IL2076 Trim analysis: 'TestMethod' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The field '_typeField' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      return _typeField;
  }
  ```

#### `IL2079`: Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type _typeField;

  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeFieldWithRequirements;

  void TestMethod()
  {
      // IL2077 Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      _typeFieldWithRequirements = _typeField;
  }
  ```

#### `IL2080`: Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  Type _typeField;

  void TestMethod()
  {
      // IL2078 Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
  }
  ```

#### `IL2081`: Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The field 'source field' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used

#### `IL2082`: Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  // This can only happen within methods of System.Type type (or derived types). Assume the below method is declared on System.Type
  void TestMethod()
  {
      // IL2082 Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors(this);
  }
  ```

#### `IL2083`: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  // This can only happen within methods of System.Type type (or derived types). Assume the below method is declared on System.Type
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod()
  {
      // IL2083 Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      return this;
  }
  ```

#### `IL2084`: Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeFieldWithRequirements;

  // This can only happen within methods of System.Type type (or derived types). Assume the below method is declared on System.Type
  void TestMethod()
  {
      // IL2084 Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      _typeFieldWithRequirements = this;
  }
  ```

#### `IL2085`: Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  // This can only happen within methods of System.Type type (or derived types). Assume the below method is declared on System.Type
  void TestMethod()
  {
      // IL2085 Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      this.GetMethods(); // Type.GetMethods is annotated with DynamicallyAccessedMemberTypes.PublicMethods
  }
  ```

#### `IL2086`: Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The implicit 'this' argument of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used


#### `IL2087`: Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void NeedsPublicConstructors([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
  {
      // ...
  }

  void TestMethod<TSource>()
  {
      // IL2087 Trim analysis: 'target parameter' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors(typeof(TSource));
  }
  ```

#### `IL2088`: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type TestMethod<TSource>()
  {
      // IL2088 Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      return typeof(TSource);
  }
  ```

#### `IL2089`: Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
  Type _typeFieldWithRequirements;

  void TestMethod<TSource>()
  {
      // IL2089 Trim analysis: value stored in field 'target field' does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      _typeFieldWithRequirements = typeof(TSource);
  }
  ```

#### `IL2090`: Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void TestMethod<TSource>()
  {
      // IL2090 Trim analysis: 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to 'target method'. The generic parameter 'source generic parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      typeof(TSource).GetMethods(); // Type.GetMethods is annotated with DynamicallyAccessedMemberTypes.PublicMethods
  }
  ```

#### `IL2091`: Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The generic parameter 'source target parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.

- The target location declares some requirements on the type value via its `DynamicallyAccessedMembersAttribute`. Those requirements must be met by those declared on the source value also via the `DynamicallyAccessedMembersAttribute`. The source value can declare more requirements than the source if necessary.

  ```C#
  void NeedsPublicConstructors<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTarget>()
  {
      // ...
  }

  void TestMethod<TSource>()
  {
      // IL2091 Trim analysis: 'target generic parameter' generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in 'target method or type'. The generic parameter 'source target parameter' of 'source method or type' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
      NeedsPublicConstructors<TSource>();
  }
  ```

#### `IL2092`: Trim analysis: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 'parameter' of method 'method' don't match overridden parameter 'parameter' of method 'base method'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.

- All overrides of a virtual method including the base method must have the same `DynamicallyAccessedMemberAttribute` usage on all it's components (return value, parameters and generic parameters).

  ```C#
  public class Base
  {
    public virtual void TestMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type) {}
  }

  public class Derived : Base
  {
    // IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 'type' of method 'Derived.TestMethod' don't match overridden parameter 'type' of method 'Base.TestMethod'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
    public override void TestMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type) {}
  }
  ```

#### `IL2093`: Trim analysis: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'method' don't match overridden return value of method 'base method'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.

- All overrides of a virtual method including the base method must have the same `DynamicallyAccessedMemberAttribute` usage on all it's components (return value, parameters and generic parameters).

  ```C#
  public class Base
  {
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public virtual Type TestMethod() {}
  }

  public class Derived : Base
  {
    // IL2093: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method 'Derived.TestMethod' don't match overridden return value of method 'Base.TestMethod'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
    public override Type TestMethod() {}
  }
  ```

#### `IL2094`: Trim analysis: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the implicit 'this' parameter of method 'method' don't match overridden implicit 'this' parameter of method 'base method'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.

- All overrides of a virtual method including the base method must have the same `DynamicallyAccessedMemberAttribute` usage on all it's components (return value, parameters and generic parameters).

  ```C#
  // This only works on methods in System.Type and derived classes - this is just an example
  public class Type
  {
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public virtual void TestMethod() {}
  }

  public class DerivedType : Type
  {
    // IL2094: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the implicit 'this' parameter of method 'DerivedType.TestMethod' don't match overridden implicit 'this' parameter of method 'Type.TestMethod'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
    public override void TestMethod() {}
  }
  ```

#### `IL2095`: Trim analysis: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the generic parameter 'generic parameter' of 'method' don't match overridden generic parameter 'generic parameter' of 'base method'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.

- All overrides of a virtual method including the base method must have the same `DynamicallyAccessedMemberAttribute` usage on all it's components (return value, parameters and generic parameters).

  ```C#
  public class Base
  {
    public virtual void TestMethod<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() {}
  }

  public class Derived : Base
  {
    // IL2095: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the generic parameter 'T' of method 'Derived.TestMethod<T>' don't match overridden generic parameter 'T' of method 'Base.TestMethod<T>'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
    public override void TestMethod<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() {}
  }
  ```

#### `IL2096`: Trim analysis: Call to 'Type.GetType method' can perform case insensitive lookup of the type, currently ILLink can not guarantee presence of all the matching types"

- Specifying a case-insensitive search on an overload of `System.Type.GetType` is not supported by ILLink. Specify false to perform a case-sensitive search or use an overload that does not use a ignoreCase bolean.

  ``` C#
  void TestMethod()
  {
      // IL2096 Trim analysis: Call to 'System.Type.GetType(String,Boolean,Boolean)' can perform case insensitive lookup of the type, currently ILLink can not guarantee presence of all the matching types
      Type.GetType ("typeName", false, true);
  }
  ```

#### `IL2097`: Trim analysis: Field 'field' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to fields of type 'System.Type' or 'System.String'

- `DynamicallyAccessedMembersAttribute` is only applicable to items of type `System.Type` or `System.String` (or derived), on all other types the attribute will be ignored. Using the attribute on any other type is likely incorrect and unintentional.

  ```C#
  // IL2097: Field '_valueField' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to fields of type 'System.Type' or 'System.String'
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
  object _valueField;
  ```

#### `IL2098`: Trim analysis: Parameter 'parameter' of method 'method' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to parameters of type 'System.Type' or 'System.String'

- `DynamicallyAccessedMembersAttribute` is only applicable to items of type `System.Type` or `System.String` (or derived), on all other types the attribute will be ignored. Using the attribute on any other type is likely incorrect and unintentional.

  ```C#
  // IL2098: Parameter 'param' of method 'TestMethod' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to parameters of type 'System.Type' or 'System.String'
  void TestMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] object param)
  {
    // param.GetType()....
  }
  ```

#### `IL2099`: Trim analysis: Property 'property' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'

- `DynamicallyAccessedMembersAttribute` is only applicable to items of type `System.Type` or `System.String` (or derived), on all other types the attribute will be ignored. Using the attribute on any other type is likely incorrect and unintentional.

  ```C#
  // IL2099: Parameter 'param' of method 'TestMethod' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
  object TestProperty { get; set; }
  ```

#### `IL2100`: XML contains unsupported wildcard for assembly "fullname" attribute

- A wildcard "fullname" for an assembly in XML is only valid for link attribute XML on the command-line, not for descriptor or substitution XML or for embedded attribute XML. Specify a specific assembly name instead.

  ```XML
  <!-- IL2100: XML contains unsupported wildcard for assembly "fullname" attribute -->
  <linker>
    <assembly fullname="*">
      <type fullname="MyType" />
    </assembly>
  </linker>
  ```

#### `IL2101`: Embedded XML in assembly 'assembly' contains assembly "fullname" attribute for another assembly 'assembly'

- Embedded attribute or substitution XML may only contain elements that apply to the containing assembly. Attempting to modify another assembly will not have any effect.

  ```XML
  <!-- IL2101: Embedded XML in assembly 'ContainingAssembly' contains assembly "fullname" attribute for another assembly 'OtherAssembly' -->
  <linker>
    <assembly fullname="OtherAssembly">
      <type fullname="MyType" />
    </assembly>
  </linker>
  ```

#### `IL2102`: Invalid AssemblyMetadata("IsTrimmable", ...) attribute in assembly 'assembly'. Value must be "True"

- AssemblyMetadataAttribute may be used at the assembly level to turn on trimming for the assembly. The only supported value is "True", but the attribute contained an unsupported value.

  ``` C#
  // IL2102: Invalid AssemblyMetadata("IsTrimmable", "False") attribute in assembly 'assembly'. Value must be "True"
  [assembly: AssemblyMetadata("IsTrimmable", "False")] 
  ```

#### `IL2103`: Trim analysis: Value passed to the 'propertyAccessor' parameter of method 'System.Linq.Expressions.Expression.Property(Expression, MethodInfo)' cannot be statically determined as a property accessor

- The value passed to the `propertyAccessor` parameter of `Expression.Property(expression, propertyAccessor)` was not recognized as a property accessor method. Trimmer can't guarantee the presence of the property.

  ```C#
  void TestMethod(MethodInfo methodInfo)
  {
    // IL2103: Value passed to the 'propertyAccessor' parameter of method 'System.Linq.Expressions.Expression.Property(Expression, MethodInfo)' cannot be statically determined as a property accessor.
    Expression.Property(null, methodInfo);
  }
  ```

#### `IL2104`: Assembly 'assembly' produced trim warnings. For more information see https://aka.ms/dotnet-illink/libraries

- The assembly 'assembly' produced trim analysis warnings in the context of the app. This means the assembly has not been fully annotated for trimming. Consider contacting the library author to request they add trim annotations to the library. To see detailed warnings for this assembly, turn off grouped warnings by passing either `--singlewarn-` to show detailed warnings for all assemblies, or `--singlewarn- "assembly"` to show detailed warnings for that assembly. https://aka.ms/dotnet-illink/libraries has more information on annotating libraries for trimming.

#### `IL2105`: Type 'type' was not found in the caller assembly nor in the base library. Type name strings used for dynamically accessing a type should be assembly qualified.

- Type name strings representing dynamically accessed types must be assembly qualified, otherwise linker will first search for the type name in the caller's assembly and then in System.Private.CoreLib.
  If the linker fails to resolve the type name, null will be returned.

  ```C#
  void TestInvalidTypeName()
  {
      RequirePublicMethodOnAType("Foo.Unqualified.TypeName");
  }
  void RequirePublicMethodOnAType(
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
      string typeName)
  {
  }
  ```

#### `IL2106`: Trim analysis: Return type of method 'method' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'

- `DynamicallyAccessedMembersAttribute` is only applicable to items of type `System.Type` or `System.String` (or derived), on all other types the attribute will be ignored. Using the attribute on any other type is likely incorrect and unintentional.

  ```C#
  // IL2106: Return type of method 'TestMethod' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] object TestMethod()
  {
      return typeof(TestType);
  }
  ```

#### `IL2107`: Trim analysis: Methods 'method1' and 'method2' are both associated with state machine type 'type'. This is currently unsupported and may lead to incorrectly reported warnings.

- Trimmer currently can't correctly handle if the same compiler generated state machine type is associated (via the state machine attributes) with two different methods.
  Since the trimmer currently derives warning suppressions from the method which produced the state machine and currently doesn't support reprocessing the same method/type more than once.

  Only a meta-sample:

  ```C#
  class <compiler_generated_state_machine>_type {
      void MoveNext()
      {
          // This should normally produce IL2026
          CallSomethingWhichRequiresUnreferencedCode ();
      }
  }

  [RequiresUnreferencedCode ("")] // This should suppress all warnings from the method
  [IteratorStateMachine(typeof(<compiler_generated_state_machine>_type))]
  IEnumerable<int> UserDefinedMethod()
  {
      // Uses the state machine type
      // The IL2026 from the state machine should be suppressed in this case
  }

  // IL2107: Methods 'UserDefinedMethod' and 'SecondUserDefinedMethod' are both associated with state machine type '<compiler_generated_state_machine>_type'.
  [IteratorStateMachine(typeof(<compiler_generated_state_machine>_type))]
  IEnumerable<int> SecondUserDefinedMethod()
  {
      // Uses the state machine type
      // The IL2026 from the state should be reported in this case
  }
  ```

#### `IL2108`: Invalid scope 'scope' used in 'UnconditionalSuppressMessageAttribute' on module 'module' with target 'target'.

The only scopes supported on global unconditional suppressions are 'module', 'type' and 'member'. If the scope and target arguments are null or missing on a global suppression,
it is assumed that the suppression is put on the module. Global unconditional suppressions using invalid scopes are ignored.

```C#
// IL2108: Invalid scope 'method' used in 'UnconditionalSuppressMessageAttribute' on module 'Warning' with target 'MyTarget'.
[module: UnconditionalSuppressMessage ("Test suppression with invalid scope", "IL2026", Scope = "method", Target = "MyTarget")]

class Warning
{
   static void Main(string[] args)
   {
      Foo();
   }

   [RequiresUnreferencedCode("Warn when Foo() is called")]
   static void Foo()
   {
   }
}
```

#### `IL2109` Trim analysis: Type 'type' derives from 'BaseType' which has 'RequiresUnreferencedCodeAttribute'. [message]. [url]

- A type is being referenced in code, and this type derives from a base type with 'RequiresUnreferencedCodeAttribute' which can break functionality of a trimmed application.
  Types that derive from a base class with 'RequiresUnreferencedCodeAttribute' need to explicitly use the 'RequiresUnreferencedCodeAttribute' or suppress this warning

  ```C#
  [RequiresUnreferencedCode("Using any of the members inside this class is trim unsafe", Url="http://help/unreferencedcode")]
  public class UnsafeClass {
     public UnsafeClass () {}
     public static void UnsafeMethod();
  }

  // IL2109: Type 'Derived' derives from 'UnsafeClass' which has 'RequiresUnreferencedCodeAttribute'. Using any of the members inside this class is trim unsafe. http://help/unreferencedcode
  class Derived : UnsafeClass {}
  ```

#### `IL2110`: Trim analysis: Field 'field' with 'DynamicallyAccessedMembersAttribute' is accessed via reflection. Trimmer can't guarantee availability of the requirements of the field.

- Trimmer currently can't guarantee that all requirements of the `DynamicallyAccessedMembersAttribute` are fulfilled if the field is accessed via reflection.

```C#
[DynamicallyAccessedMembers(DynamicallyAccessedMemeberTypes.PublicMethods)]
Type _field;

void TestMethod()
{
    // IL2110: Field '_field' with 'DynamicallyAccessedMembersAttribute' is accessed via reflection. Trimmer can't guarantee availability of the requirements of the field.
    typeof(Test).GetField("_field");
}
```

#### `IL2111`: Trim analysis: Method 'method' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.

- Trimmer currently can't guarantee that all requirements of the `DynamicallyAccessedMembersAttribute` are fulfilled if the method is accessed via reflection.

```C#
void MethodWithRequirements([DynamicallyAccessedMembers(DynamicallyAccessedMemeberTypes.PublicMethods)] Type type)
{
}

void TestMethod()
{
    // IL2111: Method 'MethodWithRequirements' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
    typeof(Test).GetMethod("MethodWithRequirements");
}
```

#### `IL2112` Trim analysis: 'DynamicallyAccessedMembersAttribute' on 'type' or one of its base types references 'member' which requires unreferenced code. [message]. [url]

- A type is annotated with `DynamicallyAccessedMembersAttribute` indicating that the type may dynamically access some members declared on the type or its derived types. This instructs the trimmer to keep the specified members, but one of them is annotated with `RequiresUnreferencedCodeAttribute` which can break functionality when trimming. The `DynamicallyAccessedMembersAttribute` annotation may be directly on the type, or implied by an annotation on one of its base or interface types. This warning originates from the member with `RequiresUnreferencedCodeAttribute`.

  ```C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
  public class AnnotatedType {
      // Trim analysis warning IL2112: AnnotatedType.Method(): 'DynamicallyAccessedMembersAttribute' on 'AnnotatedType' or one of its
      // base types references 'AnnotatedType.Method()' which requires unreferenced code. Using this member is trim unsafe.
      [RequiresUnreferencedCode("Using this member is trim unsafe")]
      public static void Method() { }
  }
  ```

#### `IL2113` Trim analysis: 'DynamicallyAccessedMembersAttribute' on 'type' or one of its base types references 'member' which requires unreferenced code. [message]. [url]

- A type is annotated with `DynamicallyAccessedMembersAttribute` indicating that the type may dynamically access some members declared on the type or its derived types. This instructs the trimmer to keep the specified members, but a member of one of the base or interface types is annotated with `RequiresUnreferencedCodeAttribute` which can break functionality when trimming. The `DynamicallyAccessedMembersAttribute` annotation may be directly on the type, or implied by an annotation on one of its base or interface types. This warning originates from the type which has `DynamicallyAccessedMembersAttribute` requirements.

  ```C#
  public class BaseType {
      [RequiresUnreferencedCode("Using this member is trim unsafe")]
      public static void Method() { }
  }

  // Trim analysis warning IL2113: AnnotatedType: 'DynamicallyAccessedMembersAttribute' on 'AnnotatedType' or one of its
  // base types references 'BaseType.Method()' which requires unreferenced code. Using this member is trim unsafe.
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
  public class AnnotatedType : BaseType {
  }
  ```

#### `IL2114 ` Trim analysis: 'DynamicallyAccessedMembersAttribute' on 'type' or one of its base types references 'member' which has 'DynamicallyAccessedMembersAttribute' requirements.

- A type is annotated with `DynamicallyAccessedMembersAttribute` indicating that the type may dynamically access some members declared on the type or its derived types. This instructs the trimmer to keep the specified members, but one of them is annotated with `DynamicallyAccessedMembersAttribute` which can not be statically verified. The `DynamicallyAccessedMembersAttribute` annotation may be directly on the type, or implied by an annotation on one of its base or interface types. This warning originates from the member with `DynamicallyAccessedMembersAttribute` requirements.

  ```C#
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
  public class AnnotatedType {
      // Trim analysis warning IL2114: System.Type AnnotatedType::Field: 'DynamicallyAccessedMembersAttribute' on 'AnnotatedType' or one of its
      // base types references 'System.Type AnnotatedType::Field' which has 'DynamicallyAccessedMembersAttribute' requirements .
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
      public static Type Field;
  }
  ```

#### `IL2115` Trim analysis: 'DynamicallyAccessedMembersAttribute' on 'type' or one of its base types references 'member' which has 'DynamicallyAccessedMembersAttribute' requirements.

- A type is annotated with `DynamicallyAccessedMembersAttribute` indicating that the type may dynamically access some members declared on the type or its derived types. This instructs the trimmer to keep the specified members, but a member of one of the base or interface types is annotated with `DynamicallyAccessedMembersAttribute` which can not be statically verified. The `DynamicallyAccessedMembersAttribute` annotation may be directly on the type, or implied by an annotation on one of its base or interface types. This warning originates from the type which has `DynamicallyAccessedMembersAttribute` requirements.

  ```C#
  public class BaseType {
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
      public static Type Field;
  }

  // Trim analysis warning IL2115: AnnotatedType: 'DynamicallyAccessedMembersAttribute' on 'AnnotatedType' or one of its
  // base types references 'System.Type BaseType::Field' which has 'DynamicallyAccessedMembersAttribute' requirements .
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
  public class AnnotatedType : BaseType {
  }
  ```

#### `IL2116` Trim analysis: 'RequiresUnreferencedCodeAttribute' cannot be placed directly on static constructor 'static constructor', consider placing 'RequiresUnreferencedCodeAttribute' on the type declaration instead.

- The use of 'RequiresUnreferencedCodeAttribute' on static constructors is disallowed since is a method not callable by the user, is only called by the runtime. Placing the attribute directly on the static constructor will have no effect, instead use 'RequiresUnreferencedCodeAttribute' on the type which will handle warning and silencing from the static constructor.

  ```C#
  public class MyClass {
      // Trim analysis warning IL2115: 'RequiresUnreferencedCodeAttribute' cannot be placed directly on static constructor 'MyClass..cctor()', consider placing 'RequiresUnreferencedCodeAttribute' on the type declaration instead.
      [RequiresUnreferencedCode("Dangerous")]
      static MyClass () { }
  }
  ```

#### `IL2117`: Trim analysis: Methods 'method1' and 'method2' are both associated with lambda or local function 'method'. This is currently unsupported and may lead to incorrectly reported warnings.

- Trimmer currently can't correctly handle if the same compiler generated lambda or local function is associated with two different methods. We don't know of any C# patterns which would cause this problem, but it is possible to write code like this in IL.

  Only a meta-sample:

  ```C#
  [RequiresUnreferencedCode ("")] // This should suppress all warnings from the method
  void UserDefinedMethod()
  {
      // Uses the compiler-generated local function method
      // The IL2026 from the local function should be suppressed in this case
  }

  // IL2107: Methods 'UserDefinedMethod' and 'SecondUserDefinedMethod' are both associated with state machine type '<compiler_generated_state_machine>_type'.
  [RequiresUnreferencedCode ("")] // This should suppress all warnings from the method
  void SecondUserDefinedMethod()
  {
      // Uses the compiler-generated local function method
      // The IL2026 from the local function should be suppressed in this case
  }

  internal static void <UserDefinedMethod>g__LocalFunction|0_0()
  {
      // Compiler-generated method emitted for a local function.
      // This should only ever be called from one user-defined method.
  }

  ```

#### `IL2121`: Unused 'UnconditionalSuppressMessageAttribute' for warning 'warning'. Consider removing the unused warning suppression.

- The 'UnconditionalSuppressMessageAttribute' did not suppress any warning 'warning' caused by trimmer-incompatible patterns. Consider removing the attribute.

  ```C#
  // Trim analysis warning IL2121: TestMethod(): Unused 'UnconditionalSuppressMessageAttribute' for warning 'IL2070'. Consider removing the unused warning suppression.
  [UnconditionalSuppressMessage("trim", "IL2070")]
  void TestMethod()
  {
      Console.WriteLine("test");
  }
  ```


## Single-File Warning Codes

#### `IL3000`: 'member' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'

- Calls to 'System.Reflection.Assembly.Location', 'System.Reflection.AssemblyName.CodeBase' and 'System.Reflection.AssemblyName.EscapedCodeBase' return an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'

  ``` C#
  void TestMethod()
  {
      var a = Assembly.GetExecutingAssembly();
      // IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
      _ = a.Location;
  }
  ```

#### `IL3001`: Assemblies embedded in a single-file app cannot have additional files in the manifest.

- Calls to 'Assembly.GetFile(s)' methods for assemblies embedded inside the single-file bundle always throws an exception. Consider using embedded resources and the 'Assembly.GetManifestResourceStream' method.

  ``` C#
  void TestMethod()
  {
      var a = Assembly.GetExecutingAssembly();
      // IL3001: Assemblies embedded in a single-file app cannot have additional files in the manifest.
      _ = a.GetFiles();
  }
  ```

#### `IL3002`: Using member 'member' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app. [message]. [url]

- The linker found a call to a member annotated with 'RequiresAssemblyFilesAttribute' which can break functionality of a single-file application.

  ```C#
  [RequiresAssemblyFiles(Message="Use 'MethodFriendlyToSingleFile' instead", Url="http://help/assemblyfiles")]
  void MethodWithAssemblyFilesUsage()
  {
  }

  void TestMethod()
  {
      // IL3002: Using member 'MethodWithAssemblyFilesUsage' which has 'RequiresAssemblyFilesAttribute'
      // can break functionality when embedded in a single-file app. Use 'MethodFriendlyToSingleFile' instead. http://help/assemblyfiles
      MethodWithAssemblyFilesUsage();
  }
  ```

#### `IL3003`: Member 'member' with 'RequiresAssemblyFilesAttribute' has a member 'member' without 'RequiresAssemblyFilesAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.

- For all interfaces and overrides the implementation 'RequiresAssemblyFilesAttribute' must match the definition 'RequiresAssemblyFilesAttribute', either all the members contain the attribute o none of them.

  Here is a list of posible scenarios where the warning can be generated:

  A base member has the attribute but the derived member does not have the attribute
  ```C#
  public class Base
  {
    [RequiresAssemblyFiles]
    public virtual void TestMethod() {}
  }

  public class Derived : Base
  {
    // IL3003: Base member 'Base.TestMethod' with 'RequiresAssemblyFilesAttribute' has a derived member 'Derived.TestMethod()' without 'RequiresAssemblyFilesAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public override void TestMethod() {}
  }
  ```
  A derived member has the attribute but the overriden base member does not have the attribute
  ```C#
  public class Base
  {
    public virtual void TestMethod() {}
  }

  public class Derived : Base
  {
    // IL3003: Member 'Derived.TestMethod()' with 'RequiresAssemblyFilesAttribute' overrides base member 'Base.TestMethod()' without 'RequiresAssemblyFilesAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    [RequiresAssemblyFiles]
    public override void TestMethod() {}
  }
  ```
  An interface member has the attribute but it's implementation does not have the attribute
  ```C#
  interface IRAF
  {
    [RequiresAssemblyFiles]
    void TestMethod();
  }

  class Implementation : IRAF
  {
    // IL3003: Interface member 'IRAF.TestMethod()' with 'RequiresAssemblyFilesAttribute' has an implementation member 'Implementation.TestMethod()' without 'RequiresAssemblyFilesAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public void TestMethod () { }
  }
  ```
  An implementation member has the attribute but the interface that implementes does not have the attribute

  ```C#
  interface IRAF
  {
    void TestMethod();
  }

  class Implementation : IRAF
  {
    [RequiresAssemblyFiles]
    // IL3003: Member 'Implementation.TestMethod()' with 'RequiresAssemblyFilesAttribute' implements interface member 'IRAF.TestMethod()' without 'RequiresAssemblyFilesAttribute'. For all interfaces and overrides the implementation attribute must match the definition attribute.
    public void TestMethod () { }
  }
  ```