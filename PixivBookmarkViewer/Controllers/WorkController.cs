using PixivBookmarkViewer.Data.Works;
using PixivBookmarkViewer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PixivBookmarkViewer
{
	[Route("api/[controller]")]
	[ApiController]
	public class WorkController : ControllerBase
	{
		private FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();
		private PixivService _pixivService;
        private ThumbnailService _thumbnail;

        public WorkController(PixivService service, ThumbnailService thumbnail)
		{
			_pixivService = service;
			_thumbnail = thumbnail;
		}

		[HttpGet("{id}")]
		public async Task<DetailsWork> GetAsync(int id)
		{
			return await _pixivService.GetWork(id);
		}

		[HttpGet("{id}/image")]
		public async Task<IActionResult> GetImageAsync(int id, int page)
		{
			var (img, name) = await _pixivService.GetImage(id, page);

			if (_contentTypeProvider.TryGetContentType(name, out string contentType))
			{
				Response.Headers.Add("Content-Disposition", $"inline; filename={name}");

				return File(img, contentType);
			}
			else
			{
				return Redirect("~/images/static/500x500.png");
			}
		}

		[HttpGet("{id}/thumbnail")]
		public async Task<IActionResult> GetThumbnailAsync(int id, [FromQuery] int page)
		{
			var (img, name) = await _thumbnail.DownloadThumbnail(id, page);

			if (_contentTypeProvider.TryGetContentType(name, out string contentType))
			{
				Response.Headers.Add("Content-Disposition", $"inline; filename={name}");

				return File(img, contentType);
			}
			else
			{
				return Redirect("~/images/static/500x500.png");
			}
		}

		[HttpGet("random")]
		public async Task<IActionResult> GetRandomImageAsync([FromQuery] string search)
		{
			Stream stream; 
			string name;
			try
			{
				(stream, name) = await _pixivService.GetRandom(search);
			}
			catch
			{
				(stream, name) = _pixivService.NoImage;
			}
			return GetImageIntern(stream, name);
		}

		private IActionResult GetImageIntern(Stream stream, string name)
		{
			Response.Headers.Add("Content-Disposition", $"inline; filename={name}");
			Response.Headers.Add("Cache-Control", "no-store");
			_contentTypeProvider.TryGetContentType(name, out string contentType);
			return File(stream, contentType);
		}
	}
}
