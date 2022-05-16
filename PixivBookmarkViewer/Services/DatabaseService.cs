using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Works.Interfaces;
using PixivBookmarkViewer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PixivBookmarkViewer
{
	public class DatabaseService
	{
		private ILogger<DatabaseService> _logger;
		private Database _db;
		public WorkCollection Works { get; private set; } = new();
		private SemaphoreSlim _dbLock = new(1);
        private BacklogService _backlog;

        public DatabaseService(
			IWebHostEnvironment env,
			ILogger<DatabaseService> logger
			)
		{
			_logger = logger;
			_db = new Database(env, "database");
			_db.Initialize();
			SetDb(_db);
		}

		public void SetBacklogger(BacklogService service)
        {
			_backlog = service;
        }

		public void SetDb(Database db)
		{
			_db = db;
			_dbLock.Wait();
			Works = _db.GetWorkCollection();
			_dbLock.Release();
		}

		public IEnumerable<FullWork> SearchWorks(string search)
		{
			if (string.IsNullOrWhiteSpace(search))
				return Works;

			_dbLock.Wait();
			var res = Works.SearchWorks(search);
			_dbLock.Release();
			return res;
		}

		public FullWork GetWork(int id)
        {
			return _db.GetWork(id);
		}

		public void PutWork(IWorkDetails work)
		{
			var existing = GetWork(work.Id);
			// TODO: Better comparison please!
			if (work != null && existing == null || (existing.IsBookmarkable == false && work.IsBookmarkable == true))
			{
				_db.InsertWork(work);
			}
		}

		public Page GetPage(int id, int page)
		{
			_dbLock.Wait();
			var res = _db.GetPage(id, page);
			_dbLock.Release();
			return res;
		}

		public IEnumerable<Page> GetPages()
        {
			return _db.GetPages().OrderByDescending(page => Works[page.WorkId].BookmarkId);
        }

		public void PutPages(IEnumerable<Page> pages)
		{
			_dbLock.Wait();
			_db.InsertPages(pages);
			_dbLock.Release();
		}

		public IEnumerable<int> GetPagelessWorks()
        {
			return _db.GetPagelessWorks();
        }

		public void MergePixivArchive(string dbPath)
		{
			_logger.LogInformation($"Merging archive {dbPath}"); 
			_db.MergePixivArchive(dbPath);
			Works = _db.GetWorkCollection();
			_logger.LogInformation($"Merged archive {dbPath}");
			_backlog?.Refresh();
		}
	}
}
