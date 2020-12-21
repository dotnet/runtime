*This blog post originally appeared on David Broman's blog on 10/13/2005*


If your profiler plays with metadata, you've undoubtedly come across signature blobs. They’re used to encode type information for method definitions & references, local variables, and a whole lot more. They’re wonderfully compact, recursively versatile, and sometimes, well, challenging to parse. Fortunately, [Rico Mariani](https://docs.microsoft.com/en-us/archive/blogs/ricom/) was feeling generous one day, and churned out a simple parser that can read these types of signatures:

- MethodDefSig
- MethodRefSig
- StandAloneMethodSig
- FieldSig
- PropertySig
- LocalVarSig

Here are the files:

- [sigparse.cpp](samples/sigparse.cpp) (Rico's signature parser)
- [sigformat.cpp](samples/sigformat.cpp) (An example extension to the parser)
- [PlugInToYourProfiler.cpp](samples/PlugInToYourProfiler.cpp) (Example code to plug the extension into your profiler)

Open up **sigparse.cpp** in your favorite editor and take a look at the grammar at the top. The grammar comes from the ECMA CLI spec. Jonathan Keljo has a [link](http://blogs.msdn.com/jkeljo/archive/2005/08/04/447726.aspx) to it from his blog. This tells you the types of signature blobs the parser can handle.

Sigparse.cpp is structured without any dependencies on any headers, so you can easily absorb it into your profiler project. There are two things you will need to do to make use of the code. I provided examples of each of these in the download above to help you out:

1. You will **extend the code** to make use of the parsed components of the signature however you like. Perhaps you’ll build up your own internal structures based on what you find. Or maybe you’ll build a pretty-printer that displays method prototypes in the managed language of your choice.
2. You will then **call the code** to perform the parse on signature blobs you encounter while profiling.

## Extending the code

Simply derive a new class from SigParser, and override the virtual functions. The functions you override are events to be handled as the parser traverses the signature in top-down fashion. For example, when the parser encounters a MethodDef, you might see calls to your overrides of:

```
NotifyBeginMethod()
    NotifyParamCount()
    NotifyBeginRetType()
        NotifyBeginType()
            NotifyTypeSimple()
        NotifyEndType()
    NotifyEndRetType()
    NotifyBeginParam()
        NotifyBeginType()
            NotifyTypeSimple()
        NotifyEndType()
    NotifyEndParam()
    _… (more parameter notifications occur here if more parameters exist)_
NotifyEndMethod()
```

And yes, generics are handled as well.

In your overrides, it’s up to you to do what you please. **SigFormat.cpp** provides an example of a very simple pretty-printer that just prints to stdout.

You’ll notice that metadata tokens (TypeDefs, TypeRefs, etc.) are not resolved for you. This is because the parser has no knowledge of the assemblies in use—it only knows about the signature blob you give it. When the parser comes across a token it just reports it to you directly via the overrides (e.g., NotifyTypeDefOrRef()). It’s up to your profiler to figure out what to do with the tokens once it finds them.

## Calling the code

I saved the easy step for last. When your profiler encounters a signature blob to parse, just create an instance of your SigParser-derived class, and call Parse(). Could it be simpler? An example of this is in **PlugInToYourProfiler.cpp**. Here you’ll find example code that you’d add to a profiler to read metadata and feed the signature blobs to SigFormat to print all signatures found.

Go ahead! Plug this all into your profiler and watch it tear open the signature blobs in mscorlib, and pretty-print the results. Dude, can this get any more exciting?!

## Homework?!

Don't worry, it's optional. I mentioned above that only signatures whose grammar appears in the comments in sigparse.cpp are parseable by this sample. For example, it can’t parse TypeSpecs and MethodSpecs. However, adding this capability is pretty straightforward given the existing code, and so this is left as an exercise to the reader. :-)

The only gotcha is that TypeSpecs & MethodSpecs don’t have a unique byte that introduces them. For example, GENERICINST could indicate the beginning of a TypeSpec or a MethodSpec. You’ll see that SigParser::Parse() switches on the intro byte to determine what it’s looking at. So to keep things simple, you’ll want to add a couple more top-level functions to SigParser to parse TypeSpecs & MethodSpecs (say, ParseTypeSpec() & ParseMethodSpec()). You’d then call those functions instead of Parse() when you have a TypeSpec or MethodSpec on your hands. Of course, if you don’t care about TypeSpecs and MethodSpecs, you can use the code as is and not worry. But this stuff is so much fun, you’ll probably want to add the capability anyway.

Hope you find this useful. And thanks again to Rico Mariani for sigparse.cpp!
