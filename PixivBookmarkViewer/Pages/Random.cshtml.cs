using System.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PixivBookmarkViewer.Pages
{
	public class RandomModel : PageModel
    {
        public string ApiUrl { get; set; }
        public string Search { get; set; }
        public void OnGet(string search)
        {
            Search = search;
            ApiUrl = search == "" ? "/api/Work/Random" : $"/api/Work/Random?search={HttpUtility.UrlEncode(search)}";
        }
    }
}
