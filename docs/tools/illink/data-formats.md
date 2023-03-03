# Data Formats

## Input Data Formats

ILLink uses several data formats to control or influence the trimming process. The data formats are not versioned but are backward compatible.

- [Descriptors](#descriptor-format)
- [Substitutions](#substitution-format)
- [Custom Attributes Annotations](#custom-attributes-annotations-format)

## Output Data Formats

- [Dependencies Trace](#dependencies-trace-format)

# Format Details

## Descriptor Format

Descriptors are used to direct the trimmer to always keep some items in the assembly, regardless of if the trimmer can find any references to them.

Descriptor XML can be embedded in an assembly. In that case it must be stored as an embedded resource with logical name `ILLink.Descriptors.xml`. To achieve this when building an assembly use this in the project file to include the XML:

```xml
  <ItemGroup>
    <EmbeddedResource Include="ILLink.Descriptors.xml">
      <LogicalName>ILLink.Descriptors.xml</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

Embedded descriptors only take effect if the containing assembly is included in the trimmer output, so if something from that assembly is marked to be kept.

Descriptor XML can also be passed to the trimmer on the command via the [`-x` parameter](illink-options.md#trimming-from-an-xml-descriptor).

### XML Examples

### Preserve entire assembly

```xml
<linker>
  <assembly fullname="AssemblyA" preserve="all" />

  <!-- No "preserve" attribute and no types specified means preserve all -->
  <assembly fullname="AssemblyB"/> 
</linker>
```

### Preserve assembly using fully qualified name

```xml
<linker>
  <assembly fullname="Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
    <type fullname="Assembly.Foo" preserve="all" />
  </assembly>
</linker>
```

### Preserve a type

The `required` attribute specifies that if the type is not marked, during the mark operation, it will not be trimmed. Both `required` and `preserve` can be combined together.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A" preserve="all" />
    
    <!-- No "preserve" attribute and no members specified means preserve all members -->
    <type fullname="Assembly.B" /> 
    
    <!-- Preserve the type declaration only -->
    <type fullname="Assembly.C" preserve="nothing" /> 

    <!-- Preserve a nested type -->
    <type fullname="Assembly.D/Nested" preserve="all" />

    <!-- Preserve all types with the prefix in their name -->
    <type fullname="Assembly.Prefix*" />

    <!-- Preserve the type if the type is used. If the type is not used it will be removed -->
    <type fullname="Assembly.E" required="false" />

    <!-- Type with generics in the signature -->
    <type fullname="Assembly.G`1" />
  </assembly>
</linker>
```

### Preserve all methods or all fields on a type

```xml
<linker>
  <assembly fullname="Assembly">
    
    <!-- Preserve all fields on a type -->
    <type fullname="Assembly.A" preserve="fields" />
    
    <!-- Preserve all methods on a type -->
    <type fullname="Assembly.B" preserve="methods" /> 
  </assembly>
</linker>
```

### Preserve more than one type within an assembly

```xml
<linker>
  <assembly fullname="Assembly">
    
    <!-- Preserves all types who's fully qualified type name matches the regular expression -->
    <type fullname="Assembly.Namespace*" />
    
    <!-- Preserve all types within the specified namespace -->
    <namespace fullname="Assembly.Namespace" /> 
  </assembly>
</linker>
```

### Preserve only selected fields on a type

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <field signature="System.Int32 field1" />
      
      <!-- Field by name rather than signature -->
      <field name="field2" />

      <!-- Field with generics in the signature -->
      <field signature="System.Collections.Generic.List`1&lt;System.Int32&gt; field3" />
      <field signature="System.Collections.Generic.List`1&lt;T&gt; field4" />
    </type>
  </assembly>
</linker>
```

### Preserve only selected methods on a type

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.Void Method1()" />
      <method signature="System.Void Method2(System.Int32,System.String)" />

      <!-- Method with generics in the signature -->
      <method signature="System.Void Method1(System.Collections.Generic.List`1&lt;System.Int32&gt;)" />

      <!-- Preserve a method by name rather than signature -->
      <method name="Method3" />

       <!-- Preserve the method if the type is used. If the type is not used it will be removed -->
      <method signature="System.Void Method4()" required="false" />
    </type>
  </assembly>
</linker>
```

### Preserve only selected properties on type

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">      
      <!-- Preserve the property, its backing field (if present), getter, and setter methods -->    
      <property signature="System.Int32 Property1" />

      <property signature="System.Int32 Property2" accessors="all" />
     
      <!-- Preserve the property, its backing field (if present), and getter method -->
      <property signature="System.Int32 Property3" accessors="get" />
      
      <!--Preserve a property, it's backing field (if present), and setter method -->
      <property signature="System.Int32 Property4" accessors="set" /> 

      <!-- Preserve a property by name rather than signature -->
      <property name="Property5" />

       <!-- Preserve the property if the type is used. If the type is not used it will be removed -->
      <property signature="System.Int32 Property6" required="false" />
    </type>
  </assembly>
</linker>
```

### Preserve only selected events on a type

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <!-- Preserve the event, it's backing field (if present), add, and remove methods -->
      <event signature="System.EventHandler Event1" />

      <!-- Preserve an event by name rather than signature -->
      <event name="Event2" />

      <!-- Preserve an event with generics in the signature-->
      <event signature="System.EventHandler`1&lt;System.EventArgs&gt; Event3" />

       <!-- Preserve the event if the type is used. If the type is not used it will be removed -->
      <event signature="System.EventHandler Event2" required="false" />
    </type>
  </assembly>
</linker>
```

## Substitution Format

Substitutions direct the trimmer to replace specific method's body with either a throw or return constant statements.

Substitutions have effect only on assemblies which are trimmed with assembly action `link`, any other assembly will not be affected. That said it is possible to have a `copy` assembly with the substitution on a method in it, and then a separate `link` assembly which calls such method. The `link` assembly will see the constant value of the method after the substitution and potentially remove unused branches and such.

Substitutions XML can be embedded in an assembly by including it as an embedded resource with logical name `ILLink.Substitutions.xml`. To include an XML file in an assembly this way, use this in the project file:

```xml
  <ItemGroup>
    <EmbeddedResource Include="ILLink.Substitutions.xml">
      <LogicalName>ILLink.Substitutions.xml</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

Embedded substitutions only take effect if the containing assembly is included in the trimmer output. Embedded substitutions should only address methods from the containing assembly.

Substitutions XML can be specified on the command line via the [`--substitutions` parameter](illink-options.md#using-custom-substitutions). Using substitutions with `ipconstprop` optimization (enabled by default) can help reduce output size as any dependencies under conditional logic which will be evaluated as unreachable will be removed.

### Substitute method body with a constant

The `value` attribute is optional and only required when the method should be hardcoded to return a non-default value and the return type is not `void`.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.String TestMethod()" body="stub" value="abcd" />
    </type>
  </assembly>
</linker>
```

### Remove method

Entire method body is replaces with `throw` instruction when method is referenced.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.String TestMethod()" body="remove" />
    </type>
  </assembly>
</linker>
```

### Override static field value with a constant

The `initialize` attribute is optional and when not specified the code to set the static field to the value will not be generated.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <field name="MyNumericField" value="5" initialize="true" />
    </type>
  </assembly>
</linker>
```

### Remove embedded resources

```xml
<linker>
  <assembly fullname="Assembly">
    <resource name="Resource" action="remove" />
  </assembly>
</linker>
```

### Conditional substitutions and descriptors

The `feature` and `featurevalue` attributes are optional, but must be used together when used.
They can be applied to any element to specify conditions under which the contained substitutions or descriptors
are applied, based on feature settings passed via `--feature FeatureName bool`

```xml
<linker>
  <assembly fullname="Assembly">
    <!-- This substitution will apply only if "EnableOptionalFeature" is set to "false" -->
    <type fullname="Assembly.A" feature="EnableOptionalFeature" featurevalue="false">
      <method signature="System.String TestMethod()" body="stub" />
    </type>
  </assembly>
</linker>
```

`featuredefault="true"` can be used to indicate that this `featurevalue` is the default value for `feature`,
causing the contained substitutions or descriptors to be applied even when the feature setting is not passed to the trimmer.
Note that this will only have an effect where it is applied - the default value is not remembered or reused for other elements.

```xml
<linker>
  <assembly fullname="Assembly">
    <!-- This method will be preserved if "EnableDefaultFeature" is "true" or unspecified -->
    <type fullname="Assembly.A" feature="EnableDefaultFeature" featurevalue="true" featuredefault="true">
      <method signature="System.String TestMethod()" />
    </type>
    <!-- This method will only be preserved if "EnableDefaultFeature" is "true", not if it is unspecified-->
    <type fullname="Assembly.A" feature="EnableDefaultFeature" featurevalue="true">
      <method signature="System.String TestMethod2()" />
    </type>
  </assembly>
</linker>
```

## Custom Attributes Annotations Format

Attribute annotations direct the trimmer to behave as if the specified item has the specified attribute.

Attribute annotations can only be used to add attributes which have effect on trimmer behavior, all other attributes will be ignored. Attributes added via attribute annotations only influence trimmer behavior, they are never added to the output assembly.

Attribute annotation XML can be embedded in an assembly by including it as an embedded resource with logical name `ILLink.LinkAttributes.xml`. To include an XML file in an assembly this way, use this in the project file:

```xml
  <ItemGroup>
    <EmbeddedResource Include="ILLink.LinkAttributes.xml">
      <LogicalName>ILLink.LinkAttributes.xml</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

Embedded attribute annotations should only address methods from the containing assembly. Whereas attribute annotations specified on the command line via the [`--link-attributes` parameter](illink-options.md#supplementary-custom-attributes) can alter types and members in any assembly.

The attribute element requires 'fullname' attribute without it trimmer will generate a warning and skip the attribute. Optionally you can use the 'assembly' attribute to point to certain assembly to look
for the attribute, if not specified the trimmer will look up the attribute in any loaded assembly.

Inside an attribute element in the xml you can further define argument, field and property elements used as an input for the attribute. An attribute can have several arguments, several fields or several properties. When writing custom attribute with multiple arguments you need to write the xml elements in an order-dependent form. That is, the first xml argument element corresponds to the first custom attribute argument, second xml argument element correspond to the second custom attribute argument and so on. When argument type is not specified it's considered to be of `string` type. Any other custom attribute value has to have its type specified for trimmer to find the correct constructor overload.

```xml
<attribute fullname="SomeCustomAttribute" assembly="AssemblyName">
  <argument>StringValue</argument>
  <argument type="System.Int32">-45</argument>
  <argument type="System.DayOfWeek">Sunday</argument>
  <field name="fieldName">StringValue</field>
  <property name="propertyName" type="System.Byte">200</property>
</attribute>
```

### Custom attribute on assembly

```xml
<linker>
  <assembly fullname="Assembly">
    <attribute fullname="CustomAttributeName" assembly="AssemblyName">
      <argument>Argument</argument>
    </attribute>
  </assembly>
</linker>
```

### Custom attribute on type

This allows to add a custom attribute to a class, interface, delegate, struct or enum.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <attribute fullname="CustomAttributeName" assembly="AssemblyName">
        <argument>Argument</argument>
      </attribute>
    </type>
  </assembly>
</linker>
```

### Custom attribute on type field

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <field name="MyTypeField">
        <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
          <argument>DefaultConstructor</argument>
        </attribute>
      </field>
    </type>
  </assembly>
</linker>
```

### Custom attribute on property

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <property name="MyTypeProperty">
        <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
          <argument>DefaultConstructor</argument>
        </attribute>
      </property>
    </type>
  </assembly>
</linker>
```

### Custom attribute on event

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <event name="MyTypeEvent">
        <attribute fullname="CustomAttribute" assembly="AssemblyName">
          <argument>DefaultConstructor</argument>
        </attribute>
      </event>
    </type>
  </assembly>
</linker>
```

### Custom attribute on method parameter

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.Void Method1(System.Type)">
        <parameter name="typeParameter">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
      </method>
      <method signature="System.Type Method2()">
        <return>
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>PublicConstructors</argument>
          </attribute>
        </return>
      </method>
      <method signature="Method3&lt;T&gt;(T)">
        <parameter name="genericParameter">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
      </method>
    </type>
  </assembly>
</linker>
```

### Custom attribute in multiple method parameters

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.Void Method1(System.Type, System.Type, System.Type)">
        <parameter name="typeParameter1">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
        <parameter name="typeParameter2">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
        <parameter name="typeParameter3">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>PublicConstructors</argument>
          </attribute>
        </parameter>
      </method>
    </type>
  </assembly>
</linker>
```

### Custom attribute on nested type

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <type name="NestedType">
        <property name="MyTypeField">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>DefaultConstructor</argument>
          </attribute>
        </property>
      </type>
    </type>
  </assembly>
</linker>
```

### Custom attribute on type in all assemblies

```xml
<linker>
  <assembly fullname="*">
    <type fullname="Namespace.SpecialAttribute">
      <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
        <argument>DefaultConstructor</argument>
      </attribute>
    </type>
  </assembly>
</linker>
```

### Conditional custom attributes

The `feature` and `featurevalue` attributes are optional, but must be used together when used.
They can be applied to any element to specify conditions under which the contained custom
attributes are applied.

```xml
<linker>
  <assembly fullname="Assembly">
    <!-- The attribute will apply only if "EnableOptionalFeature" is set to "false" -->
    <type fullname="Assembly.A" feature="EnableOptionalFeature" featurevalue="false">
      <method signature="System.String TestMethod()">
        <return>
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" assembly="System.Runtime">
            <argument>PublicConstructors</argument>
          </attribute>
        </return>
      </method>
    </type>
  </assembly>
</linker>
```

### Custom attributes with feature

```xml
<attribute fullname="SomecustomAttribute" feature="EnableOptionalFeature" featurevalue="false"/>
```

### Removing custom attributes

Any custom attribute can be annotated with a special custom attribute which can be used to specify
that all instances of the attribute can be removed by the trimmer. To do this use `internal="RemoveAttributeInstances"`
instead of specifying `fullname` in the attribute as described in the following example:

```xml
<linker>
  <assembly fullname="*">
    <type fullname="System.Runtime.CompilerServices.NullableAttribute">
      <attribute internal="RemoveAttributeInstances" feature="EnableOptionalFeature" featurevalue="false" />
    </type>
  </assembly>
</linker>
```

In some cases, it's useful to remove only specific usage of the attribute. This can be achieved by specifying the value
or values of the arguments to match. In the example below only `System.Reflection.AssemblyMetadataAttribute` custom attributes
with the first argument equal to `RemovableValue` will be removed.

```xml
<linker>
  <assembly fullname="*">
    <type fullname="System.Reflection.AssemblyMetadataAttribute">
      <attribute internal="RemoveAttributeInstances">
        <argument type="System.Object">
          <argument>RemovableValue</argument>
        </argument>
      </attribute>
    </type>
  </assembly>
</linker>
```

Notice that a descriptor file containing the custom attribute type overrides this behavior. In case the
custom attribute type is being referenced in a descriptor file and in the attribute annotations file
for removal, the custom attribute will not be removed.

## Dependencies Trace Format

This is the format of data used to capture trimmer logic about why
members, types, and other metadata elements were marked by the trimmer
as required and persisted in the trimmed output. The format includes edges
of the graph for every dependency which was tracked by the trimmer.
