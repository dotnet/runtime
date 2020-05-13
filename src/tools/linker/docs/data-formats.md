# Data Formats

ILLinker uses several data formats to control or influnce the linking process. The data formats are not versioned but are backward compatible.

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

### Override field value with a constant

The `initialize` attribute is optional and when not specified the code to set the field to the value will not be generated.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <field name="MyNumericField" value="5" initialize="true" />
    </type>
  </assembly>
</linker>
```

### Conditional substitutions

The `feature` and `featurevalue` attributes are optional, but must be used together when used.
They can be applied to any element to specify conditions under which the contained substitutions
are applied.

```xml
<linker>
  <assembly fullname="Assembly">
    <!-- The substitution will apply only if "--feature EnableOptionalFeature false" are also used -->
    <type fullname="Assembly.A" feature="EnableOptionalFeature" featurevalue="false">
      <method signature="System.String TestMethod()" body="stub">
      </method>
    </type>
  </assembly>
</linker>
```

## Custom Attributes Annotations Format

### Custom attribute on type field

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <field name="MyTypeField">
        <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
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
        <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
          <argument>DefaultConstructor</argument>
        </attribute>
      </property>
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
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
      </method>
      <method signature="System.Type Method2()">
        <return>
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>PublicConstructors</argument>
          </attribute>
        </return>
      </method>
      <method signature="Method3&lt;T&gt;(T)">
        <parameter name="genericParameter">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
      </method>
    </type>
  </assembly>
</linker>
```

### Custom attribute in multiple method parameters

When writing custom attribute for multiple method parameters you need to write the xml elements
in an order dependent form. That is, the first xml parameter element corresponds to the first 
method parameter, second xml parameter element correspond to the second method parameter and so on.

```xml
<linker>
  <assembly fullname="Assembly">
    <type fullname="Assembly.A">
      <method signature="System.Void Method1(System.Type, System.Type, System.Type)">
        <parameter name="typeParameter1">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
        <parameter name="typeParameter2">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>DefaultConstructor</argument>
          </attribute>
        </parameter>
        <parameter name="typeParameter3">
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
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
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>DefaultConstructor</argument>
          </attribute>
        </property>
      </type>
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
          <attribute fullname="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers">
            <argument>PublicConstructors</argument>
          </attribute>
        </return>
      </method>
    </type>
  </assembly>
</linker>
```