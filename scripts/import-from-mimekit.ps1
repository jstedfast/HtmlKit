#
# import-from-mimekit.ps1
#
# Re-imports HtmlKit's source files from MimeKit (where they live under MimeKit/Text and friends)
# and applies the fixups needed to turn them into HtmlKit sources.
#
# See IMPORTING.md in the root of the repository for details.
#
[CmdletBinding()]
param (
    # Path to the root of a MimeKit checkout (the directory containing MimeKit\MimeKit.csproj's parent).
    [string] $MimeKitPath = (Join-Path $PSScriptRoot "..\..\MimeKit"),

    # Build HtmlKit after importing.
    [switch] $Build,

    # Build and run the HtmlKit unit tests after importing.
    [switch] $Test
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$HtmlKitDir = Join-Path $RepoRoot "HtmlKit"
$ProjectFile = Join-Path $HtmlKitDir "HtmlKit.csproj"

if (-not (Test-Path (Join-Path $MimeKitPath "MimeKit\MimeKit.csproj"))) {
    throw "Could not find MimeKit\MimeKit.csproj under '$MimeKitPath'. Use -MimeKitPath to specify the MimeKit checkout."
}

$MimeKitPath = (Resolve-Path $MimeKitPath).Path
$MimeKitSrc = Join-Path $MimeKitPath "MimeKit"

# Source files to import: MimeKit path (relative to MimeKit\MimeKit) => HtmlKit file name (relative to HtmlKit\).
# When MimeKit's HtmlTokenizer/HtmlWriter start depending on a new MimeKit/Text file, add it here.
$ImportedFiles = [ordered] @{
    "Text\CharBuffer.cs"               = "CharBuffer.cs"
    "Text\HtmlAttribute.cs"            = "HtmlAttribute.cs"
    "Text\HtmlAttributeCollection.cs"  = "HtmlAttributeCollection.cs"
    "Text\HtmlAttributeId.cs"          = "HtmlAttributeId.cs"
    "Text\HtmlEntityDecoder.cs"        = "HtmlEntityDecoder.cs"
    "Text\HtmlEntityDecoder.g.cs"      = "HtmlEntityDecoder.g.cs"
    "Text\HtmlNamespace.cs"            = "HtmlNamespace.cs"
    "Text\HtmlOpenElementStack.cs"     = "HtmlOpenElementStack.cs"
    "Text\HtmlTagId.cs"                = "HtmlTagId.cs"
    "Text\HtmlToken.cs"                = "HtmlToken.cs"
    "Text\HtmlTokenizer.cs"            = "HtmlTokenizer.cs"
    "Text\HtmlTokenizerState.cs"       = "HtmlTokenizerState.cs"
    "Text\HtmlTokenKind.cs"            = "HtmlTokenKind.cs"
    "Text\HtmlUtils.cs"                = "HtmlUtils.cs"
    "Text\HtmlWriter.cs"               = "HtmlWriter.cs"
    "Text\HtmlWriterState.cs"          = "HtmlWriterState.cs"
    "Utils\OptimizedOrdinalComparer.cs" = "OptimizedOrdinalComparer.cs"
    "NullableAttributes.cs"            = "NullableAttributes.cs"
}

# Unit test files to import: MimeKit path (relative to MimeKit\UnitTests) => HtmlKit file name (relative to UnitTests\).
# Note: UnitTests\OptimizedOrdinalComparerTests.cs is intentionally HtmlKit-specific (it is wrapped in an #if).
$ImportedTestFiles = [ordered] @{
    "Text\HtmlAttributeCollectionTests.cs" = "HtmlAttributeCollectionTests.cs"
    "Text\HtmlAttributeTests.cs"           = "HtmlAttributeTests.cs"
    "Text\HtmlEntityDecoderTests.cs"       = "HtmlEntityDecoderTests.cs"
    "Text\HtmlTagIdTests.cs"               = "HtmlTagIdTests.cs"
    "Text\HtmlTokenizerTests.cs"           = "HtmlTokenizerTests.cs"
    "Text\HtmlTokenTests.cs"               = "HtmlTokenTests.cs"
    "Text\HtmlUtilsTests.cs"               = "HtmlUtilsTests.cs"
    "Text\HtmlWriterTests.cs"              = "HtmlWriterTests.cs"
}

# Test data directories mirrored from MimeKit\UnitTests (relative to UnitTests\ in both repos).
$ImportedTestDataDirs = @(
    "TestData\html"
)

# Files that only exist in HtmlKit and must never be overwritten by an import.
$HtmlKitOnlyFiles = @(
    "Properties\AssemblyInfo.cs"
    "StringComparers.cs"
)

# HtmlKit-only replacement for MimeKit.Utils.MimeUtils.OrdinalIgnoreCase (created if missing).
$StringComparersSource = @'
//
// StringComparers.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2015-{YEAR} Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace HtmlKit {
	// Note: This is the HtmlKit equivalent of MimeKit.Utils.MimeUtils.OrdinalIgnoreCase. The
	// import-from-mimekit.ps1 script rewrites references to MimeUtils.OrdinalIgnoreCase to use this.
	static class StringComparers
	{
		public static readonly IEqualityComparer<string> OrdinalIgnoreCase;

		static StringComparers ()
		{
#if NETFRAMEWORK || NETSTANDARD2_0
			OrdinalIgnoreCase = new OptimizedOrdinalIgnoreCaseComparer ();
#else
			OrdinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
#endif
		}
	}
}
'@

