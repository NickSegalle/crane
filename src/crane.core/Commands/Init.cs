﻿using System;
using System.IO;
using Crane.Core.Configuration;
using Crane.Core.IO;
using Crane.Core.Templates;
using Crane.Core.Templates.Resolvers;

namespace Crane.Core.Commands
{
    public class Init : ICraneCommand
    {
        private readonly ICraneContext _context;
        private readonly ITemplateResolver _templateResolver;
        private readonly IFileManager _fileManager;

        public Init(ICraneContext context, ITemplateResolver templateResolver, IFileManager fileManager)
        {
            _context = context;
            _templateResolver = templateResolver;
            _fileManager = fileManager;
        }

        public void Execute(string projectName)
        {
            _context.ProjectName = projectName;
            CreateProject();
            CreateBuild();
        }

        private void CreateProject()
        {
            _context.ProjectRootDirectory = new DirectoryInfo(Path.Combine(_fileManager.CurrentDirectory, _context.ProjectName));
            _fileManager.CreateDirectory(_context.ProjectRootDirectory.FullName);
        }

        private void CreateBuild()
        {
            var build = _templateResolver.Resolve(TemplateType.Build) as IBuildTemplate;
            if (build == null)
            {
                throw new Exception("Build template not found, please check your configuration");
            }

            build.Create();
        }
    }
}