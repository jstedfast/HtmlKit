# Importing HtmlKit sources from MimeKit

HtmlKit's tokenizer and writer are developed in [MimeKit](https://github.com/jstedfast/MimeKit) (under
`MimeKit/Text`) and copied into HtmlKit with a small set of mechanical fixups. Use
`scripts/import-from-mimekit.ps1` to re-import them.

## Quick start

```powershell
# From the root of the HtmlKit repository, with MimeKit checked out next to it (..\MimeKit):
.\scripts\import-from-mimekit.ps1

# Or specify the MimeKit checkout explicitly, then build and run the unit tests:
.\scripts\import-from-mimekit.ps1 -MimeKitPath C:\src\MimeKit -Test
```

The script prints the MimeKit commit it imported from (handy for the commit message), along with any warnings
that need manual review.

## What gets imported

| MimeKit (`MimeKit/...`)               | HtmlKit (`HtmlKit/...`)        |
|---------------------------------------|--------------------------------|
| `Text/CharBuffer.cs`                  | `CharBuffer.cs`                |
| `Text/HtmlAttribute.cs`               | `HtmlAttribute.cs`             |
| `Text/HtmlAttributeCollection.cs`     | `HtmlAttributeCollection.cs`   |
| `Text/HtmlAttributeId.cs`             | `HtmlAttributeId.cs`           |
| `Text/HtmlEntityDecoder.cs`           | `HtmlEntityDecoder.cs`         |
| `Text/HtmlEntityDecoder.g.cs`         | `HtmlEntityDecoder.g.cs`       |
| `Text/HtmlNamespace.cs`               | `HtmlNamespace.cs`             |
| `Text/HtmlOpenElementStack.cs`        | `HtmlOpenElementStack.cs`      |
| `Text/HtmlTagId.cs`                   | `HtmlTagId.cs`                 |
| `Text/HtmlToken.cs`                   | `HtmlToken.cs`                 |
| `Text/HtmlTokenizer.cs`               | `HtmlTokenizer.cs`             |
| `Text/HtmlTokenizerState.cs`          | `HtmlTokenizerState.cs`        |
| `Text/HtmlTokenKind.cs`               | `HtmlTokenKind.cs`             |
| `Text/HtmlUtils.cs`                   | `HtmlUtils.cs`                 |
| `Text/HtmlWriter.cs`                  | `HtmlWriter.cs`                |
| `Text/HtmlWriterState.cs`             | `HtmlWriterState.cs`           |
| `Utils/OptimizedOrdinalComparer.cs`   | `OptimizedOrdinalComparer.cs`  |
| `NullableAttributes.cs`               | `NullableAttributes.cs`        |

HtmlKit-only files, which the import never overwrites:

* `Properties/AssemblyInfo.cs`
* `StringComparers.cs`: the HtmlKit replacement for `MimeKit.Utils.MimeUtils.OrdinalIgnoreCase`. The script
  creates it if it's missing.

If MimeKit's `HtmlTokenizer`, `HtmlWriter`, or related classes start depending on another MimeKit file, add it to
`$ImportedFiles` at the top of the script. A build error about a missing type is the usual sign of this.

## Fixups the script applies

1. **Copyright:** `// Copyright (c) 2013-YYYY .NET Foundation and Contributors` becomes
   `// Copyright (c) 2015-YYYY Jeffrey Stedfast <jestedfa@microsoft.com>`.
2. **Usings:** `using MimeKit.*;` lines (and the blank line after them) are removed.
3. **Namespace:** `namespace MimeKit.Text` / `MimeKit.Utils` / `MimeKit` becomes `namespace HtmlKit`, and
   fully-qualified `MimeKit.Text.X` / `MimeKit.Utils.X` references become `HtmlKit.X`.
4. **`<example>` doc comments:** removed. They reference MimeKit's `Examples\*.cs` files.
5. **`MimeUtils.OrdinalIgnoreCase`:** becomes `StringComparers.OrdinalIgnoreCase`.
6. **Public API preservation:** `HtmlNamespaceExtensions` is `internal` in MimeKit but has always been
   `public` in HtmlKit, so it's kept `public`.
7. **Doc references to MimeKit-only types:** `<see cref="HtmlToHtml.X"/>` and similar references to types that
   exist in MimeKit but not in HtmlKit become `<c>HtmlToHtml.X</c>`. `<seealso>` lines for those types are
   removed. Each change is reported as a warning so you can reword the surrounding sentence if it no longer
   makes sense for HtmlKit.
8. **`HtmlKit.csproj`:** HtmlKit uses explicit `<Compile>` items, so any imported or HtmlKit-only file missing
   from the project gets added.

The script keeps each source file's encoding (UTF-8 BOM). It warns about leftover `MimeKit`, `MimeUtils`, or
`.NET Foundation` text, and about files in `HtmlKit/` that are neither imported nor known HtmlKit-only files
(for example, a file MimeKit no longer has).

## Unit tests and test data

The script also imports the unit tests (`$ImportedTestFiles`) and mirrors the test data (`$ImportedTestDataDirs`):

* `MimeKit/UnitTests/Text/Html{AttributeCollection,Attribute,EntityDecoder,TagId,Tokenizer,Token,Utils,Writer}Tests.cs`
  go to `UnitTests/`. The copyright line is fixed up, `using MimeKit.Text;` becomes `using HtmlKit;`, and
  `namespace UnitTests.Text` becomes `namespace UnitTests`.
* `MimeKit/UnitTests/TestData/html/*` is copied byte-for-byte into `UnitTests/TestData/html/`. Files that differ
  only in line endings are skipped. HtmlKit files with no MimeKit counterpart get a warning.
* `UnitTests/OptimizedOrdinalComparerTests.cs` and `UnitTests/TestHelper.cs` are HtmlKit-specific and aren't
  imported.

Tests for MimeKit-only types (`HtmlToHtml`, `HtmlTextPreviewer`, ...) aren't imported. If an imported test file
ever starts using one of them, the build fails; remove those tests by hand.

## After running the script

1. **Review the diff** (`git diff HtmlKit/`) and every warning the script printed.
2. **Check for public API changes.** HtmlKit is a published NuGet package, so look for changes to public types
   and members, especially public enums such as `HtmlTokenizerState`, and plan a version bump to match.
3. **Build and test:** `dotnet build HtmlKit.sln` and `dotnet test UnitTests\UnitTests.csproj`. MimeKit's tests
   and expected outputs are imported along with the sources, so the tests should pass as-is.
4. **`HtmlEntityDecoder.g.cs`** is generated. If `CodeGenerator/HtmlEntities.json` is out of sync with MimeKit's
   copy, update it too.
5. In the commit message, record the MimeKit commit the script printed.

## Keeping the two repositories in sync

The import is one-way, from MimeKit to HtmlKit. Make tokenizer and writer changes, including performance work,
in MimeKit first and then import them. Otherwise the next import silently reverts them.
