Book of the Runtime (BotR) FAQ
===

# What is the BotR?

The [Book of the Runtime](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/README.md) is a set of documents that describe components in the CLR and BCL. They are intended to focus more on architecture and invariants and not an annotated description of the codebase.

It was originally created within Microsoft in ~ 2007, including this document. Developers were responsible to document their feature areas. This helped new devs joining the team and also helped share the product architecture across the team.

We realized that the BotR is even more valuable now, with CoreCLR being open source on GitHub. We are publishing BotR chapters to help a new set of CLR developers.

Each of the BoTR documents were written with a [certain perspective](https://github.com/dotnet/coreclr/pull/115), both in terms of the timeframe and the author. We did not think it was right to mutate the documents to make them more "2015". They remain the docs that they were, modulo a few spelling corrections and a conversion to markdown. That said, we'll accept PRs to the docs to improve them.

# Who is the main audience of BotR?

- Developers who are working on bugs that impinge on an area and need a high level overview of the component.
- Developers working on new features with dependencies on a component need to know enough about it to ensure the new feature will interact correctly with existing components.
- New developers need this chapter to maintain a given component.

# What should be in a BotR chapter?

The purpose of Book of the Runtime chapters is to capture information that we cannot easily reconstruct from the functional specification and source code alone, and to enable communication at a high level between team members. It explains concepts and presents a top-down description, and most importantly, explains why we made the design decisions we made.

# How is this different from a design doc?

A design doc is what you write before you start implementation. A BotR chapter is usually written after a feature is implemented, at which point you have already decided the pros and cons of various design options and settled on one (and perhaps have plans to use an improved design in the future), and have a much better idea about all the details (some of which could be very hard to think of without actually going through the implementation/testing). So you can talk about rationales behind design decisions a lot better.

# I am a new dev and not familiar with any features yet, how can I contribute?

A new dev can be a great contributor to BotR as one of the most important purposes of BotR is to help new devs with getting up to speed. Here are some ways you can contribute:

- Be a reviewer! If you think some things are not clear or could be explained better, do not hesitate to contact the author of the chapter and chat with them to see how you can make it more understandable.
- As you are getting up to speed in your area, look over the BotR chapters for your area and see if there are any errors or anything that requires an update and make the modifications yourself.
- Volunteer to write a chapter or part of a chapter. This might seem like a daunting task but you can start by just accumulating knowledge - take notes as you learn stuff about your area and gradually mold it into a BotR chapter.

# What are the responsibilities of a BotR reviewer?

As a reviewer you will be expected to give constructive comments on the chapter you are reviewing. You can comment on any aspect, eg. the technical depth, writing style, content coverage. Keep in mind that BotR is mostly about design and architectural issues that may not be obvious. It is not meant to walk you through implementation details. Please focus on that.

# I _really_ don't have time to work on a BotR chapter – it seems like I always have other things to do. What do I do?

Here are some ways I think would be useful when working on BotR.

- Spread the work out; don't make it a workitem as in "I will need to spend the next Monday through Thursday to work on my chapter"; think of it more like something you do when you want to take a break from coding or bug fixing, or just a change of scenery. I find it much easier to spend a little time here and there working on a chapter than having to specifically allocate a contiguous number of days which always seem hard to come by.
- Have someone else write the chapter or most of the chapter for you. I am not joking. This is actually a very good way to help new devs ramp up. If you will be mentoring a new dev in your area, spend time with them to explain the feature area and encourage them to write a BotR chapter if one doesn't already exist. Of course be a reviewer of it.
- Use other documentation that is already there. There are MSDN docs and blog posts on .NET features. This can certainly be a base for your BotR chapter as well.
