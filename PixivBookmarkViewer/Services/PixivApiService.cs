using PixivBookmarkViewer.Background;
using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Works;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Services
{
	public class PixivApiService
	{
		private readonly PixivQueueService _queue;

		public bool HighPriority { get; set; } = true;

		public PixivApiService(PixivQueueService queue)
		{
			_queue = queue;
		}

		public Task<(Stream, string)> GetPageImage(Page page)
		{
			return _queue.Add(
				api => api.GetWorkImage(page.WorkId, page.PageNumber).Result,
				HighPriority
				);
		}

		public Task<Page[]> GetPages(int id)
		{
			return _queue.Add(
				api => Page.FromApi(api.GetPages(id).Result),
				HighPriority
				);
		}

		public Task<IAsyncEnumerable<BookmarkWork>> GetBookmarks(int number)
		{
			return _queue.Add(
				api => GetAsyncEnum(api.GetBookmarks(number)),
				HighPriority
				);
		}

		public Task<IAsyncEnumerable<BookmarkWork>> GetBookmarks()
		{
			return _queue.Add(
				api => GetAsyncEnum(api.GetBookmarks()),
				HighPriority
				);
		}

		public Task<int> GetBookmarkCount()
		{
			return _queue.Add(
				api => api.GetBookmarkCount().Result,
				HighPriority
				);
		}

		public Task<DetailsWork> GetWork(int id)
		{
			return _queue.Add(
				api => DetailsWork.FromApi(id, api.GetWork(id).Result),
				HighPriority
				);
		}

		private async IAsyncEnumerable<BookmarkWork> GetAsyncEnum(IAsyncEnumerable<PixivApi.Data.BookmarkWork> other)
        {
            await foreach (var work in other)
            {
				yield return BookmarkWork.FromApi(work);
            }
			yield break;
        } 
	}
}