# Simple literal text replacements applied to every imported file.
$TextReplacements = [ordered] @{
    # MimeKit.Utils.MimeUtils is not part of HtmlKit.
    "MimeUtils.OrdinalIgnoreCase" = "StringComparers.OrdinalIgnoreCase"

    # HtmlNamespaceExtensions is internal in MimeKit but has always been public API in HtmlKit.
    "`tstatic class HtmlNamespaceExtensions" = "`tpublic static class HtmlNamespaceExtensions"

    # Doc comments that refer readers to MimeKit's HtmlToHtml (which does not exist in HtmlKit).
    "which would allow it to bypass an <see cref=""HtmlToHtml.HtmlTagCallback""/>." = "which would allow it to bypass an HTML filter."
    "filter. See <see cref=""HtmlToHtml.NoScriptHandling""/> for details.</note>" = "filter.</note>"
}

function Read-SourceFile ([string] $Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = (New-Object System.Text.UTF8Encoding $false).GetString($bytes, $(if ($hasBom) { 3 } else { 0 }), $bytes.Length - $(if ($hasBom) { 3 } else { 0 }))

    return @{ Text = $text; HasBom = $hasBom }
}

function Write-SourceFile ([string] $Path, [string] $Text, [bool] $HasBom) {
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $HasBom))
}

function Get-EolNeutralHash ([string] $Path) {
    $text = [System.Text.Encoding]::Latin1.GetString([System.IO.File]::ReadAllBytes($Path)).Replace("`r", "")
    return [Convert]::ToBase64String([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::Latin1.GetBytes($text)))
}

$TypeDeclRegex = '(?m)^[ \t]*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|ref)\s+)*(?:class|struct|enum|interface|record)\s+(\w+)'
$DelegateDeclRegex = '(?m)^[ \t]*(?:(?:public|internal|private|protected)\s+)*delegate\s+[\w<>\[\]?.,\s]+?\s+(\w+)\s*[<(]'

function Get-DeclaredTypeNames ([string] $Text) {
    $names = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($m in [regex]::Matches($Text, $TypeDeclRegex)) { [void] $names.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($Text, $DelegateDeclRegex)) { [void] $names.Add($m.Groups[1].Value) }
    return , $names
}

# Load the sources to import.
$sources = [ordered] @{}
foreach ($entry in $ImportedFiles.GetEnumerator()) {
    $srcPath = Join-Path $MimeKitSrc $entry.Key
    if (-not (Test-Path $srcPath)) {
        throw "MimeKit source file '$srcPath' no longer exists. Update `$ImportedFiles in $($MyInvocation.MyCommand.Name)."
    }
    $sources[$entry.Value] = Read-SourceFile $srcPath
}

# Figure out which types exist in MimeKit but will not exist in HtmlKit so that doc comment
# references to them (e.g. <see cref="HtmlToHtml"/>) can be unlinked.
$importedTypes = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($src in $sources.Values) { $importedTypes.UnionWith((Get-DeclaredTypeNames $src.Text)) }
$importedTypes.Add("StringComparers") | Out-Null

$mimeKitOnlyTypes = New-Object 'System.Collections.Generic.HashSet[string]'
Get-ChildItem $MimeKitSrc -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $mimeKitOnlyTypes.UnionWith((Get-DeclaredTypeNames ([System.IO.File]::ReadAllText($_.FullName))))
}
$mimeKitOnlyTypes.ExceptWith($importedTypes)

