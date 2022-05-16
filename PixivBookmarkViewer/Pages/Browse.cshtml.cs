using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PixivBookmarkViewer.Pages
{
    public class BrowseModel : PageModel
    {
        public int PageSize => 20;
        public string SearchString { get; private set; }
        public void OnGet(string search, int p, bool random)
        {
            SearchString = search ?? "";
            ViewData["page"] = p <= 0 ? 0 : p - 1;
            ViewData["random"] = random ? "true" : "false";
        }
    }
}
