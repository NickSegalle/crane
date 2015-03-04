﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crane.Core.Api.Exceptions;
using Crane.Core.Api.Model;
using Crane.Core.Api.Readers;
using Crane.Core.Api.Writers;
using Crane.Core.Commands.Resolvers;
using Crane.Core.Extensions;
using Crane.Core.Runners;

namespace Crane.Core.Api
{
    public class CraneApi : ICraneApi
    {
        private readonly ISolutionReader _solutionReader;
        private readonly IAssemblyInfoWriter _assemblyInfoWriter;
        private readonly Func<ISolutionContext> _solutionContext;
        private readonly ISolutionPathResolver _solutionPathResolver;
        private readonly ISourceControlInformationReader _sourceControlInformationReader;
        private readonly INuget _nuget;

        public CraneApi(
            ISolutionReader solutionReader,
            IAssemblyInfoWriter assemblyInfoWriter,
            Func<ISolutionContext> solutionContext, 
            ISolutionPathResolver solutionPathResolver, 
            ISourceControlInformationReader sourceControlInformationReader,
            INuget nuget)
        {
            _solutionReader = solutionReader;
            _assemblyInfoWriter = assemblyInfoWriter;
            _solutionContext = solutionContext;
            _solutionPathResolver = solutionPathResolver;
            _sourceControlInformationReader = sourceControlInformationReader;
            _nuget = nuget;
        }

        public ISolutionContext GetSolutionContext(string rootFolderPath)
        {
            var context = _solutionContext();
            
            if (rootFolderPath.EndsWith(".sln"))
            {
                context.Solution = _solutionReader.FromPath(rootFolderPath);
                context.Path = new FileInfo(rootFolderPath).DirectoryName;
            }
            else
            {
                context.Solution =
                    _solutionReader.FromPath(Path.Combine(rootFolderPath, _solutionPathResolver.GetPath(rootFolderPath)));
                context.Path = rootFolderPath;
            }
            context.Solution.SolutionContext = context;
            return context;
        }

        public void PatchAssemblyInfo(AssemblyInfo assemblyInfo)
        {
            _assemblyInfoWriter.Patch(assemblyInfo);            
        }

        public void PatchSolutionAssemblyInfo(ISolutionContext solutionContext, string version)
        {
            var sourceControlInformation = GetSourceControlInformation(solutionContext);


            foreach (var project in solutionContext.Solution.Projects.Where(p => !p.TestProject && p.AssemblyInfo != null))
            {
                var ver = new Version(version);
                project.AssemblyInfo.Version = ver;
                project.AssemblyInfo.FileVersion = ver;

                if (sourceControlInformation == null)
                {
                    project.AssemblyInfo.InformationalVersion = ver.ToString();
                }
                else
                {
                    project.AssemblyInfo.InformationalVersion = string.Format("{0} / {1}", ver,
                        sourceControlInformation.LastCommitMessage);
                }


                _assemblyInfoWriter.Patch(project.AssemblyInfo);
            }    
        }

        public ISourceControlInformation GetSourceControlInformation(ISolutionContext solutionContext)
        {
            return _sourceControlInformationReader.ReadSourceControlInformation(solutionContext);
        }

        public IEnumerable<RunResult> NugetPublish(ISolutionContext solutionContext, string nugetOutputPath, string version, string source, string apiKey)
        {
            var nugetProjects  = solutionContext.Solution.Projects
                .Where(p => p.NugetSpec != null).ToArray();
            var results = new List<RunResult>(nugetProjects.Length);
            
            nugetProjects.ForEach(
                item =>
                {
                    var result = _nuget.Publish(
                            Path.Combine(nugetOutputPath, 
                            string.Format("{0}.{1}.nupkg", item.Name, version)),
                            source, apiKey);
                    results.Add(result);

                    if (result.ExitCode != 0 || 
                        (!string.IsNullOrEmpty(result.StandardOutput) && result.StandardOutput.Contains("invalid arguments")) ||
                        !string.IsNullOrEmpty(result.ErrorOutput))
                    {
                        throw new NugetException(string.Format("Error executing nuget push for project {0}{1}{2}",
                            item.Name, Environment.NewLine, result));
                    }
                });

            return results;
        }
    }
}