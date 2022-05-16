using PixivBookmarkViewer.Data.Pixiv;
using PixivBookmarkViewer.Data.Works.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PixivBookmarkViewer.Data.Works
{
    public record BookmarkWork : MinWork, IWorkPersonalTags, IWorkPublicTags
    {

        public IEnumerable<string> PersonalTags { get; init; }

        public IEnumerable<string> PublicTags { get; init; }

        public IEnumerable<Tag> Tags =>
            PersonalTags.Select(t => (t, false)).Union(PublicTags.Select(t => (t, true)))
            .Select(t => new Tag { Name = t.Item1, IsPublic = t.Item2 });

        public override IWorkDatabase ToDatabaseWork() => new DatabaseWork
        {
            Id = Id,
            Title = Title,
            IllustType = IllustType,
            UserId = UserId,
            UserName = UserName,
            Width = Width,
            Height = Height,
            PageCount = PageCount,
            IsBookmarkable = IsBookmarkable,
            CreateDate = CreateDate,
            UpdateDate = UpdateDate,
            BookmarkId = BookmarkId,
            IsUnlisted = IsUnlisted,

            BookmarkCount = -1,
            LikeCount = -1,
            CommentCount = -1,
            ViewCount = -1
        };

        public static BookmarkWork FromApi(PixivApi.Data.BookmarkWork other) => new BookmarkWork
        {
            Id = other.Id,
            Title = other.Title,
            IllustType = other.IllustType,
            UserName = other.UserName,
            UserId = other.UserId,
            Width = other.Width,
            Height = other.Height,
            PageCount = other.PageCount,
            IsBookmarkable = other.IsBookmarkable,
            CreateDate= other.CreateDate,
            UpdateDate= other.UpdateDate,
            BookmarkId = other.BookmarkId,
            IsUnlisted = other.IsUnlisted,

            PersonalTags = other.PersonalTags,
            PublicTags = other.PublicTags
        };
    }
}
