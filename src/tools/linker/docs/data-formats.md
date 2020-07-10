# Data Formats

IL linker uses several data formats to control or influence the linking process. The data formats are not versioned but are backward compatible.

## Descriptor Format

The `fullname` attribute specifies the fullname of the type in the format specified by ECMA-335. This is in certain cases not the same as the one reported by Type.FullName for example for nested types.

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

The `required` attribute specifies that if the type is not marked, during the mark operation, it will not be linked. Both `required` and `preserve` can be combined together.

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
causing the contained substitutions or descriptors to be applied even when the feature setting is not passed to the linker.
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

This allows to add a custom attribute to a class, interface, delegate, struct or enum 

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
    <!-- The substitution will apply only if "--feature EnableOptionalFeature false" are also used -->
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

### Custom attributes elements

The attribute element requires 'fullname' attribute without it linker will generate a warning and skip
the attribute. Optionally you can use the 'assembly' attribute to point to certain assembly to look
for the attribute, if not specified the linker will look the attribute in any loaded assembly.
Inside an attribute element in the xml you can define argument, field and property elements. 
An attribute could have several arguments, several fields or several properties. When writing 
custom attribute with multiple arguments you need to write the xml elements in an order dependent 
form. That is, the first xml argument element corresponds to the first custom attribute argument, 
second xml argument element correspond to the second custom attribute argument and so on.
For fields and properties, you need to include the name since they are not order dependent.

```xml
<attribute fullname="SomeCustomAttribute" assembly="AssemblyName">
  <argument>Argument1</argument>
  <argument>Argument2</argument>
  <argument>Argument3</argument>
  <field name="fieldName">SomeValue</field>
  <property name="propertyName">SomeValue</property>
</attribute>
```

Additionally the attribute element also supports the usage of the feature switch
```xml
<attribute fullname="SomecustomAttribute" feature="EnableOptionalFeature" featurevalue="false"/>
```

Also if the attribute is used in a type, a special property can be used to specify that the type
is a Custom Attribute an it's instances should be removed by the linker. To do this the word "internal" 
and value "RemoveAttributeInstances" should be included in the attribute as described in the following
example:

```xml
<linker>
  <assembly fullname="*"> 
    <type fullname="System.Runtime.CompilerServices.NullableAttribute">
      <attribute internal="RemoveAttributeInstances" feature="EnableOptionalFeature" featurevalue="false" />
    </type>
  </assembly>
</linker>
```
Notice that a descriptor file containing the custom attribute type overrides this behavior. In case the
custom attribute type is being referenced in a descriptor xml file and in the linkattributes xml file
for removal, the custom attribute will not be removed