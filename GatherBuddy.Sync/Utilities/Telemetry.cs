using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GatherBuddy.Sync.Utilities
{
    public class Telemetry
    {
        private readonly ILogger<Telemetry> _logger;
        public Telemetry(ILogger<Telemetry> logger) {
            _logger = logger;
        }

        public void FinishTimerAndLog(Stopwatch sw, [CallerMemberName] string memberName = "")
        {
            sw.Stop();
            _logger.LogInformation($"[{memberName}] {sw.Elapsed.TotalMilliseconds}");
        }
    }
}
