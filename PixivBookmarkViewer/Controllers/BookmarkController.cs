using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PixivBookmarkViewer
{
    [Route("api/[controller]")]
	[ApiController]
	public class BookmarkController : ControllerBase
	{
		private PixivService _pixiv;

		public BookmarkController(PixivService pixiv)
		{
			_pixiv = pixiv;
		}

		// GET api/<BookmarkController>/5
		[HttpGet("count")]
		public async Task<int> GetCount()
		{
			return await _pixiv.GetBookmarkCount();
		}

		[HttpGet("last")]
		public List<FullWork> Last(int count)
		{
			return _pixiv.Last(count);
		}
	}
}
