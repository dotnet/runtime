According to SimonHal in a hallway chat on 8/29/03, the MethodImpl
(.override) mechanism is a "body" override, not a "virtual" override.
So calling virtually or non-virtually should result in the same
behavior.  To "virtualize" a call, as far as he seemed concerned, has
everything to do with seeking out the method override in the most
derived class.

So MethodImpls aren't method "virtualization".  They are simply a
way to declare method body overrides.  Now, the ECMA spec in Partition
II says that in a MethodImpl, both the declaration and the
implementation are supposed to be virtual.  According to PII 21.25, an
error should be thrown if either the MethodDeclaration or MethodBody
are not virtual.

As a consequence of all of this, Simon is not surprised if a
non-virtual call on a method in a class that has a .override within
the same class actually invokes the .override method.  That is because
there's only one slot for the method in the method table, and by using
.override, we have determined that the .override method gets that
slot.  So it won't matter whether the method is called virtually or
non-virtually, the result is the same.

Another consequence of this is that if the original declaration that
is being overridden has a body itself and then is .overridden in the
same class, it will be impossible to invoke that original method,
regardless of whether calls are virtual or non-virtual.



Another MethodImpl test is located under
Loader\ClassLoader\Regressions\163172