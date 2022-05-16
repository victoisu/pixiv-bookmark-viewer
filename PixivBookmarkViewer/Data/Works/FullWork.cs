using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Pixiv;
using PixivBookmarkViewer.Data.Works.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PixivBookmarkViewer
{
    public record FullWork : MinWork, IWorkDetails, IWorkPersonalTags
	{

		public IEnumerable<Tag> Tags =>
			PersonalTags.Select(t => (t, false)).Union(PublicTags.Select(t => (t, true)))
			.Select(t => new Tag { Name = t.Item1, IsPublic = t.Item2 });

		public IEnumerable<string> PublicTags { get; init; }
		public IEnumerable<string> PersonalTags { get; init; }

		public bool IsPublicBookmark { get; init; }

		public string ThumbnailUrl { get; init; }

		public string OriginalUrl { get; init; }

		public int BookmarkCount { get; init; }

		public int LikeCount { get; init; }

		public int CommentCount { get; init; }

		public int ViewCount { get; init; }

		public static FullWork FromDatabase(DatabaseWork other, IEnumerable<string> publicTags, IEnumerable<string> personalTags)
		{
			return new FullWork {
				Id = other.Id,
				Title = other.Title,
				IllustType = other.IllustType,
				UserId = other.UserId,
				UserName = other.UserName,
				BookmarkId = other.BookmarkId,
				Width = other.Width,
				Height = other.Height,
				PageCount = other.PageCount,
				IsBookmarkable = other.IsBookmarkable,
				CreateDate = other.CreateDate,
				UpdateDate = other.UpdateDate,
				IsUnlisted = other.IsUnlisted,
				BookmarkCount = other.BookmarkCount,
				LikeCount = other.LikeCount,
				CommentCount = other.CommentCount,
				ViewCount = other.ViewCount,
				PublicTags = publicTags,
				PersonalTags = personalTags
			};
		}

		public override IWorkDatabase ToDatabaseWork() => this;
    }
}
