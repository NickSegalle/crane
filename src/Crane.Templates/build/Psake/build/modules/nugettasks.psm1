Task NugetExists -Depends SetupContext {

  $global:context.nuget_file = Join-Path "$($global:context.build_dir)" NuGet.exe

  if (Test-Path $global:context.nuget_file){
    return
  }

  ((new-object net.webclient).DownloadFile('http://www.nuget.org/nuget.exe', $global:context.nuget_file))
}

Task NugetPack -Depends NugetExists {
    $global:context.solution_context.Solution.Projects | where { $_.NugetSpec -ne $null } | % {
        & $global:context.nuget_file @("pack"; $_.NugetSpec.Path)
    }
}

Task NugetPublish -Depends NugetExists, NugetPack {

}