.NET Standards
==============

There was a very early realization by the founders of .NET that they were creating a new programming technology that had broad applicability across operating systems and CPU types and that advanced the state of the art of late 1990s (when the .NET project started at Microsoft) programming language implementation techniques. This led to considering and then pursuing standardization as an important pillar of establishing .NET in the industry.

The key addition to the state of the art was support for multiple programming languages with a single language runtime, hence the name _Common Language Runtime_. There were many other smaller additions, such as value types, a simple exception model and attributes. Generics and language integrated query were later added to that list.

Looking back, standardization was quite effective, leading to .NET having a strong presence on iOS and Android, with the Unity and Xamarin offerings, both of which use the Mono runtime. The same may end up being true for .NET on Linux.

The various .NET standards have been made meaningful by the collaboration of multiple companies and industry experts that have served on the working groups that have defined the standards. In addition (and most importantly), the .NET standards have been implemented by multiple commercial (ex: Unity IL2CPP, .NET Native) and open source (ex: Mono) implementors. The presence of multiple implementations proves the point of standardization.

ECMA 334 - C#
=============

The C# language was standardized as [ECMA 334](https://www.ecma-international.org/publications-and-standards/standards/ecma-334) in 2002 and approved as [ISO/IEC 23270](http://www.iso.org/iso/home/store/catalogue_ics/catalogue_detail_ics.htm?csnumber=42926) in 2003.

**ECMA 334 Resources**

- [ECMA 334 Standard Overview](https://www.ecma-international.org/publications-and-standards/standards/ecma-334)
- [ECMA 334 Standard (PDF)](https://www.ecma-international.org/wp-content/uploads/ECMA-334_6th_edition_june_2022.pdf)

ECMA 335 - CLI
==============

[Common Language Infrastructure](http://en.wikipedia.org/wiki/Common_Language_Infrastructure) - the formalized basis of .NET -- was standardized as [ECMA 335](https://www.ecma-international.org/publications-and-standards/standards/ecma-335) in 2001 and approved as [ISO/IEC 23271](http://www.iso.org/iso/home/store/catalogue_ics/catalogue_detail_ics.htm?csnumber=58046) in 2003. The  standards have been since updated, to reflect changes in .NET, such as generics.

**ECMA 335 Resources**

- [ECMA 335 Standard Overview](https://www.ecma-international.org/publications-and-standards/standards/ecma-335)
- [ECMA 335 Standard (PDF)](https://www.ecma-international.org/wp-content/uploads/ECMA-335_6th_edition_june_2012.pdf)
- [Wikipedia entry on CLI](http://en.wikipedia.org/wiki/Common_Language_Infrastructure)
- [ECMA 335 Addendum](../design/specs/Ecma-335-Augments.md)

**ECMA 335 Partitions with added Microsoft Specific Implementation Notes**

- [Partition I: Concepts and Architecture](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20I.pdf)
- [Partition II: Meta Data Definition and Semantics](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20II.pdf)
- [Partition III: CIL Instruction Set](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20III.pdf)
- [Partition IV: Profiles and Libraries](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20IV.pdf)
- [Partition V: Debug Interchange Format](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20V.pdf)
- [Partition VI: Annexes](http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20VI.pdf)

**ECMA Technical Report 084: Information Derived from Partition IV XML File**

- [ECMA TR/84 Report (PDF)](https://www.ecma-international.org/wp-content/uploads/ECMA_TR-84_6th_edition_june_2012.pdf)
- [ECMA TR/84 Tools and Source Code](https://www.ecma-international.org/wp-content/uploads/ecma-tr-84-6th_edition_files.zip)

ECMA 372 - C++/CLI
==================

The C++/CLI language was standardized as [ECMA 372](http://www.ecma-international.org/publications/standards/Ecma-372.htm) in 2005.

ECMA 372 is supported by the .NET Framework, but not .NET Core.

**ECMA 372 Resources**

- [ECMA 372 Standard Overview](https://www.ecma-international.org/publications-and-standards/standards/ecma-372)
- [ECMA 372 Standard (PDF)](https://www.ecma-international.org/wp-content/uploads/ECMA-372_1st_edition_december_2005.pdf)

Shared Source CLI (SSCLI)
=========================

[Shared Source CLI](http://en.wikipedia.org/wiki/Shared_Source_Common_Language_Infrastructure) or "Rotor" was a working implementation for the ECMA-334 (C#) and ECMA-335 (Common Language Infrastructure, or CLI) standards. It was released under a shared source license in 2002, primarily to encourage academic research focused on .NET and to demonstrate viability of .NET on diverse platforms. It was last released in 2006, to align with the .NET Framework 2 release. It is no longer relevant, given that [CoreCLR](https://github.com/dotnet/coreclr) has been released as open source on GitHub.

**SSCLI Resources**

- [Wikipedia entry on SSCLI](http://en.wikipedia.org/wiki/Shared_Source_Common_Language_Infrastructure)
- [The Microsoft Shared Source CLI Implementation](https://msdn.microsoft.com/library/ms973879.aspx)
- [Shared Source Common Language Infrastructure 2.0 Release ](http://www.microsoft.com/en-us/download/details.aspx?id=4917)
- [Shared Source CLI 2.0 Infrastructure 2.0 Release - 3rd party provided on GitHub](https://github.com/gbarnett/shared-source-cli-2.0)
