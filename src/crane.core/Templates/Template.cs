﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crane.Core.Configuration;
using Crane.Core.IO;
using Crane.Core.Templates.Parsers;

namespace Crane.Core.Templates
{
    public class Template : ITemplate
    {
        private readonly IFileManager _fileManager;
        private readonly ITemplateParser _templateParser;
        private readonly IFileAndDirectoryTokenParser _fileAndDirectoryTokenParser;
        private readonly ICraneContext _context;
        private readonly IConfiguration _configuration;
        private DirectoryInfo _templateInstallDirectory;
        private readonly ITokenDictionaryFactory _tokenDictionaryFactory;

        public Template(
            ICraneContext context, 
            IConfiguration configuration, 
            IFileManager fileManager, 
            ITemplateParser templateParser, 
            IFileAndDirectoryTokenParser fileAndDirectoryTokenParser, ITokenDictionaryFactory tokenDictionaryFactory)
        {
            _fileManager = fileManager;
            _templateParser = templateParser;
            _fileAndDirectoryTokenParser = fileAndDirectoryTokenParser;
            _tokenDictionaryFactory = tokenDictionaryFactory;
            _context = context;
            _configuration = configuration;
        }
           
        public string Name { get; set; }

        public TemplateType TemplateType { get; set; }

        public DirectoryInfo TemplateSourceDirectory { get; set; }

        public DirectoryInfo TemplateInstallDirectory
        {
            get
            {
                if (_templateInstallDirectory == null)
                {
                    _templateInstallDirectory = new DirectoryInfo(Path.Combine(this._context.ProjectRootDirectory.FullName, this.TemplateInstallRootFolderName));
                }

                return _templateInstallDirectory; 
            }
        }

        public string TemplateInstallRootFolderName
        {
            get
            {
                switch (TemplateType)
                {
                    case TemplateType.Build:
                        return _configuration.BuildFolderName;
                    case TemplateType.Source:
                        return _configuration.SourceFolderName;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected IEnumerable<FileInfo> SourceFiles
        {
            get
            {
                return _fileManager
                    .EnumerateFiles(TemplateSourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
                    .Select(item => new FileInfo(item));
            }
        }

        protected IEnumerable<FileInfo> InstalledFiles
        {
            get
            {
                return _fileManager
                    .EnumerateFiles(TemplateInstallDirectory.FullName, "*.*",
                        SearchOption.AllDirectories)
                    .Select(item => new FileInfo(item));
            }
        } 
        

        public void Create(IProjectContext projectContext)
        {
            var tokenDictionary = _tokenDictionaryFactory.Create(_context, projectContext);
            _fileManager.CopyFiles(TemplateSourceDirectory.FullName, _context.ProjectRootDirectory.FullName, true);
            _fileAndDirectoryTokenParser.Parse(_context.ProjectRootDirectory, tokenDictionary);
            ParseTemplate(tokenDictionary);
        }


        private void ParseTemplate(ITokenDictionary tokenDictionary)
        {
            foreach (var file in this.InstalledFiles.Where(IsTextFile))
            {
                var parsed = _templateParser.Parse(tokenDictionary, _fileManager.ReadAllText(file.FullName));
                _fileManager.WriteAllText(file.FullName, parsed);
            }
        }

        private bool IsTextFile(FileInfo file)
        {
            return !file.Extension.Equals(".exe");
        }
    }
}