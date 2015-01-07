Task NugetExists {

  $nugetFile = Join-Path "$($global:context.build_dir)" NuGet.exe

  if (Test-Path $nugetFile){
    return
  }

  ((new-object net.webclient).DownloadFile('http://www.nuget.org/nuget.exe', $nugetFile))
}

Task Clean {
  Write-Host "Creating build-output directory" -ForegroundColor Green
  if (Test-Path $($global:context.build_artifacts_dir)){
    rd $($global:context.build_artifacts_dir) -rec -force | out-null
  }

  mkdir $($global:context.build_artifacts_dir) | out-null

  Write-Host "Cleaning $($global:context.sln_file_info.Name) ($($global:context.configuration))" -ForegroundColor Green
  Exec { msbuild $($global:context.sln_file_info.FullName) /t:Clean /p:Configuration=$($global:context.configuration) /v:quiet }
}


Task NugetRestore -Depends NugetExists {
  & "$($global:context.build_dir)\nuget.exe" @('restore', $($global:context.sln_file_info.FullName))
}

Task Build -Depends Clean, NugetRestore {
  Write-Host "Building $($global:context.sln_file_info.Name) ($($global:context.configuration))" -ForegroundColor Green
  $verboseLevel = "quiet"
  if ($verbose) {
    $verboseLevel = "normal"
  }
  Exec { msbuild "$($global:context.sln_file_info.FullName)" /t:ReBuild /p:Configuration=$($global:context.configuration) /v:$verboseLevel /p:OutDir=$($global:context.build_artifacts_dir) }
}


Task Test {
  $xunit_consoleRunner = Join-Path $($global:context.sln_file_info.Directory.FullName) "\packages\xunit.runners.**\tools\xunit.console.clr4.exe"

  Get-ChildItem -Path $($global:context.build_artifacts_dir) -Filter *.Tests.dll |
  % {
    Debug("$xunit_consoleRunner @($($_.FullName), '/silent')")
    & $xunit_consoleRunner @($_.FullName, '/silent')
    if($LastExitCode -ne 0){
      throw "failed executing tests $_.FullName. See last error."
    }
  }
}

function Debug($message){
  Write-Verbose $message
}


function Get-GitCommit{
  $gitLog = git log --oneline -1
  return $gitLog.Split(' ')[0]
}

function Get-VersionFromGitTag{
  $gitTag = git describe --tags --abbrev=0
  return $gitTag.Replace("v", "") + ".0"
}

function Invoke-GenerateAssemblyInfo{
  param(
    [string]$clsCompliant = "true",
    [string]$title,
    [string]$description,
    [string]$company,
    [string]$product,
    [string]$copyright,
    [string]$version,
    [string]$file = $(throw "file is a required parameter.")
  )

  $commit = Get-GitCommit
  $asmInfo = "using System;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using System.Runtime.InteropServices;

  [assembly: CLSCompliantAttribute($clsCompliant )]
  [assembly: ComVisibleAttribute(false)]
  [assembly: AssemblyTitleAttribute(""$title"")]
  [assembly: AssemblyDescriptionAttribute(""$description"")]
  [assembly: AssemblyCompanyAttribute(""$company"")]
  [assembly: AssemblyProductAttribute(""$product"")]
  [assembly: AssemblyCopyrightAttribute(""$copyright"")]
  [assembly: AssemblyVersionAttribute(""$version"")]
  [assembly: AssemblyInformationalVersionAttribute(""$version / $commit"")]
  [assembly: AssemblyFileVersionAttribute(""$version"")]
  [assembly: AssemblyDelaySignAttribute(false)]
  "

  $dir = [System.IO.Path]::GetDirectoryName($file)
  if ([System.IO.Directory]::Exists($dir) -eq $false){
    Write-Host "Creating directory $dir"
    [System.IO.Directory]::CreateDirectory($dir)
  }
  Write-Host "Generating assembly info file: $file"
  Write-Output $asmInfo > $file
}

Task PatchAssemblyInfo {
  $version = $global:context.build_version
  Invoke-GenerateAssemblyInfo  -title "Crane.Core" -description "Core crane functionality" -version $version -file "$($global:context.sln_file_info.Directory.FullName)\src\crane.core\Properties\AssemblyInfo.cs"

  if ($($global:context.teamcity_build)) {
    Write-Host "##teamcity[buildNumber '$version']"
  }
}
