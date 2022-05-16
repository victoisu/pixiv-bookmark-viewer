using PixivBookmarkViewer.Data.Pixiv;
using PixivBookmarkViewer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Background
{
	public class ArchiverService : IHostedService
	{
		private CancellationTokenSource _tokenSource;
		private Timer _timer = null;
		private IWebHostEnvironment _env;
		private PixivApiService _api;
		private DatabaseService _db;
		private ILogger<ArchiverService> _logger;

		public ArchiverService(
			IWebHostEnvironment environment,
			ILogger<ArchiverService> logger,
			PixivApiService api,
			DatabaseService db
			)
		{
			_env = environment;
			_api = api;
			_api.HighPriority = false;
			_db = db;
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_tokenSource = new();
			_timer = new Timer(ArchiveNow, null, TimeSpan.Zero, TimeSpan.FromHours(18));
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_tokenSource.Cancel();
			_timer.Dispose();
			return Task.CompletedTask;
		}

		public void SkipTimer()
		{
			_timer.Change(TimeSpan.Zero, TimeSpan.FromHours(6));
		}

		private async void ArchiveNow(object state)
		{
#if DEBUG_STYLE
#else
            _logger.LogInformation($"Beginning pixiv archive.");
            var archiver = new PixivArchiver(_api);
            var token = _tokenSource.Token;
            archiver.Initialize(dbPath);
            await archiver.ArchiveAsync(token);
            _db.MergePixivArchive(archiver.DbPath);
#endif
        }

        private string dbPath => Path.Combine(_env.ContentRootPath, "archives");
	}
}
