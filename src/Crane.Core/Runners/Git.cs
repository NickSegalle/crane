﻿using System.Diagnostics;
using System.Text;
using log4net;

namespace Crane.Core.Runners
{
    public class Git
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Git));

        public RunResult Run(string command, string workingDirectory)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = workingDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "git",
                    Arguments = command
                }
            };

            var error = new StringBuilder();
            var output = new StringBuilder();

            process.ErrorDataReceived += (sender, args) => error.Append(args.Data);
            process.OutputDataReceived += (sender, args) => output.Append(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            _log.DebugFormat("standard out: {0}  error: {1}", output, error);

            return new RunResult(
                string.Format("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments),
                output.ToString(),
                error.ToString(),
                process.ExitCode);
        } 
    }
}