$warnings = New-Object System.Collections.Generic.List[string]

foreach ($name in $sources.Keys) {
    $text = $sources[$name].Text

    # 1. Copyright: MimeKit's copyright line => HtmlKit's (keeping the end year).
    $text = [regex]::Replace($text, '(?m)^// Copyright \(c\) \d{4}-(\d{4}) \.NET Foundation and Contributors', '// Copyright (c) 2015-$1 Jeffrey Stedfast <jestedfa@microsoft.com>')

    # 2. Remove "using MimeKit.*;" lines (and the blank line that follows them).
    $text = [regex]::Replace($text, '(?m)^using MimeKit(?:\.\w+)*;\r?\n(?:\r?\n)?', '')

    # 3. Namespace: MimeKit / MimeKit.Text / MimeKit.Utils => HtmlKit.
    $text = [regex]::Replace($text, '(?m)^namespace MimeKit(?:\.\w+)*', 'namespace HtmlKit')
    $text = [regex]::Replace($text, '\bMimeKit\.(?:Text|Utils)\.(?=[A-Z])', 'HtmlKit.')

    # 4. Remove <example> doc comment blocks (they reference MimeKit's Examples\*.cs files).
    $text = [regex]::Replace($text, '(?m)^[ \t]*/// <example>\r?\n(?:^[ \t]*///.*\r?\n)*?^[ \t]*/// </example>\r?\n', '')
    $text = [regex]::Replace($text, '(?m)^[ \t]*/// <example>.*</example>[ \t]*\r?\n', '')

    # 5. Literal replacements.
    foreach ($r in $TextReplacements.GetEnumerator()) {
        $text = $text.Replace($r.Key, $r.Value)
    }

    # 6. Unlink doc comment references to MimeKit-only types.
    $text = [regex]::Replace($text, '(?m)^[ \t]*/// <seealso cref="(?:[TMPFE]:)?(\w+)[^"]*"\s*/>[ \t]*\r?\n', {
        param ($m)
        if ($mimeKitOnlyTypes.Contains($m.Groups[1].Value)) { $warnings.Add("${name}: removed $($m.Value.Trim())"); return '' }
        return $m.Value
    })
    $text = [regex]::Replace($text, '<see cref="(?:[TMPFE]:)?((\w+)[\w.]*)(?:\([^"]*\))?"\s*/>', {
        param ($m)
        if ($mimeKitOnlyTypes.Contains($m.Groups[2].Value)) { $warnings.Add("${name}: unlinked $($m.Value) (review the surrounding doc text)"); return "<c>$($m.Groups[1].Value)</c>" }
        return $m.Value
    })

    # 7. Report anything MimeKit-specific that is left over.
    $lineNumber = 0
    foreach ($line in ($text -split "`n")) {
        $lineNumber++
        if ($line -match '\bMimeKit\b|\bMimeUtils\b|\.NET Foundation') {
            $warnings.Add("${name}:${lineNumber}: leftover MimeKit reference: $($line.Trim())")
        }
    }

    Write-SourceFile (Join-Path $HtmlKitDir $name) $text $sources[$name].HasBom
    Write-Output "Imported $name"
}

# Ensure the HtmlKit-only StringComparers.cs helper exists.
$stringComparersPath = Join-Path $HtmlKitDir "StringComparers.cs"
if (-not (Test-Path $stringComparersPath)) {
    $text = $StringComparersSource.Replace("{YEAR}", (Get-Date).Year.ToString()) -replace '\r?\n', "`r`n"
    Write-SourceFile $stringComparersPath ($text + "`r`n") $true
    Write-Output "Created StringComparers.cs"
}

# Ensure every imported/HtmlKit-only file is listed in HtmlKit.csproj (it uses explicit <Compile> items).
$projectText = [System.IO.File]::ReadAllText($ProjectFile)
$projectChanged = $false
foreach ($name in @($ImportedFiles.Values) + $HtmlKitOnlyFiles) {
    if ($projectText -match ('<Compile Include="' + [regex]::Escape($name) + '"')) { continue }

    # Insert alphabetically among the existing top-level <Compile> items.
    $items = [regex]::Matches($projectText, '(?m)^([ \t]*)<Compile Include="([^"\\]+)"')
    $before = $items | Where-Object { [string]::Compare($_.Groups[2].Value, $name, [StringComparison]::OrdinalIgnoreCase) -gt 0 } | Select-Object -First 1
    if ($null -eq $before) { $before = $items[$items.Count - 1] }
    $indent = $before.Groups[1].Value
    $projectText = $projectText.Insert($before.Index, "$indent<Compile Include=""$name"" />`r`n")
    $projectChanged = $true
    Write-Output "Added $name to HtmlKit.csproj"
}
if ($projectChanged) {
    [System.IO.File]::WriteAllText($ProjectFile, $projectText, (New-Object System.Text.UTF8Encoding $false))
}

