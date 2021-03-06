using Crane.Core.Commands.Execution;

namespace Crane.Core.Hosts
{
    public class ConsoleHost : IHost
    {
        private readonly ICommandExecutor _commandExecutor;

        public ConsoleHost(ICommandExecutor commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        public int Run(string [] args)
        {
            return _commandExecutor.ExecuteCommand(args); 
        }
    }
}