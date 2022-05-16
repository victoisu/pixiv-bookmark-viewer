using PixivBookmarkViewer.Data.Works.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PixivBookmarkViewer.Data.Works
{
    public record DetailsWork : DatabaseWork, IWorkDetails
	{
		public IEnumerable<string> PublicTags { get; init; }
		public IEnumerable<Tag> Tags => PublicTags.Select(t => new Tag { Name = t, IsPublic = true });

		public static DetailsWork FromApi(int id, PixivApi.Data.Work other)
		{
			if (other == null)
				return null;
			return new DetailsWork
			{
				Id = other.Id,
				Title = other.Title,
				IllustType = other.IllustType,
				UserId = other.UserId,
				UserName = other.UserName,
				Width = other.Width,
				Height = other.Height,
				PageCount = other.PageCount,
				IsBookmarkable = other.IsBookmarkable,
				CreateDate = other.CreateDate,
				UpdateDate = other.UpdateDate,

				BookmarkCount = other.BookmarkCount,
				LikeCount = other.LikeCount,
				CommentCount = other.CommentCount,
				ViewCount = other.ViewCount,

				PublicTags = other.PublicTags
			};
		}
    }
}
