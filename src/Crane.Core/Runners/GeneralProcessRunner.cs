using System.Diagnostics;
using System.Text;

namespace Crane.Core.Runners
{
    /// <summary>
    /// Runs a process, capturing the standard output, error output and exit code.
    /// </summary>
    public static class GeneralProcessRunner
    {
        public static RunResult Run(string fileName, string arguments, string workingDirectory = null)
        {
            var error = new StringBuilder();
            var output = new StringBuilder();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = fileName,
                    Arguments = arguments,
                }
            };

            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            process.ErrorDataReceived += (sender, args) => error.AppendLine(args.Data);
            process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return new RunResult(
                string.Format("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments),
                output.ToString(),
                error.ToString(),
                process.ExitCode);
        }

    }
}