# Report HtmlKit source files that are neither imported nor known HtmlKit-only files.
Get-ChildItem $HtmlKitDir -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $rel = $_.FullName.Substring($HtmlKitDir.Length + 1)
    if (-not ($ImportedFiles.Values -contains $rel) -and -not ($HtmlKitOnlyFiles -contains $rel)) {
        $warnings.Add("HtmlKit\$rel is not imported from MimeKit; delete it if it is no longer needed.")
    }
}

# Import the unit tests.
$testsDir = Join-Path $RepoRoot "UnitTests"
foreach ($entry in $ImportedTestFiles.GetEnumerator()) {
    $srcPath = Join-Path $MimeKitPath "UnitTests\$($entry.Key)"
    if (-not (Test-Path $srcPath)) {
        throw "MimeKit test file '$srcPath' no longer exists. Update `$ImportedTestFiles in $($MyInvocation.MyCommand.Name)."
    }

    $src = Read-SourceFile $srcPath
    $text = [regex]::Replace($src.Text, '(?m)^// Copyright \(c\) \d{4}-(\d{4}) \.NET Foundation and Contributors', '// Copyright (c) 2015-$1 Jeffrey Stedfast <jestedfa@microsoft.com>')
    $text = [regex]::Replace($text, '(?m)^using MimeKit\.Text;', 'using HtmlKit;')
    $text = [regex]::Replace($text, '(?m)^namespace UnitTests\.\w+', 'namespace UnitTests')
    $text = [regex]::Replace($text, '\bMimeKit\.Text\.(?=[A-Z])', 'HtmlKit.')
    $text = $text.Replace("MimeUtils.OrdinalIgnoreCase", "StringComparers.OrdinalIgnoreCase")

    $lineNumber = 0
    foreach ($line in ($text -split "`n")) {
        $lineNumber++
        if ($line -match '\bMimeKit\b|\bMimeUtils\b|\.NET Foundation') {
            $warnings.Add("UnitTests\$($entry.Value):${lineNumber}: leftover MimeKit reference: $($line.Trim())")
        }
    }

    Write-SourceFile (Join-Path $testsDir $entry.Value) $text $src.HasBom
    Write-Output "Imported UnitTests\$($entry.Value)"
}

# Mirror the test data (byte-for-byte copies).
foreach ($dir in $ImportedTestDataDirs) {
    $srcDir = Join-Path $MimeKitPath "UnitTests\$dir"
    $dstDir = Join-Path $testsDir $dir
    New-Item -ItemType Directory -Force $dstDir | Out-Null

    $copied = 0
    Get-ChildItem $srcDir -File | ForEach-Object {
        $dst = Join-Path $dstDir $_.Name
        # Note: Ignore line-ending-only differences (git may check files out with different EOLs in each repo).
        if (-not (Test-Path $dst) -or (Get-EolNeutralHash $_.FullName) -ne (Get-EolNeutralHash $dst)) {
            Copy-Item $_.FullName $dst -Force
            $copied++
        }
    }
    Get-ChildItem $dstDir -File | Where-Object { -not (Test-Path (Join-Path $srcDir $_.Name)) } | ForEach-Object {
        $warnings.Add("UnitTests\$dir\$($_.Name) does not exist in MimeKit; delete it if it is no longer needed.")
    }
    Write-Output "Updated $copied file(s) in UnitTests\$dir"
}

Write-Output ""
Write-Output "Imported from MimeKit commit: $(git -C $MimeKitPath rev-parse --short HEAD) ($(git -C $MimeKitPath log -1 --format=%s))"

if ($warnings.Count -gt 0) {
    Write-Output ""
    Write-Output "Warnings (review these manually):"
    $warnings | ForEach-Object { Write-Warning $_ }
}

if ($Build -or $Test) {
    Write-Output ""
    & dotnet build (Join-Path $RepoRoot "HtmlKit.sln") -c Debug
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

if ($Test) {
    & dotnet test (Join-Path $RepoRoot "UnitTests\UnitTests.csproj") -c Debug --no-build
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
}
