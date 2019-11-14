Contracts such as NameResolution and Sockets require internal access to Primitive types. Binary copies of these types have been made within the System.Net.Internals namespace using #ifdef pragmas (source code is reused). 

An adaptation layer between .Internals and public types exists within the Extensions classes.

