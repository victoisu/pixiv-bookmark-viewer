using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Works;
using PixivBookmarkViewer.Services;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixivBookmarkViewer
{
	public class PixivService
    {
        private IWebHostEnvironment _env;
		private DatabaseService _db;
		private PixivApiService _downloadService;
        private FileService _files;

        private string ImageDirectory => Path.Combine(_env.WebRootPath, "images/dynamic");
		public (Stream, string) NoImage => 
			(File.OpenRead($"{ImageDirectory}/500x500.png"), "500x500.png");
		private Random _rng = new();

		private static ConcurrentDictionary<string, RandomQueue> _rq = new();

		public PixivService(
			IWebHostEnvironment environment,
			DatabaseService db,
			PixivApiService downloadService,
			FileService files)
		{
            _env = environment;
			_db = db;
			_downloadService = downloadService;
			_files = files;
		}

		public async Task<(Stream, string)> GetRandom()
		{
			return await GetRandom("");
		}


		public List<FullWork> Last()
        {
			return Last(0);
        }

		public List<FullWork> Last(int count)
		{
			if (count <= 0)
				count = _db.Works.Count;
			return _db.Works.OrderByDescending(w => w.BookmarkId).Take(count).ToList();
		}

		public async Task<(Stream, string)> GetRandom(string search, WorkCollection collection = null)
		{
			if (search == null)
				return await GetRandom();

			if (!_rq.ContainsKey(search))
			{
				var works = collection ?? _db.Works.SearchWorks(search);
				_rq[search] = new RandomQueue(works, 5, this, _downloadService);
			}

			return await _rq[search].Next();
		}

		public async Task<int> GetBookmarkCount()
		{
			// TODO: get locally
			return await _downloadService.GetBookmarkCount();
		}

		public async Task<DetailsWork> GetWork(int id)
		{
			// TODO: get locally
			return await _downloadService.GetWork(id);
		}

		public async Task<Page> GetPage(int id, int page = 0)
		{
			var dbPage = _db.GetPage(id, page);
			return dbPage ?? await DownloadPage(id, page);
		}

		public async Task<(Stream, string)> GetImage(int id, int page = 0)
		{
			return await GetImage(await GetPage(id, page));
		}

		public async Task<(Stream, string)> GetImage(Page page)
		{
			try
			{
				var (have, image) = await _files.GetPixivOriginal(page.FileName);
				if (have)
                {
					return (image, page.FileName);
                }
				else
                {
					return await DownloadImage(page);
                }
			}
			catch (Exception e)
			{
				//Console.WriteLine(e.Message);
				return NoImage;
			}
		}

		private async Task<Page> DownloadPage(int id, int page = 0)
		{
			return (await DownloadPages(id))[page];
		}

		private async Task<Page[]> DownloadPages(int id)
		{
			var pages = await _downloadService.GetPages(id);
			_db.PutPages(pages);
			return pages;
		}

		private async Task<(Stream, string)> DownloadImage(int id, int page = 0)
		{
			return await DownloadImage(await GetPage(id, page));
		}

		private async Task<(Stream, string)> DownloadImage(Page page)
		{
			try
			{
				var download = _downloadService.GetPageImage(page);

				return (await _files.SavePixivOriginal(page.FileName, download), page.FileName);
			}
			catch
			{
				return NoImage;
			}
		}

		public class RandomQueue
		{
			public List<FullWork> Works { get; init; }
			private Task<(Stream, string)>[] _preload;
			private int _pointer;
			private PixivApiService _apiService;
			private PixivService _pixivService;
			private static SemaphoreSlim _rngLock = new(1);
			private static Random _qRng = new();

			public RandomQueue(IEnumerable<FullWork> works, int preload, PixivService pixivService, PixivApiService apiService)
			{
				Works = works.ToList();
				_preload = new Task<(Stream, string)>[preload];
				_pointer = 0;
				_apiService = apiService;
				_pixivService = pixivService;
				for (int i = 0; i < preload; i++)
				{
					_preload[i] = GetRandomPageTask();
				}
			}

			private Task<(Stream, string)> GetRandomPageTask()
			{
				var work = Works[GetRandom(Works.Count)];
				var pageNumber = GetRandom(work.PageCount);
				return Task.Run(async () => {
					var page = await _pixivService.GetPage(work.Id, pageNumber);
					return await _pixivService.GetImage(page);
				});
			}

			public async Task<(Stream, string)> Next()
			{
				var task = _preload[_pointer];
				_preload[_pointer++] = GetRandomPageTask();
				_pointer %= _preload.Length;
				return await task;
			}

			private int GetRandom(int max)
			{
				_rngLock.Wait();
				int res = _qRng.Next(max);
				_rngLock.Release();
				return res;
			}
		}
	}
}
