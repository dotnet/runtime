*This blog post originally appeared on David Broman's blog on 12/29/2011*


The docs for [GetObjectSize](http://msdn.microsoft.com/en-US/library/ms231885(v=VS.100).aspx) have recently been updated with this info, but I wanted to mention it here, too, to ensure you were aware of this information.

Some profilers manually advance through objects on the heap, inspecting their field values, by starting at an ObjectID and moving forward by its size to the next ObjectID, repeating this process, for all the reported generation ranges (via GetGenerationBounds or MovedReferences/SurvivingReferences).  If your profiler doesn’t do this, then this blog entry will be of no interest to you, and you can skip it.  But if your profiler does do this, you need to be aware of the alignment rules that the CLR employs as it allocates and moves objects around on the GC heap.

- **On x86** : All objects are 4-byte aligned, except for objects on the large-object-heap, which are always 8-byte aligned.
- **On x64** : All objects are always 8-byte aligned, in all generations.

And the important point to note is that GetObjectSize does NOT include alignment padding in the size that it reports.  Thus, as your profiler manually skips from object to object by using GetObjectSize() to determine how far to skip, your profiler must manually add in any alignment padding necessary to achieve the alignment rules listed above.

