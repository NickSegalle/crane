﻿using System;
using System.IO;
using Crane.Core.Api.Builders;
using Crane.Core.IO;
using log4net;

namespace Crane.Integration.Tests.TestUtilities
{
    public class SolutionBuilderContext
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SolutionBuilderContext));
        private readonly IFileManager _fileManager;
        private readonly ISolutionBuilderFactory _builderFactory;
        private readonly DirectoryInfo _rootDirectory;
        
        public SolutionBuilderContext(IFileManager fileManager, ISolutionBuilderFactory builderFactory)
        {
            _fileManager = fileManager;
            _builderFactory = builderFactory;
            _rootDirectory = new DirectoryInfo(_fileManager.GetTemporaryDirectory());
            
        }

        public void CreateBuilder(string solutionName)
        {
            this.SolutionBuilder = _builderFactory.Create(Path.Combine(this.RootDirectory, solutionName));
        }

        public ISolutionBuilder SolutionBuilder { get; set; }

        public string RootDirectory
        {
            get { return _rootDirectory.FullName; }
        }
        
        public void TearDown()
        {
            try
            {
                _fileManager.Delete(_rootDirectory);
            }
            catch (Exception exception)
            {
                _log.Warn(string.Format("Error tearing down test, trying to delete temp directory {0}.", _rootDirectory.FullName), exception);
            }
        }
    }
}