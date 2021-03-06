using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BetterExtensions.AspNet.HostedServices
{
    public abstract class TimedHostedService<T> 
        : IHostedService, IDisposable where T : IHostedService
    {
        private Timer _timer;

        protected readonly ILogger<T> _logger;

        private readonly IServiceScopeFactory _scopeFactory;

        protected virtual TimeSpan DueTime => TimeSpan.Zero;
        protected abstract TimeSpan Period { get; }

        protected TimedHostedService(ILogger<T> logger, IServiceScopeFactory scopeFactory) => 
            (_logger,_scopeFactory) = 
            (logger,scopeFactory);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SafeJobAsync, null, DueTime, Period);
            return Task.CompletedTask;
        }

        private async void SafeJobAsync(object sender)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await JobAsync(scope);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
            }
        }

        protected abstract Task JobAsync(IServiceScope serviceScope);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => 
            _timer?.Dispose();
    }
}