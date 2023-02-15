# Interface Implementation Methods Marking
#### (Does this method need to be kept due to the interface method it overrides)

The following behavior is expected for interface methods. This logic could be used to begin marking and sweeping the `.Override` of a method since if the method isn't a dependency due to the interface/base type, we should be able to remove the methodImpl. Right now, the methodImpl is always kept if both the interface method and overriding method is kept, but that isn't always necessary.

Whether or not a method implementing an interface method is required due to the _interface_ is affected by the following cases / possibilities (the method could still be kept for other reasons):
- Base method is abstract or has a default implementation (`abstract` vs `virtual` in C#)
- Method is Instance or Static
- Implementing type is relevant to variant casting or not
  - Relevant to variant casting means the type token appears, the type is passed as a type argument or array type, or is reflected over.
- Base method is marked as used or not
- Base method is from preserved scope or not
- Implementing type is marked as instantiated or not
- Interface Implementation is marked or not

Note that in library mode, interface methods that can be accessed by COM or native code are marked by the linker.

### If the interface implementation is not marked, do not mark the implementation method
A type that doesn't implement the interface isn't required to have methods that implement the interface. However, a base type may have a public method that implements an interface on a derived type. If the interface implementation on the derived type is marked, then the method may be needed and we should go onto the next step.

Cases left (bold means we know it is only one of the possible options now):
- Base method is abstract or has a default implementation
- Method is Instance or Static
- Implementing type is relevant to variant casting or not
- Base method is marked as used or not
- Base method from preserved scope or not
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface method is not marked and the interface doesn't come from a preserved scope, do not mark the implementation method
Unmarked interface methods from `link` assemblies will be removed so the implementing method does not need to be kept.

Cases left:
- Base method is abstract or has a default implementation
- Method is Instance or Static
- Implementing type is relevant to variant casting or not
- ~~Base method is marked as used or not~~
- ~~Base method from preserved scope or not~~
- _Base method is either marked as used or from preserved scope (combine above)_
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the interface method is abstract, mark the implementation method
The method is needed for valid IL.

Cases left:
- __Base method has a default implementation__
- Method is Instance or Static
- Implementing type is relevant to variant casting or not
- Base method is marked as used or from preserved scope
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

### If the method is an instance method then mark the implementation method if the type is instantiated (or instantiable in library mode) and do not mark the implementation otherwise.
An application can call the instance interface method if and only if the type is instantiated.

Cases left:
- __Base method has a default implementation__
- __Method is Static__
- Implementing type is relevant to variant casting or not
- Base method is marked as used or from preserved scope
- Implementing type is marked as instantiated or not
- __Interface Implementation is marked__

The use of static methods is not related to whether or not a type is instantiated or not.

Cases left:
- __Base method has a default implementation__
- __Method is Static__
- Implementing type is relevant to variant casting or not
- Base method is marked as used or from preserved scope
- __Interface Implementation is marked__

### If the implementing type is relevant to variant casting, mark the implementation method.
A static method may only be called through a constrained call if the type is relevant to variant casting.

Cases left:
- __Base method has a default implementation__
- __Method is Static__
- __Implementing type is not relevant to variant casting__
- Base method is marked as used or from preserved scope
- __Interface Implementation is marked__

### If the interface method is in a preserved scope, mark the implementation method.
We assume the implementing type could be relevant to variant casting in the preserved scope assembly and could be called, so we will keep the method.

### Otherwise, do not mark the implementing method


Summary:

if __Interface Implementation is not marked__ then do not mark the implementation method.

else if __Base method is marked as not used__ AND __Interface is not from preserved scope__ do not mark the implementation method

else if __Base method does not have a default implementation__ then mark the implementation method

else if __Implementation method is an instance method__ AND __Implementing type is instantiated__ then mark the implementation method

else if __Implementation method is an instance method__ AND __Implementing type is not instantiated__ then do not mark the implementation method

else if __Method is Static__ AND __Implementing type is relevant to variant casting__ then mark the implementation method

else if __Method is Static__ AND __Interface method is from a preserved scope__ then mark the implementation method

else do not mark the implementation method
