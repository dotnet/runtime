# [IL Linker](src/linker/README.md)

The IL Linker is a tool one can use to only ship the minimal possible IL code and metadata that a set of 
programs might require to run as opposed to the full libraries.

It is used by the various Xamarin products to extract only the bits of code that are needed to run
an application on Android, iOS and other platforms.

It can also be used in the form of [ILLink.Tasks](src/ILLink.Tasks/README.md) to reduce the size of .NET Core apps.

# [Analyzer](src/analyzer/README.md)

The analyzer is a tool to analyze dependencies which were recorded during linker processing and led linker to mark an item to keep it in the resulting linked assembly.

It can be used to better understand dependencies between different metadata members to help further reduce the linked output.

## How to build the IL Linker

TODO

## Build & Test Status

[![Build Status](https://jenkins.mono-project.com/buildStatus/icon?job=test-linker-mainline)](https://jenkins.mono-project.com/job/test-linker-mainline/)

## Link xml file examples

A link xml file can be used to explicitly preserve assemblies, types, and members.  Below is a sample file containing examples of various usages.

```xml
<linker>
  <!--
  Preserve types and members in an assembly
  -->
  <assembly fullname="Assembly1">
    <!--Preserve an entire type-->
    <type fullname="Assembly1.A" preserve="all"/>
    <!--No "preserve" attribute and no members specified means preserve all members-->
    <type fullname="Assembly1.B"/>
    <!--Preserve all fields on a type-->
    <type fullname="Assembly1.C" preserve="fields"/>
    <!--Preserve all fields on a type-->
    <type fullname="Assembly1.D" preserve="methods"/>
    <!--Preserve the type only-->
    <type fullname="Assembly1.E" preserve="nothing"/>
    <!--Preserving only specific members of a type-->
    <type fullname="Assembly1.F">
      <!--
      Fields
      -->
      <field signature="System.Int32 field1" />
      <!--Preserve a field by name rather than signature-->
      <field name="field2" />
      <!--
      Methods
      -->
      <method signature="System.Void Method1()" />
      <!--Preserve a method with parameters-->
      <method signature="System.Void Method2(System.Int32,System.String)" />
      <!--Preserve a method by name rather than signature-->
      <method name="Method3" />
      <!--
      Properties
      -->
      <!--Preserve a property, it's backing field (if present), getter, and setter methods-->
      <property signature="System.Int32 Property1" />
      <property signature="System.Int32 Property2" accessors="all" />
      <!--Preserve a property, it's backing field (if present), and getter method-->
      <property signature="System.Int32 Property3" accessors="get" />
      <!--Preserve a property, it's backing field (if present), and setter method-->
      <property signature="System.Int32 Property4" accessors="set" />
      <!--Preserve a property by name rather than signature-->
      <property name="Property5" />
      <!--
      Events
      -->
      <!--Preserve an event, it's backing field (if present), add, and remove methods-->
      <event signature="System.EventHandler Event1" />
      <!--Preserve an event by name rather than signature-->
      <event name="Event2" />
    </type>
    <!--Examples with generics-->
    <type fullname="Assembly1.G`1">
      <!--Preserve a field with generics in the signature-->
      <field signature="System.Collections.Generic.List`1&lt;System.Int32&gt; field1" />
      <field signature="System.Collections.Generic.List`1&lt;T&gt; field2" />
      <!--Preserve a method with generics in the signature-->
      <method signature="System.Void Method1(System.Collections.Generic.List`1&lt;System.Int32&gt;)" />
      <!--Preserve an event with generics in the signature-->
      <event signature="System.EventHandler`1&lt;System.EventArgs&gt; Event1" />
    </type>
    <!--Preserve a nested type-->
    <type fullname="Assembly1.H/Nested" preserve="all"/>
    <!--Preserve all fields of a type if the type is used.  If the type is not used it will be removed-->
    <type fullname="Assembly1.I" preserve="fields" required="0"/>
    <!--Preserve all methods of a type if the type is used.  If the type is not used it will be removed-->
    <type fullname="Assembly1.J" preserve="methods" required="0"/>
    <!--Preserve all types in a namespace-->
    <type fullname="Assembly1.SomeNamespace*" />
    <!--Preserve all types with a common prefix in their name-->
    <type fullname="Prefix*" />
  </assembly>
  <!--
  Preserve an entire assembly
  -->
  <assembly fullname="Assembly2" preserve="all"/>
  <!--No "preserve" attribute and no types specified means preserve all-->
  <assembly fullname="Assembly3"/>
  <!--
  Fully qualified assembly name
  -->
  <assembly fullname="Assembly4, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
    <type fullname="Assembly4.Foo" preserve="all"/>
  </assembly>
</linker>
```
