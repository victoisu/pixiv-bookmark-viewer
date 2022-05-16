using System.Linq;

namespace PixivBookmarkViewer.Data
{
    public record Page
    {
		public int WorkId { get; init; }
		public int PageNumber { get; set; }
		public string OriginalUrl { get; init; }
		public int Width { get; init; }
		public int Height { get; init; }
		public string Extension { get; init; }
		public string FileName { get; init; }

		public string ThumbnailName => $"{WorkId}_p{PageNumber}_thumb.jpg";

		public static Page FromApi(PixivApi.Data.Page other) => new Page
		{
			WorkId = other.WorkId,
			PageNumber = other.PageNumber,
			OriginalUrl = other.OriginalUrl,
			Width = other.Width,
			Height = other.Height,
			Extension = other.Extension,
			FileName = other.FileName
		};

		public static Page[] FromApi(PixivApi.Data.Page[] other) => other.Select(p => FromApi(p)).ToArray();
	}
}
