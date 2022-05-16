using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PixivBookmarkViewer.Pages
{
    public class IndexModel : PageModel
	{
		private readonly ILogger<IndexModel> _logger;

        public int BookmarkCount { get; private set; }

		public IndexModel(ILogger<IndexModel> logger, PixivService pixiv, DatabaseService db)
		{
			_logger = logger;
		}

		public async Task<IActionResult> OnGetAsync()
		{
			return Page();
		}
	}
}
