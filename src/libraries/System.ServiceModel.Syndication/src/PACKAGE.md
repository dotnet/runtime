## About

<!-- A description of the package and where one can find more documentation -->

Provides types for generating and consuming RSS and Atom feeds.
It is used for creating and parsing syndication feeds, making it easier to build and integrate web content syndication.

## Key Features

<!-- The key features of this package -->

* Easy generation and parsing of RSS and Atom feeds.
* Customizable for different syndication needs.
* Support for both feed reading and writing.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Creating a Feed

```csharp
using System.ServiceModel.Syndication;
using System.Xml;

// Create a new syndication feed
SyndicationFeed feed = new SyndicationFeed(
    "Feed Title",
    "Feed Description",
    new Uri("http://example.com"),
    "FeedID",
    DateTime.Now);

// Add items to the feed
SyndicationItem item1 = new SyndicationItem(
    "Title1",
    "Content1",
    new Uri("http://example.com/item1"));
feed.Items = new List<SyndicationItem> { item1 };

// Serialize the feed to RSS format
using (XmlWriter writer = XmlWriter.Create("rss.xml"))
{
    Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter(feed);
    rssFormatter.WriteTo(writer);
}
```

Resulting RSS feed:

```xml
<?xml version="1.0" encoding="utf-8"?>
<rss xmlns:a10="http://www.w3.org/2005/Atom" version="2.0">
    <channel>
        <title>Feed Title</title>
        <link>http://example.com/</link>
        <description>Feed Description</description>
        <lastBuildDate>Sat, 11 Nov 2023 18:05:21 +0100</lastBuildDate>
        <a10:id>FeedID</a10:id>
        <item>
            <link>http://example.com/item1</link>
            <title>Title1</title>
            <description>Content1</description>
        </item>
    </channel>
</rss>
```

### Consuming a Feed

```csharp
using System.ServiceModel.Syndication;
using System.Xml;

string feedUrl = "https://devblogs.microsoft.com/dotnet/feed/";
using XmlReader reader = XmlReader.Create(feedUrl);

// Read the feed using Atom10FeedFormatter.
SyndicationFeed feed = SyndicationFeed.Load(reader);

Console.WriteLine($"Feed Title: {feed.Title.Text}");
Console.WriteLine("Feed Items:");

// Iterate through the feed items and display the title and a brief summary of each.
foreach (SyndicationItem item in feed.Items)
{
    Console.WriteLine($"Title: {item.Title.Text}");
    Console.WriteLine($"Published Date: {item.PublishDate.DateTime}");
}

/*
 * This code produces the following output:
 *
 * Feed Title: .NET Blog
 * Feed Items:
 * - Title: Join us for the Great .NET 8 Hack
 *   Published Date: 07/11/2023 18:05:00
 * - Title: The convenience of System.IO
 *   Published Date: 06/11/2023 18:05:00
 */
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.ServiceModel.Syndication.SyndicationFeed`
* `System.ServiceModel.Syndication.SyndicationItem`
* `System.ServiceModel.Syndication.Rss20FeedFormatter`
* `System.ServiceModel.Syndication.Atom10FeedFormatter`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/framework/wcf/feature-details/how-to-create-a-basic-rss-feed)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.servicemodel.syndication)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.ServiceModel.Syndication is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
