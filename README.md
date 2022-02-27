# HtmlKit

[![Build Status](https://github.com/jstedfast/HtmlKit/actions/workflows/main.yml/badge.svg?event=push)](https://github.com/jstedfast/HtmlKit/actions/workflows/main.yml)[![Coverity Scan Build Status](https://scan.coverity.com/projects/5621/badge.svg)](https://scan.coverity.com/projects/5621)[![Coverage Status](https://coveralls.io/repos/jstedfast/HtmlKit/badge.svg?branch=HEAD)](https://coveralls.io/r/jstedfast/HtmlKit?branch=HEAD)

## What is HtmlKit?

HtmlKit is a cross-platform .NET framework for parsing HTML.

HtmlKit implements the HTML5 tokenizing state machine described in
[W3C's HTML5 Tokenization Specification](https://dev.w3.org/html5/spec-LC/tokenization.html).

## Goals

I haven't fully figured that out yet.

So far the goal is tokenizing HTML with the intention of using it for
[MimeKit](https://github.com/jstedfast/MimeKit)'s
[HtmlToHtml](http://www.mimekit.net/docs/html/T_MimeKit_Text_HtmlToHtml.htm)
text converter, replacing the quick & dirty HTML tokenizer I originally wrote.

Maybe someday I'll implement a DOM. Who knows.

## License Information

HtmlKit is Copyright (C) 2015-2022 Jeffrey Stedfast and is licensed under the MIT license:

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

## Installing via NuGet

The easiest way to install HtmlKit is via [NuGet](https://www.nuget.org/packages/HtmlKit/).

In Visual Studio's [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console),
simply enter the following command:

    Install-Package HtmlKit

## Getting the Source Code

First, you'll need to clone HtmlKit from my GitHub repository. To do this using the command-line version of Git,
you'll need to issue the following command in your terminal:

    git clone https://github.com/jstedfast/HtmlKit.git

If you are using [TortoiseGit](https://tortoisegit.org) on Windows, you'll need to right-click in the directory
where you'd like to clone HtmlKit and select **Git Clone...** in the menu. Once you do that, you'll get a dialog
asking you to specify the repository you'd like to clone. In the textbox labeled **URL:**, enter
`https://github.com/jstedfast/HtmlKit.git` and then click **OK**. This will clone HtmlKit onto your local machine.

## Updating the Source Code

Occasionally you might want to update your local copy of the source code if I have made changes to HtmlKit since you
downloaded the source code in the step above. To do this using the command-line version fo Git, you'll need to issue
the following command in your terminal within the HtmlKit directory:

    git pull

If you are using [TortoiseGit](https://tortoisegit.org) on Windows, you'll need to right-click on the HtmlKit
directory and select **Git Sync...** in the menu. Once you do that, you'll need to click the **Pull** button.

## Building

Once you've opened **HtmlKit.sln** solution file in [Visual Studio](https://www.visualstudio.com/downloads/),
you can choose the **Debug** or **Release** build configuration and then build.

Both Visual Studio 2022 and Visual Studio 2019 should be able to build HtmlKit without any issues, but older versions such as
Visual Studio 2015 and 2017 will likely require modifications to the projects in order to build correctly.

Note: The **Release** build will generate the xml API documentation, but the **Debug** build will not.

## Using HtmlKit

### Parsing HTML

The primary purpose of HtmlKit is parsing HTML.

```csharp
using (var reader = new StreamReader (stream)) {
    var tokenizer = new HtmlTokenizer (reader);
    HtmlToken token;

    // ReadNextToken() returns `false` when the end of the stream is reached.
    while (tokenizer.ReadNextToken (out token)) {
        switch (token.Kind) {
        case HtmlTokenKind.ScriptData:
        case HtmlTokenKind.CData:
        case HtmlTokenKind.Data:
            // ScriptData, CData, and Data tokens contain text data.
            var text = (HtmlDataToken) token;

            Console.WriteLine ("{0}: {1}", token.Kind, text.Data);
            break;
        case HtmlTokenKind.Tag:
            // Tag tokens represent tags and their attributes.
            var tag = (HtmlTagToken) token;

            Console.Write ("<{0}{1}", tag.IsEndTag ? "/" : "", tag.Name);

            foreach (var attribute in tag.Attributes) {
                if (attribute.Value != null)
                    Console.Write (" {0}={1}", attribute.Name, Quote (attribute.Value));
                else
                    Console.Write (" {0}", attribute.Name);
            }

            Console.WriteLine (tag.IsEmptyElement ? "/>" : ">");
            break;
        case HtmlTokenKind.Comment:
            var comment = (HtmlCommentToken) token;

            Console.WriteLine ("Comment: {0}", comment.Comment);
            break;
        case HtmlTokenKind.DocType:
            var doctype = (HtmlDocTypeToken) token;

            if (doctype.ForceQuirksMode)
                Console.Write ("<!-- force quirks mode -->");

            Console.Write ("<!DOCTYPE");

            if (doctype.Name != null)
                Console.Write (" {0}", doctype.Name.ToUpperInvariant ());

            if (doctype.PublicIdentifier != null) {
                Console.Write (" PUBLIC \"{0}\"", doctype.PublicIdentifier);
                if (doctype.SystemIdentifier != null)
                    Console.Write (" \"{0}\"", doctype.SystemIdentifier);
            } else if (doctype.SystemIdentifier != null) {
                Console.Write (" SYSTEM \"{0}\"", doctype.SystemIdentifier);
            }

            Console.WriteLine (">");
            break;
        }
    }
}
```

## Contributing

The first thing you'll need to do is fork HtmlKit to your own GitHub repository. For instructions on how to
do that, see the section titled **Getting the Source Code**.

If you use [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) or [MonoDevelop](http://monodevelop.com),
all of the solution files are configured with the coding style used by HtmlKit. If you use Visual Studio on Windows or
some other editor, please try to maintain the existing coding style as best as you can.

Once you've got some changes that you'd like to submit upstream to the official HtmlKit repository,
send me a **Pull Request** and I will try to review your changes in a timely manner.

If you'd like to contribute but don't have any particular features in mind to work on, check out the issue
tracker and look for something that might pique your interest!

## Reporting Bugs

Have a bug or a feature request? Please open a new
[bug report](https://github.com/jstedfast/HtmlKit/issues/new?template=bug_report.md)
or
[feature request](https://github.com/jstedfast/HtmlKit/issues/new?template=feature_request.md).

Before opening a new issue, please search through any [existing issues](https://github.com/jstedfast/HtmlKit/issues)
to avoid submitting duplicates.

If you are getting an exception from somewhere within HtmlKit, don't just provide the `Exception.Message`
string. Please include the `Exception.StackTrace` as well. The `Message`, by itself, is often useless.

## Documentation

API documentation can be found in the source code in the form of XML doc comments.
