using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PixivApi;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Background
{
	public class PixivQueueService : IHostedService
	{
		private ConcurrentQueue<Task> _immediateQueue;
		private ConcurrentQueue<Task> _slowQueue;
		private SemaphoreSlim _downloadLimiter = new(8);
		private SemaphoreSlim _slowLimiter = new(4);
		private Fetcher _api => Fetcher.Instance;
		private CancellationTokenSource _cts;

		public PixivQueueService(IConfiguration configuration)
		{
			_immediateQueue = new();
			_slowQueue = new();
			_cts = new();
			Fetcher.TryConfigure(configuration["Pixiv:PhpSession"]);
		}

		public Task<T> Add<T>(Func<Fetcher, T> task, bool priority)
		{
			var res = new Task<T>(() => task.Invoke(_api));
			AddToQueue(res, (priority ? _immediateQueue : _slowQueue));
			return res;
		}

		private static void AddToQueue(Task task, ConcurrentQueue<Task> queue)
		{
			queue.Enqueue(task);
		}

		public async Task RunLoop(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				await _downloadLimiter.WaitAsync(token);
				try
				{
					var fast = _immediateQueue.TryDequeue(out Task task);
					if (fast || ((_slowLimiter.CurrentCount > 0) && _slowQueue.TryDequeue(out task)))
					{
						RunTask(task, fast);
					}
				}
				finally
				{
					_downloadLimiter.Release();
                }
			}
		}

		private async void RunTask(Task task, bool highPriority)
		{
			await _downloadLimiter.WaitAsync();
			if (!highPriority)
				await _slowLimiter.WaitAsync();

			try
			{
				task.Start();
				await task;
			}
			finally
			{
				_downloadLimiter.Release();
				if (!highPriority)
					_slowLimiter.Release();
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Task.Run(() => RunLoop(_cts.Token), cancellationToken);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_cts.Cancel();
			return Task.CompletedTask;
		}
	}
}
