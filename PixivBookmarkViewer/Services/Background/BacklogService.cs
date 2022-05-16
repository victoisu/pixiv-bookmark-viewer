using PixivBookmarkViewer.Data;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Services
{
    public class BacklogService : IHostedService
    {
        private const int lock_size = 2;
        private PixivApiService _pixiv;
        private ThumbnailService _thumbnail;
        private FileService _files;
        private DatabaseService _db;
        private ConcurrentQueue<int> _backLog = new();
        private SemaphoreSlim _lock = new(lock_size);
        private CancellationToken _token;
        private Timer _timer;

        public BacklogService(
            PixivApiService pixiv, 
            ThumbnailService thumbnail,
            FileService files, 
            DatabaseService db
            )
        {
            _pixiv = pixiv;
            _pixiv.HighPriority = false;

            _thumbnail = thumbnail;
            _files = files;
            _db = db;
            db.SetBacklogger(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
#if DEBUG_STYLE
#else
            _token = cancellationToken;
            _timer = new Timer(Refresh, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            Task.Run(() => StartLoop(cancellationToken), cancellationToken);
#endif
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Refresh()
        {
            _timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private async void Refresh(object state)
        {
            var freelocks = Task.WhenAll(Enumerable.Range(0, lock_size).Select(i => _lock.WaitAsync(_token)));

            await freelocks;
            _backLog.Clear();
            foreach (var id in _db.GetPagelessWorks())
            {
                _backLog.Enqueue(id);
            }
            _lock.Release(lock_size);
        }

        public async Task StartLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _lock.WaitAsync();
                if (_backLog.TryDequeue(out int id))
                {

                    var task = CreateWorkBacklog(id)();
                    _ = task.ContinueWith(_ => _lock.Release());
                }
                else
                {
                    _lock.Release();
                }
            }
        }

        private IEnumerable<Page> UndownloadedPages()
        {
            return _db.GetPages().Where(p => 
                _files.HavePixivOriginal(p.FileName) == string.Empty
            );
        }

        private Func<Task> CreateWorkBacklog(int id)
        {
            return async () => await BacklogWork(id);
        }

        private async Task BacklogWork(int id)
        {
            var pages = _pixiv.GetPages(id);
            await pages.ContinueWith(async pages => _db.PutPages(await pages));
            await _pixiv.GetWork(id).ContinueWith(async work => _db.PutWork(await work));
            await Task.WhenAll((await pages).Select(p => BacklogPage(p)));
            await pages;
        }

        private async Task BacklogPage(Page page)
        {
            if (_files.HavePixivOriginal(page.FileName) == string.Empty)
                await _files.SavePixivOriginal(page.FileName, _pixiv.GetPageImage(page));

            if (_files.HavePixivThumbnail(page.ThumbnailName) == string.Empty)
                await _files.SavePixivThumbnail(page.ThumbnailName, (await _thumbnail.GetThumbnail(page)).Item1);
        }
    }
}
