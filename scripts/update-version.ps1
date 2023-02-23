[CmdletBinding()]
param (
    [Parameter()]
    [string] $ProjectName = "HtmlKit",
    [string] $Version
)

function Update-AssemblyVersionValue {
    param (
        [string] $AssemblyInfoPath,
        [string] $AttributeName,
        [string] $Version
    )

    $pattern = "\[assembly:(\s+)$AttributeName(\s+)\(""(.*?)""\)\]"
    (Get-Content $AssemblyInfoPath) | ForEach-Object {
        $_ -creplace $pattern, "[assembly:`$1$AttributeName`$2(""$Version"")]"
    } | Set-Content $AssemblyInfoPath
}

function Update-AssemblyInfo {
    param (
        [string] $AssemblyInfoPath,
        [string] $Version
    )

    $versionInfo = $Version.Split('.')
    if ($versionInfo.Count -ge 2) {
        $assemblyVersion = $versionInfo[0] + "." + $versionInfo[1] + ".0.0"
    } else {
        $assemblyVersion = $versionInfo[0] + ".0.0.0"
    }

    switch ($versionInfo.Count) {
        3 { $assemblyFileVersion = $Version + ".0" }
        2 { $assemblyFileVersion = $Version + ".0.0" }
        1 { $assemblyFileVersion = $Version + ".0.0.0" }
        default { $assemblyFileVersion = $Version }
    }

    Write-Host "AssemblyInformationalVersion: $assemblyFileVersion"
    Write-Host "AssemblyFileVersion: $assemblyFileVersion"
    Write-Host "AssemblyVersion: $assemblyVersion"

    Update-AssemblyVersionValue -AssemblyInfoPath $AssemblyInfoPath -AttributeName "AssemblyInformationalVersion" -Version $assemblyFileVersion
    Update-AssemblyVersionValue -AssemblyInfoPath $AssemblyInfoPath $csharp -AttributeName "AssemblyFileVersion" -Version $assemblyFileVersion
    Update-AssemblyVersionValue -AssemblyInfoPath $AssemblyInfoPath $csharp -AttributeName "AssemblyVersion" -Version $assemblyVersion
}

function Update-Project {
    param (
        [string] $ProjectFile,
        [string] $Version
    )

    $project = New-Object -TypeName System.Xml.XmlDocument
    $project.PreserveWhitespace = $True
    $project.Load($ProjectFile)
    $versionPrefix = $project.SelectSingleNode("/Project/PropertyGroup/VersionPrefix")
    $versionPrefix.InnerText = $Version
    $project.Save($ProjectFile)
}

function Update-NuGetPackageVersion {
    param (
        [string] $NuSpecFile,
        [string] $Version
    )

    $nuspec = New-Object -TypeName System.Xml.XmlDocument
    $nuspec.PreserveWhitespace = $True
    $nuspec.Load($NuSpecFile)
    $xmlns = $nuspec.package.GetAttribute("xmlns")
    $ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
    $ns.AddNamespace("ns", $xmlns)
    $packageVersion = $nuspec.SelectSingleNode("ns:package/ns:metadata/ns:version", $ns)
    $packageVersion.InnerText = $Version
    $nuspec.Save($NuSpecFile)
}

$assemblyInfo = Join-Path $ProjectName "Properties" "AssemblyInfo.cs"
$assemblyInfo = Resolve-Path $assemblyInfo
Write-Host "Updating $assemblyInfo..."
Update-AssemblyInfo -AssemblyInfoPath $assemblyInfo -Version $Version

$fileName = $ProjectName + ".csproj"
$projectFile = Join-Path $ProjectName $fileName
$projectFile = Resolve-Path $projectFile
Write-Host "Updating $projectFile..."
Update-Project -ProjectFile $projectFile -Version $Version

$fileName = $ProjectName + ".nuspec"
$nuspec = Join-Path "nuget" $fileName
$nuspec = Resolve-Path $nuspec
Write-Host "Updating $nuspec..."
Update-NuGetPackageVersion -NuSpecFile $nuspec -Version $Version
