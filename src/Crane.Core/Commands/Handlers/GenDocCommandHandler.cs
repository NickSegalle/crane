﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crane.Core.Commands.Resolvers;
using Crane.Core.Configuration;
using Crane.Core.Documentation.Formatters;
using Crane.Core.Documentation.Providers;
using Crane.Core.Extensions;
using Crane.Core.IO;

namespace Crane.Core.Commands.Handlers
{
    public class GenDocCommandHandler : CommandHandler<GenDoc>
    {
        private readonly IFileManager _fileManager;
        private readonly IHelpProvider _helpProvider;
        private readonly IHelpFormatter _helpFormatter;
        private readonly IEnumerable<ICraneCommand> _commands;
        private string _docDirectory;
        private string _rootDirectory;
        private string _docTemplateDirectory;

        public GenDocCommandHandler(IFileManager fileManager, IPublicCommandResolver commandResolver, IHelpProvider helpProvider, MarkdownHelpFormatter helpFormatter)
        {
            _fileManager = fileManager;
            _helpProvider = helpProvider;
            _helpFormatter = helpFormatter;
            _commands = commandResolver.Resolve().OrderBy(command => command.Name());
        }

        protected override void DoHandle(GenDoc command)
        {
            var craneExeDirectory = this.GetType().Assembly.GetLocation().FullName;
            _rootDirectory = Path.GetFullPath(Path.Combine(craneExeDirectory, ".."));
            _docTemplateDirectory = Path.Combine(craneExeDirectory, "Crane.Docs");
            _docDirectory = Path.Combine(_rootDirectory, "doc");
                
            if (!_fileManager.DirectoryExists(_docDirectory))
            {
                _fileManager.CreateDirectory(_docDirectory);
            }

            this.UpdateMkdocsYml();
            this.CreateIndex();
            _commands.ForEach(this.CreateCommandPage);
        }

        private void UpdateMkdocsYml()
        {
            var mkdocsTemplateFilePath = Path.Combine(_docTemplateDirectory, "mkdocs.yml");
            var mkdocsContent = _fileManager.ReadAllText(mkdocsTemplateFilePath);
            var result = new StringBuilder();
            _commands.ForEach(command => result.AppendLine(string.Format(" - ['{0}.md', 'Commands', '{0}']", command.Name())));
            _fileManager.WriteAllText(Path.Combine(_rootDirectory, "mkdocs.yml"), mkdocsContent.Replace("%crane.commandpages%", result.ToString().TrimEnd()));
        }

        private void CreateIndex()
        {
            var indexTemplate = _fileManager.ReadAllText(Path.Combine(_docTemplateDirectory, "index.md"));
            var result = new StringBuilder();
            foreach (var command in _commands)
            {
                var helpCommand = _helpProvider.HelpCollection.Get(command.Name());
                result.AppendLine(_helpFormatter.FormatSummary(helpCommand));
                result.AppendLine();
                result.AppendLine();
            }

            _fileManager.WriteAllText(Path.Combine(_docDirectory, "index.md"), indexTemplate.Replace("%crane.listcommands%", result.ToString().TrimEnd(Environment.NewLine.ToCharArray())));
        }

        private void CreateCommandPage(ICraneCommand command)
        {
            _fileManager.WriteAllText(Path.Combine(_docDirectory, command.Name() + ".md"),
            _helpFormatter.Format(_helpProvider.HelpCollection.Get(command.Name())));
        }
    }
}